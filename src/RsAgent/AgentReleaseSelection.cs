using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal static class AgentReleaseSelection
    {
        internal const string CatalogUrl = "https://firulai.redsauce.net/api/agents/update-catalog";

        internal static async Task<string> FetchWindowsVersionAsync(HttpMessageHandler handler = null)
        {
            using (var client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                using (var response = await client.GetAsync(CatalogUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > 32768)
                        throw new InvalidOperationException("Oversized update catalog.");
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var data = new System.IO.MemoryStream())
                    using (var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    {
                        var buffer = new byte[4096];
                        int count;
                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                        {
                            if (data.Length + count > 32768) throw new InvalidOperationException("Oversized update catalog.");
                            data.Write(buffer, 0, count);
                        }
                        return ReadWindowsVersion(new JavaScriptSerializer().DeserializeObject(
                            new System.Text.UTF8Encoding(false, true).GetString(data.ToArray())));
                    }
                }
            }
        }

        internal static string ReadWindowsVersion(object response)
        {
            var fields = response as Dictionary<string, object>;
            if (fields == null || fields.Count != 2 || !fields.ContainsKey("windows") || !fields.ContainsKey("linux") ||
                !(fields["windows"] is string) || !(fields["linux"] is string))
                throw new InvalidOperationException("Invalid Firulai update catalog.");
            var tag = ((string)fields["windows"]).Trim();
            ValidateTag(tag);
            ValidateTag(((string)fields["linux"]).Trim());
            return tag;
        }

        internal static string ValidateTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) throw new InvalidOperationException("Missing release tag.");
            var version = tag.StartsWith("v", StringComparison.Ordinal) ? tag.Substring(1) : tag;
            InstallerUpgrade.ParseVersion(version);
            return version;
        }

        internal static string ManifestUrl(string tag)
        {
            ValidateTag(tag);
            return "https://github.com/Redsauce/firulai-windows-agent/releases/download/" + tag + "/agent-update.json";
        }

        internal static void ValidateManifest(string tag, UpdateManifest manifest)
        {
            if (manifest.version != ValidateTag(tag) || manifest.url !=
                "https://github.com/Redsauce/firulai-windows-agent/releases/download/" + tag + "/FirulaiAgent.exe")
                throw new InvalidOperationException("Signed update does not match the release selected by RSM.");
        }
    }
}
