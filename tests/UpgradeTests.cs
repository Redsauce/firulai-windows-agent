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
            ReleaseSelectionTests();
            var missingSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            if (!RsmClient.IsVerifiedMissingResponse(missingSerializer.DeserializeObject("{\"missing\":true,\"verified\":true}")))
                throw new Exception("Verified missing System rejected");
            foreach (var invalidMissing in new[] { "{}", "[]", "null", "{\"error\":\"denied\"}",
                "{\"missing\":true}", "{\"missing\":false,\"verified\":true}",
                "{\"missing\":true,\"verified\":false}", "{\"missing\":\"true\",\"verified\":true}",
                "{\"missing\":true,\"verified\":true,\"error\":\"denied\"}" })
                if (RsmClient.IsVerifiedMissingResponse(missingSerializer.DeserializeObject(invalidMissing)))
                    throw new Exception("Unverified missing System accepted: " + invalidMissing);
            Console.WriteLine("PASS: missing System requires independent authenticated confirmation; empty, denied and malformed responses rejected.");
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

        private sealed class VersionResponseHandler : System.Net.Http.HttpMessageHandler
        {
            internal string Response = "{\"windows\":\"0.20.7\",\"linux\":\"0.4.7\"}";
            internal System.Net.HttpStatusCode Status = System.Net.HttpStatusCode.OK;
            protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(
                System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancel)
            {
                if (request.Method != System.Net.Http.HttpMethod.Get ||
                    request.RequestUri.AbsoluteUri != AgentReleaseSelection.CatalogUrl ||
                    request.Headers.Contains("Authorization") || request.Content != null)
                    throw new Exception("Incorrect RSM version lookup request.");
                return System.Threading.Tasks.Task.FromResult(new System.Net.Http.HttpResponseMessage(Status) {
                    Content = new System.Net.Http.StringContent(Response)
                });
            }
        }

        private static void ReleaseSelectionTests()
        {
            var config = new AgentConfig { token = "test-token" };
            const string endpoint = "https://example.test/AppController/commands_RSM/api/api.php";
            var tag = FetchWindowsAgentVersionAsync(config, new VersionResponseHandler(), endpoint).GetAwaiter().GetResult();
            if (tag != "0.20.7" || AgentReleaseSelection.ManifestUrl(tag) !=
                "https://github.com/Redsauce/firulai-windows-agent/releases/download/0.20.7/agent-update.json")
                throw new Exception("RSM version did not select a pinned manifest.");
            foreach (var response in new[] {
                "not-json", "[]", "{\"error\":\"denied\"}", "[{\"windows\":\"../latest\"}]",
                "[{\"windows\":\"0.20.7-beta\"}]", "[{\"windows\":\"0.20.7\"},{\"windows\":\"0.20.8\"}]"
            })
                Reject(() => FetchWindowsAgentVersionAsync(config,
                    new VersionResponseHandler { Response = response }, endpoint).GetAwaiter().GetResult(), "invalid RSM selection");
            Reject(() => FetchWindowsAgentVersionAsync(config,
                new VersionResponseHandler { Status = System.Net.HttpStatusCode.Forbidden }, endpoint).GetAwaiter().GetResult(), "RSM permission denial");
            Reject(() => FetchWindowsAgentVersionAsync(config,
                new VersionResponseHandler { Status = System.Net.HttpStatusCode.ServiceUnavailable }, endpoint).GetAwaiter().GetResult(), "RSM outage");
            if (AgentReleaseSelection.ReadWindowsVersion(new System.Web.Script.Serialization.JavaScriptSerializer()
                .DeserializeObject("{\"windows\":\" v0.20.7 \",\"linux\":\"0.4.7\"}")) != "v0.20.7")
                throw new Exception("Version tag prefix was lost.");
            var manifest = new UpdateManifest { version = "0.20.7", url =
                "https://github.com/Redsauce/firulai-windows-agent/releases/download/v0.20.7/FirulaiAgent.exe" };
            AgentReleaseSelection.ValidateManifest("v0.20.7", manifest);
            Reject(() => AgentReleaseSelection.ValidateManifest("0.20.7", manifest), "different release tag");
            Reject(() => AgentReleaseSelection.ValidateManifest("0.20.8", manifest), "different release version");
            Console.WriteLine("PASS: public Firulai catalog lookup, pinned release, invalid responses and manifest mismatch rejection.");
        }

        private static async System.Threading.Tasks.Task<string> FetchWindowsAgentVersionAsync(
            AgentConfig config, System.Net.Http.HttpMessageHandler handler, string endpoint)
        {
            return await AgentReleaseSelection.FetchWindowsVersionAsync(handler);
        }

        private static void RecordCheckResult(DateTime now, bool succeeded)
        {
            AgentUpdater.RecordCheckResult(now, succeeded, 0);
        }

        private static void UpdateRetryTests()
        {
            var now = DateTime.UtcNow;
            RecordCheckResult(now, true);
            foreach (var minutes in new[] { 5, 10, 20, 60, 60 })
            {
                RecordCheckResult(now, false);
                if (AgentUpdater.NextCheckUtc != now.AddMinutes(minutes))
                    throw new Exception("Incorrect progressive update retry interval.");
            }
            RecordCheckResult(now, true);
            if (AgentUpdater.NextCheckUtc != now.AddHours(1))
                throw new Exception("Successful lookup must schedule the next hourly check.");
            AgentUpdater.RecordCheckResult(now, true, 45);
            if (AgentUpdater.NextCheckUtc != now.AddHours(1).AddSeconds(45))
                throw new Exception("Host jitter was not included in the next check.");
            AgentUpdater.RecordCheckResult(now, true);
            if (AgentUpdater.NextCheckUtc < now.AddHours(1) || AgentUpdater.NextCheckUtc > now.AddHours(1).AddMinutes(1))
                throw new Exception("Production host jitter is outside the allowed range.");
            RecordCheckResult(now, true);
            RecordCheckResult(now, false);
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
                    RecordCheckResult(now, attempts > 1);
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
                    if (!retryScheduled)
                        throw new Exception("Independent update polling must continue after failure and success.");
                }
                RecordCheckResult(now, false);
                type.GetMethod("ScheduleUpdateRetry", flags).Invoke(service, null);
                type.GetMethod("StopService", flags).Invoke(service, new object[] { "Isolated retry test stopped." });
                if (type.GetField("_updateRetryTimer", flags).GetValue(service) != null)
                    throw new Exception("Stopping the service did not cancel the update retry.");
                ((System.Threading.Tasks.Task)type.GetMethod("RetryUpdateAsync", flags).Invoke(service, null)).GetAwaiter().GetResult();
                if (attempts != 2 || type.GetField("_updateRetryTimer", flags).GetValue(service) != null)
                    throw new Exception("Stopped service still checks or schedules updates.");
            }
            RecordCheckResult(now, true);
            if (AgentUpdater.RetryAtUtc != DateTime.MinValue)
                throw new Exception("Successful check retained a network retry.");
            using (var fired = new System.Threading.ManualResetEvent(false))
            using (var service = new RsAgentService())
            {
                service.UpdateCheck = () => {
                    RecordCheckResult(DateTime.UtcNow, true);
                    fired.Set();
                    return System.Threading.Tasks.Task.FromResult(false);
                };
                // A successful check must also trigger the next poll without inventory.
                // Expired poll is accelerated to one second: exercise the real timer.
                RecordCheckResult(DateTime.UtcNow.AddHours(-2), true);
                typeof(RsAgentService).GetMethod("ScheduleUpdateRetry", flags).Invoke(service, null);
                if (!fired.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new Exception("Successful check never polled again independently of inventory.");
            }
            using (var resumed = new System.Threading.ManualResetEvent(false))
            using (var service = new RsAgentService())
            {
                service.DailyRunDue = ignored => false;
                service.UpdateCheck = () => {
                    RecordCheckResult(DateTime.UtcNow, true);
                    resumed.Set();
                    return System.Threading.Tasks.Task.FromResult(false);
                };
                RecordCheckResult(DateTime.UtcNow.AddHours(-2), true);
                typeof(RsAgentService).GetMethod("OnPowerEvent", flags).Invoke(service,
                    new object[] { System.ServiceProcess.PowerBroadcastStatus.ResumeAutomatic });
                if (!resumed.WaitOne(TimeSpan.FromSeconds(10)))
                    throw new Exception("Resume lost the update retry when inventory was already sent.");
            }
            using (var service = new RsAgentService())
            {
                var type = typeof(RsAgentService);
                RecordCheckResult(DateTime.UtcNow, true);
                service.UpdateCheck = () => System.Threading.Tasks.Task.FromResult(false);
                service.InventoryRun = trigger => {
                    var failed = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    failed.SetException(new System.Net.Http.HttpRequestException("Simulated inventory upload failure"));
                    return failed.Task;
                };
                var nextUpdate = AgentUpdater.NextCheckUtc;
                var before = DateTime.Now;
                ((System.Threading.Tasks.Task)type.GetMethod("ExecuteManagedRunAsync", flags).Invoke(service,
                    new object[] { "isolated-upload-failure", before.AddDays(1) })).GetAwaiter().GetResult();
                var nextInventory = (DateTime)type.GetField("_nextRunAtLocal", flags).GetValue(service);
                if (nextInventory < before.AddMinutes(30) || nextInventory > DateTime.Now.AddMinutes(30) ||
                    AgentUpdater.NextCheckUtc != nextUpdate || type.GetField("_updateRetryTimer", flags).GetValue(service) == null)
                    throw new Exception("Failed inventory did not retain its 30-minute retry and independent hourly update timer.");
                type.GetMethod("StopService", flags).Invoke(service, new object[] { "Isolated upload test stopped." });
            }
            Console.WriteLine("PASS: hourly polling, 5/10/20/60-minute retries, host jitter, inventory timer preservation and stopped service.");
        }

        private static void UpdateCheckLifecycleTests()
        {
            var now = new DateTime(2035, 1, 1, 8, 0, 0, DateTimeKind.Utc);
            RecordCheckResult(now.AddDays(-2), true);
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
            Func<bool> run = () => AgentUpdater.CheckAsync(check, () => now, 0).GetAwaiter().GetResult();
            if (run() || attempts != 1 || AgentUpdater.RetryAtUtc != now.AddMinutes(5))
                throw new Exception("Offline startup did not schedule the short retry.");
            now = now.AddMinutes(4).AddSeconds(59);
            if (run() || attempts != 1) throw new Exception("Retry bypassed backoff.");
            now = now.AddSeconds(1);
            if (run() || attempts != 2 || AgentUpdater.RetryAtUtc != now.AddMinutes(10))
                throw new Exception("Timeout stopped further short retries.");
            now = now.AddMinutes(10);
            if (!run() || attempts != 3 || AgentUpdater.RetryAtUtc != DateTime.MinValue)
                throw new Exception("Restored network did not allow launching the update.");
            now = now.AddMinutes(59).AddSeconds(59);
            if (run() || attempts != 3) throw new Exception("Successful checks ignored the normal cooldown.");
            now = now.AddSeconds(1);
            if (!run() || attempts != 4) throw new Exception("Cooldown never expired.");

            // Two overlapping checks must not download or launch two installers.
            RecordCheckResult(now.AddDays(-2), true);
            var blocked = new System.Threading.Tasks.TaskCompletionSource<bool>();
            int overlappingCalls = 0;
            Func<System.Threading.Tasks.Task<bool>> slowCheck = () => { overlappingCalls++; return blocked.Task; };
            var first = AgentUpdater.CheckAsync(slowCheck, () => now, 0);
            var second = AgentUpdater.CheckAsync(slowCheck, () => now, 0);
            if (overlappingCalls != 1) throw new Exception("Update checks overlapped.");
            blocked.SetResult(true);
            if (!first.GetAwaiter().GetResult() || second.GetAwaiter().GetResult() || overlappingCalls != 1)
                throw new Exception("Concurrent requests launched duplicate updates.");
            RecordCheckResult(DateTime.UtcNow.AddDays(-2), true);
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
