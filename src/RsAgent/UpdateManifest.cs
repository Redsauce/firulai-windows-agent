using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal sealed class UpdateManifest
    {
        public string version { get; set; }
        public string url { get; set; }
        public string sha256 { get; set; }
        public long size { get; set; }

        internal static UpdateManifest Verify(byte[] json, string signature, string publicKey)
        {
            if (json == null || json.Length == 0 || json.Length > 32768 || string.IsNullOrWhiteSpace(publicKey))
                throw new InvalidOperationException("Missing or oversized signed update manifest.");
            using (var rsa = new RSACryptoServiceProvider(new CspParameters { ProviderType = 24 }))
            {
                rsa.PersistKeyInCsp = false;
                rsa.FromXmlString(publicKey);
                if (rsa.KeySize < 3072 || !rsa.VerifyData(json, CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(signature)))
                    throw new InvalidOperationException("Invalid update signature.");
            }
            var manifest = new JavaScriptSerializer().Deserialize<UpdateManifest>(new UTF8Encoding(false, true).GetString(json));
            if (manifest == null) throw new InvalidOperationException("Missing manifest.");
            InstallerUpgrade.ParseVersion(manifest.version);
            Uri uri;
            if (!Uri.TryCreate(manifest.url, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                uri.Host != "github.com" || uri.UserInfo.Length != 0 || !uri.IsDefaultPort || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                (uri.AbsolutePath != "/Redsauce/firulai-windows-agent/releases/download/" + manifest.version + "/FirulaiAgent.exe" &&
                 uri.AbsolutePath != "/Redsauce/firulai-windows-agent/releases/download/v" + manifest.version + "/FirulaiAgent.exe"))
                throw new InvalidOperationException("Unexpected update download destination.");
            if (manifest.sha256 == null || !System.Text.RegularExpressions.Regex.IsMatch(manifest.sha256, "^[a-fA-F0-9]{64}$") ||
                manifest.size <= 0 || manifest.size > 100L * 1024 * 1024)
                throw new InvalidOperationException("Invalid update size or hash.");
            return manifest;
        }

        internal void VerifyPackage(string path)
        {
            if (new FileInfo(path).Length != size) throw new InvalidOperationException("Update size mismatch.");
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                if (!string.Equals(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", ""), sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Update hash mismatch.");
        }
    }
}
