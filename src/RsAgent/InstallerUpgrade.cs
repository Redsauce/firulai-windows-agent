using System;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;
using Microsoft.Win32;

namespace RsAgent
{
    // Runs from the installer's temporary directory, independently of firulaiAgent.
    internal static class InstallerUpgrade
    {
        private const string RecoveryKey = @"SOFTWARE\Redsauce\firulaiAgent\UpgradeRecovery";
        private const string RecoveryTask = "FirulaiAgentUpgradeRecovery";

        private static int RunTaskCommand(string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), arguments) {
                UseShellExecute = false, CreateNoWindow = true
            }))
            {
                if (!process.WaitForExit(30000)) return 1;
                return process.ExitCode;
            }
        }

        internal static bool RecoveryPending()
        {
            using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = registry.OpenSubKey(RecoveryKey)) return key != null;
        }

        internal static int PrepareRecovery(string appDir, string installed)
        {
            try
            {
                ParseVersion(installed);
                appDir = Path.GetFullPath(appDir);
                var binary = Path.Combine(appDir, "FirulaiAgent.exe");
                using (var serviceKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\firulaiAgent"))
                {
                    var imagePath = serviceKey == null ? "" : Environment.ExpandEnvironmentVariables(Convert.ToString(serviceKey.GetValue("ImagePath"))).Trim().Trim('"');
                    if (!string.Equals(imagePath, binary, StringComparison.OrdinalIgnoreCase)) return 1;
                }
                if (!File.Exists(binary + ".pre-update") || RecoveryPending()) return 1;
                if ((File.GetAttributes(AgentConfig.DataDir) & FileAttributes.ReparsePoint) != 0) return 1;
                var recoveryDir = Path.Combine(AgentConfig.DataDir, "upgrade-recovery");
                var directoryAcl = new System.Security.AccessControl.DirectorySecurity();
                directoryAcl.SetAccessRuleProtection(true, false);
                foreach (var sid in new[] { "S-1-5-18", "S-1-5-32-544" })
                    directoryAcl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(sid),
                        System.Security.AccessControl.FileSystemRights.FullControl,
                        System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                        System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
                Directory.CreateDirectory(recoveryDir, directoryAcl);
                if ((File.GetAttributes(recoveryDir) & FileAttributes.ReparsePoint) != 0) return 1;
                Directory.SetAccessControl(recoveryDir, directoryAcl);
                var helper = Path.Combine(recoveryDir, "upgrade-recovery.exe");
                if (File.Exists(helper) && (File.GetAttributes(helper) & FileAttributes.ReparsePoint) != 0) return 1;
                File.Copy(typeof(InstallerUpgrade).Assembly.Location, helper, true);
                var acl = new System.Security.AccessControl.FileSecurity();
                acl.SetAccessRuleProtection(true, false);
                foreach (var sid in new[] { "S-1-5-18", "S-1-5-32-544" })
                    acl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(new System.Security.Principal.SecurityIdentifier(sid),
                        System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
                File.SetAccessControl(helper, acl);
                using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = registry.CreateSubKey(RecoveryKey))
                {
                    key.SetValue("AppDir", appDir);
                    key.SetValue("Version", installed);
                }
                if (RunTaskCommand("/Create /TN " + RecoveryTask + " /SC ONSTART /DELAY 0001:00 /RU SYSTEM /RL HIGHEST /TR \"\\\"" + helper + "\\\" --recover-upgrade\" /F") != 0)
                {
                    CompleteRecovery();
                    return 1;
                }
                if (StopForReplacement(binary) == 0) return 0;
                // No installation file has been replaced. Avoid copying onto
                // a still-locked image when undoing a rejected preparation.
                Logger.Warn("Upgrade preparation canceled; starting the unchanged agent.");
                if (ServiceAction("start") == 0) CompleteRecovery();
                return 1;
            }
            catch (Exception ex) { Logger.Error("Could not prepare agent replacement.", ex); return 1; }
        }

        internal static int CompleteRecovery()
        {
            try
            {
                using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    registry.DeleteSubKeyTree(RecoveryKey, false);
                // A stale startup task is harmless: Recover() exits when there is no journal.
                RunTaskCommand("/Delete /TN " + RecoveryTask + " /F");
                return 0;
            }
            catch { return 1; }
        }

        internal static int Recover()
        {
            try
            {
                string appDir, installed;
                using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = registry.OpenSubKey(RecoveryKey))
                {
                    if (key == null) return 0;
                    appDir = Convert.ToString(key.GetValue("AppDir"));
                    installed = Convert.ToString(key.GetValue("Version"));
                }
                ParseVersion(installed);
                var binary = Path.Combine(appDir, "FirulaiAgent.exe");
                if (StopForReplacement(binary, true) != 0) return 1;
                bool restored = false;
                for (int attempt = 0; attempt < 30 && !restored; attempt++)
                {
                    try { File.Copy(binary + ".pre-update", binary, true); restored = true; }
                    catch (IOException) { System.Threading.Thread.Sleep(1000); }
                }
                if (!restored) return 1;
                using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1", true))
                    if (key != null) key.SetValue("DisplayVersion", installed);
                if (ServiceAction("start") != 0) return 1;
                return CompleteRecovery();
            }
            catch (Exception ex) { Logger.Error("Could not restore the previous agent; recovery remains pending.", ex); return 1; }
        }

        private static int StopForReplacement(string binary, bool allowMissing = false)
        {
            if (ServiceAction("stop") != 0)
            {
                Logger.Warn("Could not stop firulaiAgent before replacing its executable.");
                return 1;
            }
            var timer = Stopwatch.StartNew();
            return WaitForExecutableRelease(binary, TimeSpan.FromSeconds(90), () => timer.Elapsed,
                () => System.Threading.Thread.Sleep(500), Logger.Info, allowMissing) ? 0 : 1;
        }

        // A stopped SCM service can still have a live process, and a manual
        // --run-once invocation is independent of the service. Never terminate
        // either process: wait for the exact installed image to be released.
        internal static bool WaitForExecutableRelease(string binary, TimeSpan timeout,
            Func<TimeSpan> elapsed, Action wait, Action<string> log, bool allowMissing = false)
        {
            binary = Path.GetFullPath(binary);
            string previous = null;
            while (true)
            {
                string reason = null;
                foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(binary)))
                {
                    using (process)
                    {
                        try
                        {
                            if (!process.HasExited && string.Equals(process.MainModule.FileName, binary, StringComparison.OrdinalIgnoreCase))
                                reason = "installed executable still running, PID=" + process.Id;
                        }
                        catch (InvalidOperationException) { } // Process exited during inspection.
                        catch (System.ComponentModel.Win32Exception) { } // File probe below remains mandatory.
                    }
                }
                if (reason == null)
                {
                    try
                    {
                        // Read/write plus exclusive sharing rejects a mapped executable
                        // as well as external handles. Opening must never truncate it.
                        using (var file = new FileStream(binary, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                        log("Agent executable is released and ready for replacement.");
                        return true;
                    }
                    catch (FileNotFoundException ex)
                    {
                        // An aborted installer can have removed the destination.
                        // Recovery must still be able to restore the saved image.
                        if (allowMissing)
                        {
                            log("Agent executable is absent; ready to restore the saved image.");
                            return true;
                        }
                        reason = ex.GetType().Name + ": " + ex.Message;
                    }
                    catch (IOException ex) { reason = ex.GetType().Name + ": " + ex.Message; }
                    catch (UnauthorizedAccessException ex) { reason = ex.GetType().Name + ": " + ex.Message; }
                }
                if (reason != previous)
                {
                    log("Waiting for agent executable release: " + reason);
                    previous = reason;
                }
                if (elapsed() >= timeout)
                {
                    log("Agent replacement canceled before file installation: executable not released. " + reason);
                    return false;
                }
                wait();
            }
        }
        internal static Version ParseVersion(string value)
        {
            Version version;
            if (value == null || !System.Text.RegularExpressions.Regex.IsMatch(value, @"^[0-9]+\.[0-9]+\.[0-9]+$") ||
                !Version.TryParse(value, out version)) throw new InvalidOperationException("Invalid stable version.");
            return version;
        }

        internal static bool IsNewer(string candidate, string installed)
        {
            return ParseVersion(candidate).CompareTo(ParseVersion(installed)) > 0;
        }

        internal static int Inspect(string candidate, string resultPath)
        {
            try
            {
                if (RecoveryPending()) return 1;
                var config = AgentConfig.Load();
                string installed;
                using (var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1"))
                    installed = key == null ? "" : Convert.ToString(key.GetValue("DisplayVersion"));
                if (!IsNewer(candidate, installed)) return 2;
                // No tokens or other credentials leave config.json.
                File.WriteAllText(resultPath, "[upgrade]\nuuid=" + config.Uuid + "\nlocale=" + config.Locale + "\nversion=" + installed + "\n");
                return 0;
            }
            catch { return 1; }
        }

        internal static int ServiceAction(string action)
        {
            try
            {
                using (var service = new ServiceController("firulaiAgent"))
                {
                    service.Refresh();
                    if (action == "stop")
                    {
                        if (service.Status != ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.StopPending)
                            service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(90));
                        return 0;
                    }
                    if (action != "start") return 1;
                    if (service.Status != ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.StartPending)
                        service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
                    System.Threading.Thread.Sleep(2000);
                    service.Refresh();
                    return service.Status == ServiceControllerStatus.Running ? 0 : 1;
                }
            }
            catch { return 1; }
        }
    }
}
