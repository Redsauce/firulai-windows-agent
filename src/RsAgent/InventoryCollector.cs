using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal static class InventoryCollector
    {
        private sealed class SoftwareInventory
        {
            public List<Dictionary<string, string>> Components { get; private set; }
            public List<Dictionary<string, string>> Packages { get; private set; }

            public SoftwareInventory()
            {
                Components = new List<Dictionary<string, string>>();
                Packages = new List<Dictionary<string, string>>();
            }
        }

        private sealed class RegistrySoftwareRecord
        {
            public string RegistryScope { get; set; }
            public string RegistryKey { get; set; }
            public string Name { get; set; }
            public string CanonicalName { get; set; }
            public string Version { get; set; }
            public string Publisher { get; set; }
            public string InstallAnchor { get; set; }
            public string InstallDirectory { get; set; }
            public string ParentKeyName { get; set; }
            public string ParentDisplayName { get; set; }
            public bool SystemComponent { get; set; }
        }

        public static string Collect(AgentConfig config)
        {
            var software = CollectSoftwareInventory();
            var payload = new Dictionary<string, object>
            {
                { "RSToken", config.Token },
                { "system", CollectSystem(config.Uuid) },
                { "hardware", CollectHardware() },
                { "components", software.Components },
                { "packages", software.Packages }
            };

            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(payload);
        }

        private static Dictionary<string, object> CollectSystem(string uuid)
        {
            var os = QueryFirst("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem");
            var version = WmiString(os, "Version", Environment.OSVersion.Version.ToString());
            var build = WmiString(os, "BuildNumber", Environment.OSVersion.Version.Build.ToString());
            var productName = RegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            var edition = RegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID");
            var displayVersion = RegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion");
            if (string.IsNullOrWhiteSpace(displayVersion))
            {
                displayVersion = RegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");
            }

            return new Dictionary<string, object>
            {
                { "hostname", Environment.MachineName },
                { "fqdn", GetFqdn() },
                { "uuid", uuid },
                { "os", new Dictionary<string, object>
                    {
                        { "name", FirstNonEmpty(productName, WmiString(os, "Caption", "Windows")) },
                        { "version", version },
                        { "build", build },
                        { "edition", FirstNonEmpty(edition, "Unknown") },
                        { "distro_id", "windows" },
                        { "distro_version", FirstNonEmpty(displayVersion, MajorWindowsVersion(version)) },
                        { "kernel", version },
                        { "architecture", FirstNonEmpty(WmiString(os, "OSArchitecture", ""), Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), "unknown") }
                    }
                },
                { "collected_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "timezone", TimeZoneInfo.Local.Id },
                { "agent_version", AgentConfig.AgentVersion }
            };
        }

        private static Dictionary<string, object> CollectHardware()
        {
            var cpu = QueryFirst("SELECT Name FROM Win32_Processor");
            var firmware = new List<Dictionary<string, string>>();

            foreach (var disk in Query("SELECT DeviceID, Model FROM Win32_DiskDrive"))
            {
                firmware.Add(new Dictionary<string, string>
                {
                    { "device", WmiString(disk, "DeviceID", "").Replace(@"\\.\", "") },
                    { "model", WmiString(disk, "Model", "Unknown") }
                });
            }

            return new Dictionary<string, object>
            {
                { "cpu_model", WmiString(cpu, "Name", "Unknown") },
                { "firmware", firmware }
            };
        }

        private static SoftwareInventory CollectSoftwareInventory()
        {
            var inventory = new SoftwareInventory();
            var seenComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var registryRecords = CollectRegistrySoftware().ToList();
            foreach (var record in registryRecords) EnrichRegisteredApplication(record);
            var applicationRecords = new List<RegistrySoftwareRecord>();
            foreach (var record in registryRecords)
            {
                var parent = FindParentPackage(record, registryRecords);
                if (parent != null)
                {
                    AddPackage(inventory.Packages, seenPackages, parent.CanonicalName, parent.Version);
                    AddComponent(inventory.Components, seenComponents, record.Name, record.Version, "registry", parent);
                }
                else if (record.SystemComponent)
                {
                    AddComponent(inventory.Components, seenComponents, record.Name, record.Version, "registry", null);
                }
                else
                {
                    AddPackage(inventory.Packages, seenPackages, record.CanonicalName, record.Version);
                    applicationRecords.Add(record);
                }
            }

            foreach (var application in CollectFileSystemApplications(applicationRecords))
            {
                AddPackage(inventory.Packages, seenPackages, application.CanonicalName, application.Version);
                if (!applicationRecords.Any(existing =>
                    SameValue(existing.CanonicalName, application.CanonicalName) &&
                    SameValue(existing.InstallDirectory, application.InstallDirectory)))
                {
                    applicationRecords.Add(application);
                }
            }

            foreach (var application in applicationRecords)
            {
                foreach (var library in CollectApplicationLibraries(application.InstallDirectory))
                {
                    AddComponent(
                        inventory.Components,
                        seenComponents,
                        library.Item1,
                        library.Item2,
                        "file",
                        application);
                }
            }

            foreach (var appxGroup in CollectAppxPackages().GroupBy(
                appx => GetDictionaryString(appx, "name"),
                StringComparer.OrdinalIgnoreCase))
            {
                var appx = appxGroup
                    .OrderByDescending(item => ParseVersion(GetDictionaryString(item, "version")))
                    .First();
                var isComponent = GetDictionaryString(appx, "is_framework") == "true" ||
                                  GetDictionaryString(appx, "is_resource") == "true";
                var isSystemPackage = GetDictionaryString(appx, "is_system") == "true" ||
                                      GetDictionaryString(appx, "non_removable") == "true";
                if (GetDictionaryString(appx, "is_resource") == "true" || (!isComponent && isSystemPackage))
                {
                    continue;
                }
                if (isComponent)
                {
                    AddComponent(
                        inventory.Components,
                        seenComponents,
                        GetDictionaryString(appx, "name"),
                        GetDictionaryString(appx, "version"),
                        "appx",
                        null);
                }
                else
                {
                    AddPackage(
                        inventory.Packages,
                        seenPackages,
                        GetDictionaryString(appx, "name"),
                        GetDictionaryString(appx, "version"));
                }
            }

            ReconcileComponentPackageNames(inventory);
            return inventory;
        }

        private static void ReconcileComponentPackageNames(SoftwareInventory inventory)
        {
            foreach (var component in inventory.Components)
            {
                var sourcePackage = GetDictionaryString(component, "source_package");
                if (string.IsNullOrWhiteSpace(sourcePackage)) continue;
                if (inventory.Packages.Any(package => SameValue(GetDictionaryString(package, "name"), sourcePackage))) continue;

                var sourceVersion = GetDictionaryString(component, "source_version");
                var canonicalPackage = inventory.Packages.FirstOrDefault(package =>
                    PackageRecordsMatch(package, sourcePackage, sourceVersion));
                if (canonicalPackage != null)
                {
                    component["source_package"] = GetDictionaryString(canonicalPackage, "name");
                }
            }
        }

        private static IEnumerable<RegistrySoftwareRecord> CollectRegistrySoftware()
        {
            var records = new List<RegistrySoftwareRecord>();
            records.AddRange(ReadRegistrySoftwareRoot(
                Registry.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                "machine-64"));
            records.AddRange(ReadRegistrySoftwareRoot(
                Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                "machine-32"));

            try
            {
                foreach (var userSid in Registry.Users.GetSubKeyNames())
                {
                    if (!Regex.IsMatch(userSid, @"^S-1-5-21-(?:\d+-){3}\d+$")) continue;
                    records.AddRange(ReadRegistrySoftwareRoot(
                        Registry.Users,
                        userSid + @"\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                        "user:" + userSid));
                    records.AddRange(ReadRegistrySoftwareRoot(
                        Registry.Users,
                        userSid + @"\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                        "user-32:" + userSid));
                }
            }
            catch
            {
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                var identity = string.Join("|", new[]
                {
                    record.Name ?? "",
                    record.Version ?? "",
                    record.Publisher ?? "",
                    record.InstallAnchor ?? "",
                    record.SystemComponent ? "component" : "visible"
                });
                if (seen.Add(identity)) yield return record;
            }
        }

        private static IEnumerable<RegistrySoftwareRecord> ReadRegistrySoftwareRoot(
            RegistryKey hive,
            string path,
            string scope)
        {
            var records = new List<RegistrySoftwareRecord>();
            try
            {
                using (var key = hive.OpenSubKey(path))
                {
                    if (key == null) return records;
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            if (subKey == null) continue;
                            var name = Convert.ToString(subKey.GetValue("DisplayName"));
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            var productName = Convert.ToString(subKey.GetValue("ProductName"));
                            records.Add(new RegistrySoftwareRecord
                            {
                                RegistryScope = scope,
                                RegistryKey = subKeyName,
                                Name = name.Trim(),
                                CanonicalName = FirstNonEmpty(productName, RemoveOnlySuffix(name)).Trim(),
                                Version = Convert.ToString(subKey.GetValue("DisplayVersion")).Trim(),
                                Publisher = Convert.ToString(subKey.GetValue("Publisher")).Trim(),
                                InstallAnchor = GetInstallAnchor(subKey),
                                InstallDirectory = GetInstallDirectory(subKey),
                                ParentKeyName = Convert.ToString(subKey.GetValue("ParentKeyName")).Trim(),
                                ParentDisplayName = Convert.ToString(subKey.GetValue("ParentDisplayName")).Trim(),
                                SystemComponent = IsRegistryTrue(subKey.GetValue("SystemComponent"))
                            });
                        }
                    }
                }
            }
            catch
            {
            }

            return records;
        }

        private static RegistrySoftwareRecord FindParentPackage(
            RegistrySoftwareRecord component,
            IList<RegistrySoftwareRecord> records)
        {
            if (!string.IsNullOrWhiteSpace(component.ParentKeyName))
            {
                var keyedParent = records.FirstOrDefault(candidate =>
                    !ReferenceEquals(candidate, component) &&
                    SameValue(candidate.RegistryScope, component.RegistryScope) &&
                    string.Equals(candidate.RegistryKey, component.ParentKeyName, StringComparison.OrdinalIgnoreCase));
                if (keyedParent == null)
                {
                    keyedParent = records.FirstOrDefault(candidate =>
                        !ReferenceEquals(candidate, component) &&
                        string.Equals(candidate.RegistryKey, component.ParentKeyName, StringComparison.OrdinalIgnoreCase));
                }
                if (keyedParent != null) return keyedParent;
            }

            if (!string.IsNullOrWhiteSpace(component.ParentDisplayName))
            {
                var namedParent = FindNamedParent(component, records, component.ParentDisplayName);
                if (namedParent != null) return namedParent;
            }

            var installedBy = Regex.Match(
                component.Name ?? "",
                @"\s+installed\s+by\s+(.+?)(?:\s+\(remove only\))?$",
                RegexOptions.IgnoreCase);
            if (installedBy.Success)
            {
                var installedByParent = FindNamedParent(component, records, installedBy.Groups[1].Value);
                if (installedByParent != null) return installedByParent;
            }

            var sameInstallation = records.Where(candidate =>
                !ReferenceEquals(candidate, component) &&
                !candidate.SystemComponent &&
                SameValue(candidate.Publisher, component.Publisher) &&
                SameValue(candidate.InstallAnchor, component.InstallAnchor)).ToList();

            var prefixedParent = sameInstallation
                .Where(candidate => NameStartsWithProduct(component.Name, candidate.CanonicalName))
                .OrderByDescending(candidate => candidate.CanonicalName.Length)
                .FirstOrDefault();
            if (prefixedParent != null) return prefixedParent;

            if (component.SystemComponent && sameInstallation.Count == 1)
            {
                return sameInstallation[0];
            }

            return null;
        }

        private static RegistrySoftwareRecord FindNamedParent(
            RegistrySoftwareRecord component,
            IEnumerable<RegistrySoftwareRecord> records,
            string parentName)
        {
            return records
                .Where(candidate => !ReferenceEquals(candidate, component) && !candidate.SystemComponent)
                .Where(candidate =>
                    SameValue(candidate.CanonicalName, parentName) ||
                    SameValue(candidate.Name, parentName))
                .OrderByDescending(candidate =>
                    SameValue(candidate.Publisher, component.Publisher) &&
                    SameValue(candidate.InstallAnchor, component.InstallAnchor))
                .FirstOrDefault();
        }

        private static void AddPackage(
            ICollection<Dictionary<string, string>> packages,
            ISet<string> seen,
            string name,
            string version)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var displayName = NormalizePackageDisplayName(name);
            var key = NormalizePackageIdentity(displayName, version);
            if (string.IsNullOrWhiteSpace(key)) return;
            if (packages.Any(existing => PackageRecordsMatch(existing, displayName, version))) return;
            if (seen.Any(existing => PackageIdentitiesMatch(existing, key))) return;
            seen.Add(key);

            packages.Add(new Dictionary<string, string>
            {
                { "name", displayName },
                { "version", version ?? "" }
            });
        }

        private static string NormalizePackageIdentity(string name, string version)
        {
            var normalized = RemoveOnlySuffix(name ?? "");
            if (!string.IsNullOrWhiteSpace(version))
            {
                normalized = Regex.Replace(
                    normalized,
                    @"(?:^|\s)" + Regex.Escape(version.Trim()) + @"(?:\s|$)",
                    " ",
                    RegexOptions.IgnoreCase);
            }
            normalized = Regex.Replace(normalized, @"\((?:user|machine|remove only)\)", "", RegexOptions.IgnoreCase);
            return NormalizeProductName(normalized);
        }

        private static bool PackageIdentitiesMatch(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase)) return true;
            var shorter = left.Length <= right.Length ? left : right;
            var longer = left.Length > right.Length ? left : right;
            if (longer == shorter + shorter) return true;
            if (shorter.Length < 8 || (double)shorter.Length / longer.Length < 0.60) return false;
            return longer.IndexOf(shorter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool PackageRecordsMatch(
            Dictionary<string, string> existing,
            string candidateName,
            string candidateVersion)
        {
            var existingName = GetDictionaryString(existing, "name");
            var existingVersion = GetDictionaryString(existing, "version");
            var existingIdentity = NormalizePackageIdentity(existingName, existingVersion);
            var candidateIdentity = NormalizePackageIdentity(candidateName, candidateVersion);
            if (PackageIdentitiesMatch(existingIdentity, candidateIdentity)) return true;
            if (!VersionsEquivalent(existingVersion, candidateVersion)) return false;

            var identifierName = candidateName.IndexOf('.') >= 0 ? candidateName : existingName;
            var displayIdentity = candidateName.IndexOf('.') >= 0 ? existingIdentity : candidateIdentity;
            var identifierParts = identifierName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (identifierParts.Length < 2) return false;

            var hints = identifierParts.Skip(1)
                .Select(NormalizeProductName)
                .Where(hint => hint.Length >= 3)
                .OrderByDescending(hint => hint.Length);
            return hints.Any(hint => displayIdentity.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void AddComponent(
            ICollection<Dictionary<string, string>> components,
            ISet<string> seen,
            string name,
            string version,
            string manager,
            RegistrySoftwareRecord parent)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var key = name.Trim() + "|" + (version ?? "") + "|" + (manager ?? "") + "|" +
                      (parent == null ? "" : parent.CanonicalName + "|" + parent.Version);
            if (!seen.Add(key)) return;

            var component = Package(name.Trim(), version, manager);
            if (parent != null)
            {
                component["source_package"] = NormalizePackageDisplayName(parent.CanonicalName);
                if (!string.IsNullOrWhiteSpace(parent.Version))
                {
                    component["source_version"] = parent.Version;
                    component["upstream_version"] = parent.Version;
                }
            }
            components.Add(component);
        }

        private static string NormalizePackageDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var normalized = RemoveOnlySuffix(value.Trim());

            // Normalize technical identifiers without a vendor/product catalogue.
            // Decimal version separators remain intact.
            normalized = Regex.Replace(normalized, @"(?<=[A-Za-z])\.(?=[A-Za-z0-9])", " ");
            normalized = Regex.Replace(normalized, @"(?<=\d)\.(?=[A-Za-z])", " ");
            normalized = normalized.Replace('_', ' ');
            normalized = Regex.Replace(normalized, @"(?<=[a-z])(?=[A-Z])", " ");
            normalized = Regex.Replace(normalized, @"(?<=\d)(?=[A-Z][a-z])", " ");
            normalized = Regex.Replace(normalized, @"(?<=[A-Z])(?=[A-Z][a-z])", " ");
            normalized = Regex.Replace(normalized, @"\b([A-Za-z]+)\s+\1\b", "$1", RegexOptions.IgnoreCase);
            normalized = Regex.Replace(normalized, @"^([A-Za-z]+)\s+\1(?=\d|[A-Z])", "$1 ", RegexOptions.IgnoreCase);

            return Regex.Replace(normalized, @"\s+", " ").Trim();
        }

        private static IEnumerable<RegistrySoftwareRecord> CollectFileSystemApplications(
            IEnumerable<RegistrySoftwareRecord> registeredApplications)
        {
            var executableCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var programFilesRoot in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                if (string.IsNullOrWhiteSpace(programFilesRoot) || !System.IO.Directory.Exists(programFilesRoot)) continue;
                try
                {
                    foreach (var applicationDirectory in System.IO.Directory.GetDirectories(programFilesRoot))
                    {
                        var coveredByRegistry = registeredApplications.Any(application =>
                            PathContains(application.InstallDirectory, applicationDirectory) ||
                            PathContains(applicationDirectory, application.InstallDirectory));
                        if (!coveredByRegistry)
                        {
                            var primary = FindPrimaryExecutable(applicationDirectory);
                            if (!string.IsNullOrWhiteSpace(primary)) executableCandidates.Add(primary);
                        }
                    }
                }
                catch
                {
                }
            }

            foreach (var executable in CollectNativeEntrypointExecutables())
            {
                if (System.IO.File.Exists(executable)) executableCandidates.Add(executable);
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var executable in executableCandidates)
            {
                FileVersionInfo info;
                try { info = FileVersionInfo.GetVersionInfo(executable); }
                catch { continue; }

                var name = FirstNonEmpty(info.ProductName, info.FileDescription);
                var version = FirstNonEmpty(info.ProductVersion, info.FileVersion);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version)) continue;

                string directory;
                try { directory = System.IO.Path.GetDirectoryName(executable); }
                catch { continue; }
                if (!IsApplicationDirectory(directory)) continue;

                var identity = NormalizeProductName(name) + "|" + version.Trim() + "|" + directory;
                if (!seen.Add(identity)) continue;
                yield return new RegistrySoftwareRecord
                {
                    RegistryScope = "filesystem",
                    RegistryKey = executable,
                    Name = name.Trim(),
                    CanonicalName = name.Trim(),
                    Version = version.Trim(),
                    InstallAnchor = directory.ToLowerInvariant(),
                    InstallDirectory = directory,
                    SystemComponent = false
                };
            }
        }

        private static void EnrichRegisteredApplication(RegistrySoftwareRecord application)
        {
            if (!string.IsNullOrWhiteSpace(application.Version) || !IsApplicationDirectory(application.InstallDirectory)) return;
            var executable = FindPrimaryExecutable(application.InstallDirectory);
            if (string.IsNullOrWhiteSpace(executable)) return;
            try
            {
                var info = FileVersionInfo.GetVersionInfo(executable);
                application.Version = FirstNonEmpty(info.ProductVersion, info.FileVersion);
            }
            catch { }
        }

        private static string FindPrimaryExecutable(string applicationDirectory)
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddExecutableCandidates(applicationDirectory, candidates, 2);
            var directoryName = NormalizeProductName(System.IO.Path.GetFileName(applicationDirectory));
            return candidates
                .Select(path => new { Path = path, Score = ScoreExecutable(path, applicationDirectory, directoryName) })
                .Where(item => item.Score >= 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Path.Length)
                .Select(item => item.Path)
                .FirstOrDefault() ?? "";
        }

        private static int ScoreExecutable(string path, string root, string directoryName)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                if (string.IsNullOrWhiteSpace(FirstNonEmpty(info.ProductName, info.FileDescription)) ||
                    string.IsNullOrWhiteSpace(FirstNonEmpty(info.ProductVersion, info.FileVersion))) return -1;
                var score = 0;
                var executableName = NormalizeProductName(System.IO.Path.GetFileNameWithoutExtension(path));
                var productName = NormalizeProductName(info.ProductName);
                if (SameValue(System.IO.Path.GetDirectoryName(path), root)) score += 30;
                if (directoryName.Length >= 3 && executableName.IndexOf(directoryName, StringComparison.OrdinalIgnoreCase) >= 0) score += 80;
                if (directoryName.Length >= 3 && productName.IndexOf(directoryName, StringComparison.OrdinalIgnoreCase) >= 0) score += 100;
                return score;
            }
            catch { return -1; }
        }

        private static bool PathContains(string child, string parent)
        {
            if (string.IsNullOrWhiteSpace(child) || string.IsNullOrWhiteSpace(parent)) return false;
            try
            {
                var normalizedChild = System.IO.Path.GetFullPath(child).TrimEnd('\\');
                var normalizedParent = System.IO.Path.GetFullPath(parent).TrimEnd('\\');
                return SameValue(normalizedChild, normalizedParent) ||
                       normalizedChild.StartsWith(normalizedParent + "\\", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static void AddExecutableCandidates(string root, ISet<string> results, int maxDepth)
        {
            if (!IsApplicationDirectory(root)) return;
            var pending = new Stack<Tuple<string, int>>();
            pending.Push(Tuple.Create(root, 0));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                try
                {
                    foreach (var file in System.IO.Directory.GetFiles(current.Item1, "*.exe"))
                    {
                        var filename = System.IO.Path.GetFileName(file);
                        if (Regex.IsMatch(filename, @"^(?:unins|uninstall|setup|update|updater|crashpad)", RegexOptions.IgnoreCase)) continue;
                        results.Add(file);
                    }
                    if (current.Item2 >= maxDepth) continue;
                    foreach (var child in System.IO.Directory.GetDirectories(current.Item1))
                    {
                        if ((System.IO.File.GetAttributes(child) & System.IO.FileAttributes.ReparsePoint) == 0)
                        {
                            pending.Push(Tuple.Create(child, current.Item2 + 1));
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static IEnumerable<string> CollectNativeEntrypointExecutables()
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths"
            })
            {
                foreach (var executable in ReadAppPaths(Registry.LocalMachine, path)) results.Add(executable);
                foreach (var executable in ReadAppPaths(Registry.CurrentUser, path)) results.Add(executable);
            }

            foreach (var service in Query("SELECT PathName FROM Win32_Service"))
            {
                var executable = ExtractExecutablePath(WmiString(service, "PathName", ""));
                if (!string.IsNullOrWhiteSpace(executable)) results.Add(executable);
            }

            foreach (var shortcutRoot in GetShortcutRoots())
            {
                if (!System.IO.Directory.Exists(shortcutRoot)) continue;
                string[] shortcuts;
                try { shortcuts = System.IO.Directory.GetFiles(shortcutRoot, "*.lnk", System.IO.SearchOption.AllDirectories); }
                catch { continue; }
                foreach (var shortcutPath in shortcuts)
                {
                    var target = ResolveShortcut(shortcutPath);
                    if (!string.IsNullOrWhiteSpace(target)) results.Add(target);
                }
            }
            return results;
        }

        private static IEnumerable<string> ReadAppPaths(RegistryKey hive, string path)
        {
            var results = new List<string>();
            try
            {
                using (var root = hive.OpenSubKey(path))
                {
                    if (root == null) return results;
                    foreach (var name in root.GetSubKeyNames())
                    {
                        using (var key = root.OpenSubKey(name))
                        {
                            var executable = key == null ? "" : ExtractExecutablePath(Convert.ToString(key.GetValue("")));
                            if (!string.IsNullOrWhiteSpace(executable)) results.Add(executable);
                        }
                    }
                }
            }
            catch
            {
            }
            return results;
        }

        private static IEnumerable<string> GetShortcutRoots()
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };
            try
            {
                using (var profiles = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList"))
                {
                    if (profiles != null)
                    {
                        foreach (var sid in profiles.GetSubKeyNames())
                        using (var profile = profiles.OpenSubKey(sid))
                        {
                            var home = profile == null ? "" : Environment.ExpandEnvironmentVariables(Convert.ToString(profile.GetValue("ProfileImagePath")));
                            if (string.IsNullOrWhiteSpace(home)) continue;
                            roots.Add(System.IO.Path.Combine(home, @"AppData\Roaming\Microsoft\Windows\Start Menu"));
                            roots.Add(System.IO.Path.Combine(home, "Desktop"));
                            var localPrograms = System.IO.Path.Combine(home, @"AppData\Local\Programs");
                            if (System.IO.Directory.Exists(localPrograms)) roots.Add(localPrograms);
                        }
                    }
                }
            }
            catch
            {
            }
            return roots.Where(root => !string.IsNullOrWhiteSpace(root));
        }

        private static string ResolveShortcut(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return "";
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                return ExtractExecutablePath(Convert.ToString(shortcut.TargetPath));
            }
            catch { return ""; }
        }

        private static string ExtractExecutablePath(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "";
            var expanded = Environment.ExpandEnvironmentVariables(command.Trim());
            var match = Regex.Match(expanded, "^\\\"([^\\\"]+?\\.exe)\\\"|^([^\\\"]+?\\.exe)(?:\\s|$)", RegexOptions.IgnoreCase);
            var value = match.Success ? FirstNonEmpty(match.Groups[1].Value, match.Groups[2].Value) : expanded;
            try { return System.IO.Path.GetFullPath(value.Trim().Trim('"')); }
            catch { return ""; }
        }

        private static IEnumerable<Tuple<string, string>> CollectApplicationLibraries(string root)
        {
            var results = new List<Tuple<string, string>>();
            if (!IsApplicationDirectory(root)) return results;
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                try
                {
                    foreach (var child in System.IO.Directory.GetDirectories(directory))
                    {
                        if ((System.IO.File.GetAttributes(child) & System.IO.FileAttributes.ReparsePoint) == 0) pending.Push(child);
                    }
                    foreach (var file in System.IO.Directory.GetFiles(directory, "*.dll"))
                    {
                        try
                        {
                            var info = FileVersionInfo.GetVersionInfo(file);
                            var version = FirstNonEmpty(info.ProductVersion, info.FileVersion);
                            if (string.IsNullOrWhiteSpace(version)) continue;
                            var name = FirstNonEmpty(info.OriginalFilename, System.IO.Path.GetFileName(file));
                            results.Add(Tuple.Create(name.Trim(), version.Trim()));
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return results;
        }

        private static bool IsApplicationDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return false;
            string fullPath;
            try { fullPath = System.IO.Path.GetFullPath(directory).TrimEnd('\\'); }
            catch { return false; }
            if (!System.IO.Directory.Exists(fullPath)) return false;
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
            return !fullPath.StartsWith(windows + "\\", StringComparison.OrdinalIgnoreCase) &&
                   !SameValue(fullPath, windows);
        }

        private static string GetInstallDirectory(RegistryKey key)
        {
            var directory = FirstNonEmpty(
                Convert.ToString(key.GetValue("InstallLocation")),
                Convert.ToString(key.GetValue("InstallDir")));
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = GetExecutableDirectory(FirstNonEmpty(
                    Convert.ToString(key.GetValue("DisplayIcon")),
                    Convert.ToString(key.GetValue("UninstallString")),
                    Convert.ToString(key.GetValue("QuietUninstallString"))));
            }
            try { return string.IsNullOrWhiteSpace(directory) ? "" : System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory.Trim().Trim('"'))).TrimEnd('\\'); }
            catch { return ""; }
        }

        private static string GetInstallAnchor(RegistryKey key)
        {
            var anchor = FirstNonEmpty(
                Convert.ToString(key.GetValue("InstallLocation")),
                Convert.ToString(key.GetValue("InstallDir")),
                Convert.ToString(key.GetValue("InstallSource")));
            if (string.IsNullOrWhiteSpace(anchor))
            {
                anchor = GetExecutableDirectory(FirstNonEmpty(
                    Convert.ToString(key.GetValue("DisplayIcon")),
                    Convert.ToString(key.GetValue("UninstallString")),
                    Convert.ToString(key.GetValue("QuietUninstallString"))));
            }

            if (string.IsNullOrWhiteSpace(anchor)) return "";
            try
            {
                return System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(anchor.Trim().Trim('"')))
                    .TrimEnd('\\')
                    .ToLowerInvariant();
            }
            catch
            {
                return anchor.Trim().Trim('"').TrimEnd('\\').ToLowerInvariant();
            }
        }

        private static string GetExecutableDirectory(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "";
            var expanded = Environment.ExpandEnvironmentVariables(command.Trim());
            string executable;
            if (expanded.StartsWith("\"", StringComparison.Ordinal))
            {
                var closingQuote = expanded.IndexOf('"', 1);
                executable = closingQuote > 1 ? expanded.Substring(1, closingQuote - 1) : "";
            }
            else
            {
                var match = Regex.Match(expanded, @"^(.*?\.exe)(?:,|\s|$)", RegexOptions.IgnoreCase);
                executable = match.Success ? match.Groups[1].Value : "";
            }

            try { return string.IsNullOrWhiteSpace(executable) ? "" : System.IO.Path.GetDirectoryName(executable); }
            catch { return ""; }
        }

        private static bool IsRegistryTrue(object value)
        {
            int parsed;
            return value != null && int.TryParse(Convert.ToString(value), out parsed) && parsed == 1;
        }

        private static bool SameValue(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left) &&
                   !string.IsNullOrWhiteSpace(right) &&
                   string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool NameStartsWithProduct(string componentName, string productName)
        {
            if (string.IsNullOrWhiteSpace(componentName) || string.IsNullOrWhiteSpace(productName)) return false;
            if (!componentName.StartsWith(productName, StringComparison.OrdinalIgnoreCase)) return false;
            if (componentName.Length == productName.Length) return false;
            var separator = componentName.Substring(productName.Length).TrimStart();
            if (separator.Length == 0) return false;
            return separator.StartsWith("-", StringComparison.Ordinal) ||
                   separator.StartsWith("(", StringComparison.Ordinal) ||
                   char.IsDigit(separator[0]);
        }

        private static string RemoveOnlySuffix(string name)
        {
            return Regex.Replace(name ?? "", @"\s*\(remove only\)\s*$", "", RegexOptions.IgnoreCase);
        }

        private static bool VersionsEquivalent(string left, string right)
        {
            if (SameValue(left, right)) return true;
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            return left.Trim().StartsWith(right.Trim() + ".", StringComparison.OrdinalIgnoreCase) ||
                   right.Trim().StartsWith(left.Trim() + ".", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeProductName(string value)
        {
            return Regex.Replace(value ?? "", @"[^0-9A-Za-z]+", "").ToLowerInvariant();
        }

        private static Version ParseVersion(string value)
        {
            Version version;
            return Version.TryParse(value, out version) ? version : new Version(0, 0);
        }

        private static Dictionary<string, string> Package(string name, string version, string manager)
        {
            return new Dictionary<string, string>
            {
                { "name", name ?? "" },
                { "version", version ?? "" },
                { "manager", manager }
            };
        }

        private static string RunCommand(string fileName, string arguments, int timeoutSeconds)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) return "";
                    var output = new System.Text.StringBuilder();
                    var error = new System.Text.StringBuilder();
                    process.OutputDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data == null) return;
                        lock (output) { output.AppendLine(eventArgs.Data); }
                    };
                    process.ErrorDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data == null) return;
                        lock (error) { error.AppendLine(eventArgs.Data); }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutSeconds * 1000))
                    {
                        try { process.Kill(); } catch { }
                        return "";
                    }
                    process.WaitForExit();

                    return FirstNonEmpty(output.ToString(), error.ToString()).Trim();
                }
            }
            catch
            {
                return "";
            }
        }

        private static List<ManagementObject> Query(string query)
        {
            var items = new List<ManagementObject>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject item in results)
                    {
                        items.Add(item);
                    }
                }
            }
            catch
            {
            }

            return items;
        }

        private static ManagementObject QueryFirst(string query)
        {
            return Query(query).FirstOrDefault();
        }

        private static string WmiString(ManagementBaseObject item, string name, string fallback)
        {
            if (item == null) return fallback;
            try
            {
                var value = item[name];
                return value == null ? fallback : Convert.ToString(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static string RegistryString(RegistryKey root, string path, string name)
        {
            try
            {
                using (var key = root.OpenSubKey(path))
                {
                    return key == null ? "" : Convert.ToString(key.GetValue(name));
                }
            }
            catch
            {
                return "";
            }
        }

        private static string GetFqdn()
        {
            try
            {
                var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                if (string.IsNullOrWhiteSpace(domain)) return Dns.GetHostName();
                return Dns.GetHostName() + "." + domain;
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
        }

        private static string MajorWindowsVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "Unknown";
            var major = version.Split('.')[0];
            return major == "10" ? "10/11" : major;
        }

        private static string GetDictString(Dictionary<string, object> item, string key)
        {
            object value;
            return item != null && item.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
        }

        private static IEnumerable<Dictionary<string, string>> CollectAppxPackages()
        {
            const string script =
                "$items=@(Get-AppxPackage -AllUsers -ErrorAction SilentlyContinue);" +
                "if($items.Count -eq 0){$items=@(Get-AppxPackage -ErrorAction SilentlyContinue)};" +
                "@($items|ForEach-Object{[PSCustomObject]@{" +
                "name=[string]$_.Name;version=[string]$_.Version;" +
                "is_framework=[bool]$_.IsFramework;is_resource=[bool]$_.IsResourcePackage;" +
                "non_removable=[bool]$_.NonRemovable;is_system=([string]$_.SignatureKind -eq 'System')" +
                "}})|ConvertTo-Json -Compress";
            var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            var output = RunCommand(
                "powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded,
                45);
            if (string.IsNullOrWhiteSpace(output)) yield break;

            object parsed;
            try { parsed = new JavaScriptSerializer().DeserializeObject(output); }
            catch { yield break; }

            var rows = parsed as object[];
            if (rows == null)
            {
                var single = parsed as Dictionary<string, object>;
                rows = single == null ? new object[0] : new object[] { single };
            }

            foreach (var item in rows.OfType<Dictionary<string, object>>())
            {
                var name = GetDictString(item, "name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                yield return new Dictionary<string, string>
                {
                    { "name", name },
                    { "version", GetDictString(item, "version") },
                    { "is_framework", GetBooleanString(item, "is_framework") },
                    { "is_resource", GetBooleanString(item, "is_resource") },
                    { "non_removable", GetBooleanString(item, "non_removable") },
                    { "is_system", GetBooleanString(item, "is_system") }
                };
            }
        }

        private static string GetDictionaryString(Dictionary<string, string> item, string key)
        {
            string value;
            return item != null && item.TryGetValue(key, out value) && value != null ? value : "";
        }

        private static string GetBooleanString(Dictionary<string, object> item, string key)
        {
            object value;
            bool parsed;
            return item != null && item.TryGetValue(key, out value) && value != null &&
                   bool.TryParse(Convert.ToString(value), out parsed) && parsed
                ? "true"
                : "false";
        }
    }
}
