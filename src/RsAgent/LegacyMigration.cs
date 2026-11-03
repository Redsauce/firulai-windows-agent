using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace RsAgent
{
    // Called only by the signed installer while replacing the pre-rename agent.
    internal static class LegacyMigration
    {
        private const string UninstallKey = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{A2B3E8CC-81AC-49DD-B2FB-8078A01D76D9}_is1";
        private static readonly string OldApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RSAgent");
        private static readonly string OldData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RSAgent");
        private static readonly string NewData = AgentConfig.DataDir;
        private static readonly string Journal = Path.Combine(NewData, "legacy-migration.reg");
        private static readonly string RecoveryExe = Path.Combine(NewData, "legacy-recovery.exe");
        private const string RecoveryTask = "FirulaiAgentLegacyMigrationRecovery";
        private const string CleanupTask = "FirulaiAgentLegacyCleanup";

        private static int Command(string file, string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, file), arguments)
                { UseShellExecute = false, CreateNoWindow = true }))
            {
                if (process == null || !process.WaitForExit(90000)) return 1;
                return process.ExitCode;
            }
        }

        private static bool ServiceRunning(string name)
        {
            using (var service = new ServiceController(name))
            {
                service.Refresh();
                return service.Status == ServiceControllerStatus.Running;
            }
        }

        internal static int Prepare()
        {
            bool created = false;
            try
            {
                if (!File.Exists(Path.Combine(OldApp, "RsAgent.exe")) ||
                    !File.Exists(Path.Combine(OldData, "config.json")) ||
                    Directory.Exists(NewData) ||
                    (File.GetAttributes(OldData) & FileAttributes.ReparsePoint) != 0 ||
                    (File.GetAttributes(OldApp) & FileAttributes.ReparsePoint) != 0) return 1;
                using (var service = new ServiceController("RSAgent"))
                    if (service.Status != ServiceControllerStatus.Running) return 1;
                using (var key = Registry.LocalMachine.OpenSubKey(UninstallKey.Substring(5)))
                {
                    if (key == null || !InstallerUpgrade.IsNewer(AgentConfig.AgentVersion,
                        Convert.ToString(key.GetValue("DisplayVersion")))) return 1;
                }
                using (var serviceKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RSAgent"))
                {
                    if (serviceKey == null) return 1;
                    var image = Environment.ExpandEnvironmentVariables(Convert.ToString(serviceKey.GetValue("ImagePath")) ?? "")
                        .Trim().Trim('"');
                    if (!string.Equals(image, Path.Combine(OldApp, "RsAgent.exe"), StringComparison.OrdinalIgnoreCase)) return 1;
                }

                var acl = new DirectorySecurity();
                acl.SetAccessRuleProtection(true, false);
                foreach (var sid in new[] { "S-1-5-18", "S-1-5-32-544" })
                    acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid), FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                        AccessControlType.Allow));
                Directory.CreateDirectory(NewData, acl);
                created = true;
                // The old updater is still running from OldData/updates while it
                // waits for this installer. Copy only persistent agent state.
                foreach (var name in new[] { "config.json", "state.json", "inventory.json" })
                {
                    var source = Path.Combine(OldData, name);
                    if (!File.Exists(source)) continue;
                    if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException("Reparse point in legacy state.");
                    File.Copy(source, Path.Combine(NewData, name), false);
                }
                Directory.CreateDirectory(Path.Combine(NewData, "logs"));
                if (Command("reg.exe", "export \"" + UninstallKey + "\" \"" + Journal + "\" /y") != 0)
                    throw new InvalidOperationException("Could not preserve uninstall entry.");
                File.Copy(typeof(LegacyMigration).Assembly.Location, RecoveryExe, false);
                var recoveryCommand = "\"" + RecoveryExe + "\" --rollback-legacy-migration";
                if (Command("schtasks.exe", "/Create /TN " + RecoveryTask +
                    " /SC ONSTART /DELAY 0001:00 /RU SYSTEM /RL HIGHEST /TR \"" + recoveryCommand + "\" /F") != 0)
                    throw new InvalidOperationException("Could not schedule migration recovery.");
                if (Command("sc.exe", "config RSAgent start= disabled") != 0)
                    throw new InvalidOperationException("Could not disable old service for migration.");
                if (Command("sc.exe", "stop RSAgent") != 0)
                    throw new InvalidOperationException("Could not stop old service.");
                using (var service = new ServiceController("RSAgent"))
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(90));
                return 0;
            }
            catch
            {
                try { Command("schtasks.exe", "/Delete /TN " + RecoveryTask + " /F"); } catch { }
                try { Command("sc.exe", "config RSAgent start= auto"); } catch { }
                try { Command("sc.exe", "start RSAgent"); } catch { }
                if (created) try { Directory.Delete(NewData, true); } catch { }
                return 1;
            }
        }

        internal static int Complete()
        {
            try
            {
                if (!File.Exists(Journal) || !File.Exists(Path.Combine(OldData, "config.json")) ||
                    !ServiceRunning("firulaiAgent")) return 1;
                var current = AgentConfig.Load();
                var previous = new System.Web.Script.Serialization.JavaScriptSerializer()
                    .Deserialize<System.Collections.Generic.Dictionary<string, object>>(File.ReadAllText(Path.Combine(OldData, "config.json")));
                if (!string.Equals(current.Uuid, Convert.ToString(previous["uuid"]), StringComparison.OrdinalIgnoreCase)) return 1;
                if (Command("sc.exe", "delete RSAgent") != 0) return 1;
                Command("schtasks.exe", "/Delete /TN " + RecoveryTask + " /F");
                // The old update-worker.exe is still waiting for this installer.
                // A separate helper removes its directory after that worker exits.
                var helper = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "firulaiAgent", "FirulaiAgent.exe");
                var cleanupCommand = "\"" + helper + "\" --cleanup-legacy-migration";
                try
                {
                    Command("schtasks.exe", "/Create /TN " + CleanupTask +
                        " /SC ONSTART /DELAY 0001:00 /RU SYSTEM /RL HIGHEST /TR \"" + cleanupCommand + "\" /F");
                }
                catch { }
                try
                {
                    Process.Start(new ProcessStartInfo(helper, "--cleanup-legacy-migration")
                        { UseShellExecute = false, CreateNoWindow = true });
                }
                catch { /* The boot task retries cleanup after a reboot. */ }
                return 0;
            }
            catch { return 1; }
        }

        internal static int Cleanup()
        {
            if (!File.Exists(Journal)) return 1;
            try { if (!ServiceRunning("firulaiAgent")) return 1; } catch { return 1; }
            try { if (ServiceRunning("RSAgent")) return 1; } catch { }
            for (int attempt = 0; attempt < 120; attempt++)
            {
                try
                {
                    if (Directory.Exists(OldApp)) Directory.Delete(OldApp, true);
                    if (Directory.Exists(OldData)) Directory.Delete(OldData, true);
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Redsauce\RSAgent", false);
                    Registry.LocalMachine.DeleteSubKeyTree(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\RSAgent", false);
                    try { File.Delete(Journal); } catch { }
                    try { File.Delete(RecoveryExe); } catch { }
                    Command("schtasks.exe", "/Delete /TN " + CleanupTask + " /F");
                    return 0;
                }
                catch { System.Threading.Thread.Sleep(2000); }
            }
            return 1;
        }

        internal static int Rollback()
        {
            try
            {
                if (!File.Exists(Journal)) return 0;
                Command("sc.exe", "stop firulaiAgent");
                Command("sc.exe", "delete firulaiAgent");
                if (Command("reg.exe", "import \"" + Journal + "\"") != 0) return 1;
                if (Command("sc.exe", "config RSAgent start= auto") != 0) return 1;
                if (!ServiceRunning("RSAgent") && Command("sc.exe", "start RSAgent") != 0) return 1;
                if (!ServiceRunning("RSAgent")) return 1;
                Command("schtasks.exe", "/Delete /TN " + RecoveryTask + " /F");
                try { Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Redsauce\firulaiAgent", false); } catch { }
                try { Registry.LocalMachine.DeleteSubKeyTree(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\firulaiAgent", false); } catch { }
                // Inno removes its partial new app directory when setup fails.
                try { Directory.Delete(NewData, true); } catch { }
                return 0;
            }
            catch { return 1; }
        }
    }
}
