using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RsAgent
{
    internal static class UpgradeTests
    {
        private static int Main()
        {
            Check("0.19.10", "0.19.3", true);
            Check("0.20.0", "0.19.99", true);
            Check("1.0.0", "0.99.99", true);
            Check("0.19.3", "0.19.3", false);
            Check("0.19.2", "0.19.3", false);
            foreach (var invalid in new[] { "", "v0.20.0", "0.20", "0.20.0-beta", "0.20.0.1", "-1.0.0", "2147483648.0.0" })
            {
                bool rejected = false;
                try { InstallerUpgrade.IsNewer(invalid, "0.19.3"); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new Exception("Invalid candidate accepted: " + invalid);
                rejected = false;
                try { InstallerUpgrade.IsNewer("0.20.0", invalid); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new Exception("Unknown installed version accepted: " + invalid);
            }
            SignedManifestTests();
            UpdateCheckLifecycleTests();
            UpdateRetryTests();
            var uninstalled = new System.Collections.Generic.Dictionary<string, object> { { "1751", "Agente desinstalado" }, { "1972", "uncovered" } };
            if (!RsmClient.IsUninstalledRow(uninstalled)) throw new Exception("Confirmed uninstallation rejected");
            uninstalled["1972"] = "covered";
            if (RsmClient.IsUninstalledRow(uninstalled)) throw new Exception("Coverage not released but uninstallation accepted");
            uninstalled["1972"] = "uncovered"; uninstalled["1751"] = "Disconnected";
            if (RsmClient.IsUninstalledRow(uninstalled)) throw new Exception("Disconnected mistaken for uninstalled");
            var versionReport = RsmClient.BuildAgentVersionReport(new AgentConfig {
                uuid = "550e8400-e29b-41d4-a716-446655440000", token = "fixture-token"
            });
            if (versionReport["action"] != "report_version" || versionReport["agent_version"] != AgentConfig.AgentVersion ||
                versionReport["uuid"] != "550e8400-e29b-41d4-a716-446655440000" || versionReport.Count != 6 ||
                versionReport.ContainsKey("packages") || versionReport.ContainsKey("last_scan"))
                throw new Exception("Agent version report contains an incorrect version, identity or inventory fields.");
            if (!new AgentConfig().AutoUpdate || new AgentConfig { auto_update = false }.AutoUpdate)
                throw new Exception("Default update preference or opt-out is incorrect.");
            Console.WriteLine("PASS: numeric upgrade ordering, downgrade rejection, malformed and prerelease versions.");
            return 0;
        }

        private static void Reject(Action action, string name)
        {
            try { action(); }
            catch { return; }
            throw new Exception("Accepted " + name);
        }

        private static void UpdateRetryTests()
        {
            var now = DateTime.UtcNow;
            AgentUpdater.RecordCheckResult(now, false);
            if (AgentUpdater.RetryAtUtc != now.AddMinutes(5))
                throw new Exception("Failed check must retry after five minutes, not 24 hours.");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            using (var service = new RsAgentService())
            using (var inventoryTimer = new System.Timers.Timer(60000))
            {
                var type = typeof(RsAgentService);
                type.GetField("_timer", flags).SetValue(service, inventoryTimer);
                int attempts = 0;
                service.UpdateCheck = () => {
                    attempts++;
                    AgentUpdater.RecordCheckResult(now, attempts > 1);
                    return System.Threading.Tasks.Task.FromResult(false);
                };
                type.GetMethod("ScheduleUpdateRetry", flags).Invoke(service, null);
                if (type.GetField("_updateRetryTimer", flags).GetValue(service) == null)
                    throw new Exception("Failed check has no independent retry timer.");
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var task = (System.Threading.Tasks.Task)type.GetMethod("RetryUpdateAsync", flags).Invoke(service, null);
                    task.GetAwaiter().GetResult();
                    if (!Object.ReferenceEquals(type.GetField("_timer", flags).GetValue(service), inventoryTimer))
                        throw new Exception("Update retry replaced the inventory schedule.");
                    bool retryScheduled = type.GetField("_updateRetryTimer", flags).GetValue(service) != null;
                    if (retryScheduled != (attempt == 0))
                        throw new Exception("Retry must continue after failure and stop after success.");
                }
                AgentUpdater.RecordCheckResult(now, false);
                type.GetMethod("ScheduleUpdateRetry", flags).Invoke(service, null);
                type.GetMethod("StopService", flags).Invoke(service, new object[] { "Isolated retry test stopped." });
                if (type.GetField("_updateRetryTimer", flags).GetValue(service) != null)
                    throw new Exception("Stopping the service did not cancel the update retry.");
                ((System.Threading.Tasks.Task)type.GetMethod("RetryUpdateAsync", flags).Invoke(service, null)).GetAwaiter().GetResult();
                if (attempts != 2 || type.GetField("_updateRetryTimer", flags).GetValue(service) != null)
                    throw new Exception("Stopped service still checks or schedules updates.");
            }
            AgentUpdater.RecordCheckResult(now, true);
            if (AgentUpdater.RetryAtUtc != DateTime.MinValue)
                throw new Exception("Successful check retained a network retry.");
            using (var fired = new System.Threading.ManualResetEvent(false))
            using (var service = new RsAgentService())
            {
                service.UpdateCheck = () => {
                    AgentUpdater.RecordCheckResult(DateTime.UtcNow, true);
                    fired.Set();
                    return System.Threading.Tasks.Task.FromResult(false);
                };
                // Expired retry is accelerated to one second: exercise the real timer.
                AgentUpdater.RecordCheckResult(DateTime.UtcNow.AddMinutes(-5), false);
                typeof(RsAgentService).GetMethod("ScheduleUpdateRetry", flags).Invoke(service, null);
                if (!fired.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new Exception("Retry never ran independently of an inventory execution.");
            }
            using (var resumed = new System.Threading.ManualResetEvent(false))
            using (var service = new RsAgentService())
            {
                service.DailyRunDue = ignored => false;
                service.UpdateCheck = () => {
                    AgentUpdater.RecordCheckResult(DateTime.UtcNow, true);
                    resumed.Set();
                    return System.Threading.Tasks.Task.FromResult(false);
                };
                AgentUpdater.RecordCheckResult(DateTime.UtcNow.AddMinutes(-10), false);
                typeof(RsAgentService).GetMethod("OnPowerEvent", flags).Invoke(service,
                    new object[] { System.ServiceProcess.PowerBroadcastStatus.ResumeAutomatic });
                if (!resumed.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new Exception("Resume lost the update retry when inventory was already sent.");
            }
            Console.WriteLine("PASS: five-minute update retries, repeated failure, inventory timer preservation and stopped service.");
        }

        private static void UpdateCheckLifecycleTests()
        {
            var now = new DateTime(2035, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            AgentUpdater.RecordCheckResult(now.AddDays(-2), true);
            int attempts = 0;
            Func<System.Threading.Tasks.Task<bool>> check = () => {
                attempts++;
                var completion = new System.Threading.Tasks.TaskCompletionSource<bool>();
                if (attempts == 1)
                    completion.SetException(new System.Net.Http.HttpRequestException("Simulated offline startup",
                        new System.Net.WebException("Simulated DNS failure", System.Net.WebExceptionStatus.NameResolutionFailure)));
                else if (attempts == 2)
                    completion.SetException(new System.Threading.Tasks.TaskCanceledException("Simulated download timeout"));
                else completion.SetResult(true);
                return completion.Task;
            };
            Func<bool> run = () => AgentUpdater.CheckAsync(check, () => now).GetAwaiter().GetResult();
            if (run() || attempts != 1 || AgentUpdater.RetryAtUtc != now.AddMinutes(5))
                throw new Exception("Offline startup did not schedule the short retry.");
            now = now.AddMinutes(4).AddSeconds(59);
            if (run() || attempts != 1) throw new Exception("Retry bypassed backoff.");
            now = now.AddSeconds(1);
            if (run() || attempts != 2 || AgentUpdater.RetryAtUtc != now.AddMinutes(5))
                throw new Exception("Timeout stopped further short retries.");
            now = now.AddMinutes(5);
            if (!run() || attempts != 3 || AgentUpdater.RetryAtUtc != DateTime.MinValue)
                throw new Exception("Restored network did not allow launching the update.");
            now = now.AddHours(23);
            if (run() || attempts != 3) throw new Exception("Successful checks ignored the normal cooldown.");
            now = now.AddHours(1);
            if (!run() || attempts != 4) throw new Exception("Cooldown never expired.");

            // Two overlapping checks must not download or launch two installers.
            AgentUpdater.RecordCheckResult(now.AddDays(-2), true);
            var blocked = new System.Threading.Tasks.TaskCompletionSource<bool>();
            int overlappingCalls = 0;
            Func<System.Threading.Tasks.Task<bool>> slowCheck = () => { overlappingCalls++; return blocked.Task; };
            var first = AgentUpdater.CheckAsync(slowCheck, () => now);
            var second = AgentUpdater.CheckAsync(slowCheck, () => now);
            if (overlappingCalls != 1) throw new Exception("Update checks overlapped.");
            blocked.SetResult(true);
            if (!first.GetAwaiter().GetResult() || second.GetAwaiter().GetResult() || overlappingCalls != 1)
                throw new Exception("Concurrent requests launched duplicate updates.");
            AgentUpdater.RecordCheckResult(DateTime.UtcNow.AddDays(-2), true);
            Console.WriteLine("PASS: offline startup, DNS failure, download timeout, online recovery, cooldown and concurrent checks.");
        }

        private static void SignedManifestTests()
        {
            using (var rsa = new RSACryptoServiceProvider(3072, new CspParameters { ProviderType = 24 }))
            {
                rsa.PersistKeyInCsp = false;
                string key = rsa.ToXmlString(false);
                var package = Encoding.UTF8.GetBytes("fixture installer contents");
                string digest;
                using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(package)).Replace("-", "");
                var json = Encoding.UTF8.GetBytes("{\"version\":\"0.20.1\",\"url\":\"https://github.com/Redsauce/firulai-windows-agent/releases/download/0.20.1/FirulaiAgent.exe\",\"sha256\":\"" + digest + "\",\"size\":" + package.Length + "}");
                Func<byte[], string> sign = bytes => Convert.ToBase64String(rsa.SignData(bytes, CryptoConfig.MapNameToOID("SHA256")));
                string signature = sign(json);
                var manifest = UpdateManifest.Verify(json, signature, key);
                var fixture = Path.GetTempFileName();
                try
                {
                    File.WriteAllBytes(fixture, package);
                    manifest.VerifyPackage(fixture);
                    package[0] ^= 1;
                    File.WriteAllBytes(fixture, package);
                    Reject(() => manifest.VerifyPackage(fixture), "same-size altered package");
                    File.WriteAllText(fixture, "short");
                    Reject(() => manifest.VerifyPackage(fixture), "wrong-size package");
                }
                finally { File.Delete(fixture); }
                Reject(() => UpdateManifest.Verify(json, signature, ""), "missing trust key");
                var altered = (byte[])json.Clone();
                altered[12] ^= 1;
                Reject(() => UpdateManifest.Verify(altered, signature, key), "altered signed manifest");
                using (var wrong = new RSACryptoServiceProvider(3072, new CspParameters { ProviderType = 24 }))
                {
                    wrong.PersistKeyInCsp = false;
                    Reject(() => UpdateManifest.Verify(json, signature, wrong.ToXmlString(false)), "wrong signing key");
                }
                var external = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(json).Replace("github.com", "evil.invalid"));
                Reject(() => UpdateManifest.Verify(external, sign(external), key), "signed foreign destination");
                var latest = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(json).Replace("download/0.20.1", "latest/download"));
                Reject(() => UpdateManifest.Verify(latest, sign(latest), key), "unversioned package URL");
                var prerelease = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(json).Replace("0.20.1", "0.20.1-beta"));
                Reject(() => UpdateManifest.Verify(prerelease, sign(prerelease), key), "signed prerelease");
                Reject(() => UpdateManifest.Verify(json, "not base64", key), "malformed signature");
                Console.WriteLine("PASS: signed packages, tampering, wrong trust key, size, destination and prerelease rejection.");
            }
        }

        private static void Check(string candidate, string installed, bool expected)
        {
            if (InstallerUpgrade.IsNewer(candidate, installed) != expected)
                throw new Exception(candidate + " compared to " + installed);
        }
    }
}
