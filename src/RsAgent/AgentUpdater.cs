using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace RsAgent
{
    internal static class AgentUpdater
    {
        private static DateTime nextCheckUtc;
        internal static DateTime NextCheckUtc { get { return nextCheckUtc; } }
        internal static DateTime RetryAtUtc { get; private set; }
        private static int consecutiveFailures;
        private static readonly int HostJitterSeconds = GetHostJitterSeconds();

        private static int GetHostJitterSeconds()
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Environment.MachineName));
                return (bytes[0] * 256 + bytes[1]) % 61;
            }
        }

        internal static void RecordCheckResult(DateTime nowUtc, bool succeeded, int? jitterSeconds = null)
        {
            consecutiveFailures = succeeded ? 0 : Math.Min(consecutiveFailures + 1, 4);
            var minutes = succeeded || consecutiveFailures >= 4 ? 60 :
                consecutiveFailures == 1 ? 5 : consecutiveFailures == 2 ? 10 : 20;
            nextCheckUtc = nowUtc.AddMinutes(minutes).AddSeconds(jitterSeconds ?? HostJitterSeconds);
            RetryAtUtc = succeeded ? DateTime.MinValue : nextCheckUtc;
        }
        private static readonly string Stage = Path.Combine(AgentConfig.DataDir, "updates");

        private static void SecureStage()
        {
            if ((File.GetAttributes(AgentConfig.DataDir) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Update data directory is a reparse point.");
            Directory.CreateDirectory(Stage);
            if ((File.GetAttributes(Stage) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Update staging directory is a reparse point.");
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { "S-1-5-18", "S-1-5-32-544" })
                acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid), FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(Stage, acl);
            foreach (var name in new[] { "FirulaiAgent.exe", "agent-update.json", "agent-update.json.sig", "update-worker.exe", "attempted-version.txt", "installer.log" })
            {
                var path = Path.Combine(Stage, name);
                if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Update staging file is a reparse point.");
            }
        }

        private static string PublicKey()
        {
            using (var stream = typeof(AgentUpdater).Assembly.GetManifestResourceStream("RsAgent.UpdatePublicKey.xml"))
            {
                if (stream == null) return "";
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd().Trim();
            }
        }

        private static async Task<byte[]> DownloadAsync(HttpClient client, string url, long limit)
        {
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                if (response.RequestMessage.RequestUri.Scheme != "https") throw new InvalidOperationException("Insecure download redirect.");
                if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > limit)
                    throw new InvalidOperationException("Download too large.");
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[8192];
                    int count;
                    using (var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60)))
                    {
                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                        {
                            if (output.Length + count > limit) throw new InvalidOperationException("Download too large.");
                            output.Write(buffer, 0, count);
                        }
                    }
                    return output.ToArray();
                }
            }
        }

        private static readonly System.Threading.SemaphoreSlim CheckLock = new System.Threading.SemaphoreSlim(1, 1);

        internal static Task<bool> CheckAsync()
        {
            return CheckAsync(CheckForUpdateAsync, () => DateTime.UtcNow);
        }

        internal static async Task<bool> CheckAsync(Func<Task<bool>> check, Func<DateTime> utcNow, int? jitterSeconds = null)
        {
            await CheckLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (utcNow() < nextCheckUtc) return false;
                try
                {
                    var launched = await check().ConfigureAwait(false);
                    RecordCheckResult(utcNow(), true, jitterSeconds);
                    Logger.Info("Automatic update check completed. Next check at " + NextCheckUtc.ToLocalTime().ToString("o") + ".");
                    return launched;
                }
                catch (Exception ex)
                {
                    RecordCheckResult(utcNow(), false, jitterSeconds);
                    var networkError = ex.InnerException as System.Net.WebException;
                    Logger.Warn("Automatic update check failed; inventory continues. " + ex.GetType().Name +
                        (networkError == null ? "" : " NetworkStatus=" + networkError.Status) +
                        ". Reason=" + ex.Message.Replace('\r', ' ').Replace('\n', ' ') +
                        ". Update retry at " + RetryAtUtc.ToLocalTime().ToString("o") + ".");
                    return false;
                }
            }
            finally { CheckLock.Release(); }
        }

        private static async Task<bool> CheckForUpdateAsync()
        {
            var config = AgentConfig.Load();
            if (!config.AutoUpdate)
            {
                Logger.Info("Automatic updates disabled by local configuration.");
                return false;
            }
            if (InstallerUpgrade.RecoveryPending()) throw new InvalidOperationException("Upgrade recovery is pending.");
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            var publicKey = PublicKey();
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                Logger.Warn("Automatic updates unavailable: this build has no trusted release key.");
                return false;
            }
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
            {
                var releaseTag = await AgentReleaseSelection.FetchWindowsVersionAsync().ConfigureAwait(false);
                if (!InstallerUpgrade.IsNewer(AgentReleaseSelection.ValidateTag(releaseTag), AgentConfig.AgentVersion))
                    return false;
                var manifestUrl = AgentReleaseSelection.ManifestUrl(releaseTag);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Firulai-Windows-Agent/" + AgentConfig.AgentVersion);
                var json = await DownloadAsync(client, manifestUrl, 32768).ConfigureAwait(false);
                var signature = System.Text.Encoding.ASCII.GetString(await DownloadAsync(client, manifestUrl + ".sig", 8192).ConfigureAwait(false));
                var manifest = UpdateManifest.Verify(json, signature, publicKey);
                AgentReleaseSelection.ValidateManifest(releaseTag, manifest);
                if (!InstallerUpgrade.IsNewer(manifest.version, AgentConfig.AgentVersion))
                    return false;
                SecureStage();
                var attempted = Path.Combine(Stage, "attempted-version.txt");
                if (File.Exists(attempted) && File.ReadAllText(attempted).Trim() == manifest.version)
                {
                    Logger.Warn("Automatic update " + manifest.version + " was already attempted; automatic reinstall suppressed. See updates/installer.log.");
                    return false;
                }
                File.WriteAllBytes(Path.Combine(Stage, "FirulaiAgent.exe"), await DownloadAsync(client, manifest.url, manifest.size).ConfigureAwait(false));
                manifest.VerifyPackage(Path.Combine(Stage, "FirulaiAgent.exe"));
                File.WriteAllBytes(Path.Combine(Stage, "agent-update.json"), json);
                File.WriteAllText(Path.Combine(Stage, "agent-update.json.sig"), signature);
                var helper = Path.Combine(Stage, "update-worker.exe");
                File.Copy(typeof(AgentUpdater).Assembly.Location, helper, true);
                using (var process = Process.Start(new ProcessStartInfo(helper, "--apply-update") { UseShellExecute = false, CreateNoWindow = true }))
                {
                    if (process == null) throw new InvalidOperationException("Could not launch update worker.");
                }
                Logger.Info("Launched signed agent update " + manifest.version + ".");
                return true;
            }
        }

        internal static int Apply()
        {
            try
            {
                using (var mutex = new System.Threading.Mutex(false, @"Global\FirulaiAgentUpdateWorker"))
                {
                    bool acquired;
                    try { acquired = mutex.WaitOne(0); }
                    catch (System.Threading.AbandonedMutexException) { acquired = true; }
                    if (!acquired) return 1;
                    try
                    {
                        if (!AgentConfig.Load().AutoUpdate || InstallerUpgrade.RecoveryPending()) return 1;
                        SecureStage();
                        var manifest = UpdateManifest.Verify(File.ReadAllBytes(Path.Combine(Stage, "agent-update.json")),
                            File.ReadAllText(Path.Combine(Stage, "agent-update.json.sig")), PublicKey());
                        if (!InstallerUpgrade.IsNewer(manifest.version, AgentConfig.AgentVersion)) return 1;
                        var package = Path.Combine(Stage, "FirulaiAgent.exe");
                        manifest.VerifyPackage(package);
                        // Persist before execution, so service restarts cannot cause an update loop.
                        File.WriteAllText(Path.Combine(Stage, "attempted-version.txt"), manifest.version);
                        using (var process = Process.Start(new ProcessStartInfo(package,
                            "/SP- /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /ACCEPTLICENSE=yes /LOG=\"" + Path.Combine(Stage, "installer.log") + "\"") {
                            UseShellExecute = false, CreateNoWindow = true
                        }))
                        {
                            if (!process.WaitForExit(10 * 60 * 1000))
                            {
                                Logger.Warn("Update installer is still running; startup recovery remains available if installation cannot finish.");
                                return 1;
                            }
                            if (process.ExitCode != 0)
                            {
                                Logger.Warn("Automatic update installer failed with code " + process.ExitCode + ".");
                                InstallerUpgrade.Recover();
                                return 1;
                            }
                        }
                        Logger.Info("Automatic update installer completed for " + manifest.version + ".");
                        return 0;
                    }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Automatic update worker failed.", ex);
                return 1;
            }
        }
    }
}
