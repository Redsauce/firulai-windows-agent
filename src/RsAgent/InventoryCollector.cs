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
        public static string Collect(AgentConfig config)
        {
            var payload = new Dictionary<string, object>
            {
                { "RSToken", config.Token },
                { "system", CollectSystem(config.Uuid) },
                { "hardware", CollectHardware() },
                { "packages", CollectPackages() },
                { "core_software", CollectCoreSoftware() }
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

        private static List<Dictionary<string, string>> CollectPackages()
        {
            var packages = new List<Dictionary<string, string>>();
            packages.AddRange(CollectRegistryPackages());
            packages.AddRange(CollectWingetPackages());
            packages.AddRange(CollectChocolateyPackages());
            packages.AddRange(CollectPipPackages());
            packages.AddRange(CollectNpmPackages());
            return packages;
        }

        private static IEnumerable<Dictionary<string, string>> CollectRegistryPackages()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var root in roots)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (key == null) continue;
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            var name = Convert.ToString(subKey.GetValue("DisplayName"));
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            var version = Convert.ToString(subKey.GetValue("DisplayVersion"));
                            var id = name + "|" + version;
                            if (!seen.Add(id)) continue;

                            yield return Package(name, version, "registry");
                        }
                    }
                }
            }
        }

        private static IEnumerable<Dictionary<string, string>> CollectWingetPackages()
        {
            var output = RunCommand("winget", "list --disable-interactivity", 30);
            if (string.IsNullOrWhiteSpace(output)) yield break;

            foreach (var line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(2))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("-", StringComparison.Ordinal)) continue;

                var columns = Regex.Split(trimmed, @"\s{2,}").Where(x => x.Length > 0).ToArray();
                if (columns.Length < 2) continue;

                var version = columns.Length >= 3 ? columns[2] : "";
                yield return Package(columns[0], version, "winget");
            }
        }

        private static IEnumerable<Dictionary<string, string>> CollectChocolateyPackages()
        {
            var output = RunCommand("choco", "list --local-only --limit-output", 30);
            if (string.IsNullOrWhiteSpace(output)) yield break;

            foreach (var line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length < 2) continue;
                yield return Package(parts[0], parts[1], "choco");
            }
        }

        private static IEnumerable<Dictionary<string, string>> CollectPipPackages()
        {
            var output = RunCommand("pip", "list --format=json", 30);
            if (string.IsNullOrWhiteSpace(output)) output = RunCommand("pip3", "list --format=json", 30);
            if (string.IsNullOrWhiteSpace(output)) yield break;

            object parsed;
            try { parsed = new JavaScriptSerializer().DeserializeObject(output); }
            catch { yield break; }

            var rows = parsed as object[];
            if (rows == null) yield break;
            foreach (var item in rows.OfType<Dictionary<string, object>>())
            {
                yield return Package(GetDictString(item, "name"), GetDictString(item, "version"), "pip");
            }
        }

        private static IEnumerable<Dictionary<string, string>> CollectNpmPackages()
        {
            var output = RunCommand("npm", "list -g --depth=0 --json", 30);
            if (string.IsNullOrWhiteSpace(output)) yield break;

            object parsed;
            try { parsed = new JavaScriptSerializer().DeserializeObject(output); }
            catch { yield break; }

            var root = parsed as Dictionary<string, object>;
            if (root == null || !root.ContainsKey("dependencies")) yield break;
            var deps = root["dependencies"] as Dictionary<string, object>;
            if (deps == null) yield break;

            foreach (var dep in deps)
            {
                var meta = dep.Value as Dictionary<string, object>;
                yield return Package(dep.Key, meta == null ? "" : GetDictString(meta, "version"), "npm");
            }
        }

        private static List<Dictionary<string, string>> CollectCoreSoftware()
        {
            var rows = new List<Dictionary<string, string>>();

            AddIfPresent(rows, "iis", GetIisVersion());
            AddIfPresent(rows, "mssql", GetSqlServerVersion());

            var commands = new[]
            {
                Tuple.Create("apache2", "httpd", "-v"),
                Tuple.Create("httpd", "httpd", "-v"),
                Tuple.Create("nginx", "nginx", "-v"),
                Tuple.Create("mysql", "mysql", "--version"),
                Tuple.Create("postgresql", "psql", "--version"),
                Tuple.Create("php", "php", "--version"),
                Tuple.Create("node", "node", "--version"),
                Tuple.Create("python3", "python", "--version"),
                Tuple.Create("java", "java", "-version"),
                Tuple.Create("docker", "docker", "--version"),
                Tuple.Create("git", "git", "--version"),
                Tuple.Create("openssh", "ssh", "-V"),
                Tuple.Create("openssl", "openssl", "version"),
                Tuple.Create("powershell", "powershell", "-NoProfile -Command \"$PSVersionTable.PSVersion.ToString()\"")
            };

            foreach (var item in commands)
            {
                var raw = RunCommand(item.Item2, item.Item3, 10);
                AddIfPresent(rows, item.Item1, raw);
            }

            AddDotnet(rows, "dotnet_runtime", "dotnet", "--list-runtimes");
            AddDotnet(rows, "dotnet_sdk", "dotnet", "--list-sdks");
            return rows;
        }

        private static void AddDotnet(List<Dictionary<string, string>> rows, string name, string file, string args)
        {
            var raw = RunCommand(file, args, 10);
            if (string.IsNullOrWhiteSpace(raw)) return;

            foreach (var line in raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                AddIfPresent(rows, name, line);
            }
        }

        private static void AddIfPresent(List<Dictionary<string, string>> rows, string name, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            rows.Add(new Dictionary<string, string>
            {
                { "name", name },
                { "version", ExtractVersion(raw) },
                { "raw_output", raw.Trim() }
            });
        }

        private static string GetIisVersion()
        {
            var version = RegistryString(Registry.LocalMachine, @"SOFTWARE\Microsoft\InetStp", "VersionString");
            return string.IsNullOrWhiteSpace(version) ? "" : version;
        }

        private static string GetSqlServerVersion()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"))
            {
                if (key == null) return "";
                var names = key.GetValueNames();
                if (names.Length == 0) return "";
                return "SQL Server instance " + names[0];
            }
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
                    if (!process.WaitForExit(timeoutSeconds * 1000))
                    {
                        try { process.Kill(); } catch { }
                        return "";
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    return FirstNonEmpty(output, error).Trim();
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

        private static string ExtractVersion(string raw)
        {
            var match = Regex.Match(raw ?? "", @"\d+\.\d+(\.\d+)?([.-][0-9A-Za-z]+)?");
            return match.Success ? match.Value : "unknown";
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
    }
}
