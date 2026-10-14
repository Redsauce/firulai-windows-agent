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
