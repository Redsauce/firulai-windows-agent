using System;
using System.Collections.Generic;

namespace RsAgent
{
    internal static class AgentReleaseSelection
    {
        internal static string ReadWindowsVersion(object response)
        {
            var versions = new HashSet<string>(StringComparer.Ordinal);
            Collect(response, versions);
            if (versions.Count != 1)
                throw new InvalidOperationException("Missing, invalid or conflicting Windows agent version in RSM.");
            foreach (var version in versions) return version;
            throw new InvalidOperationException("Missing Windows agent version in RSM.");
        }

        private static void Collect(object value, HashSet<string> versions)
        {
            var rows = value as object[];
            if (rows != null)
            {
                foreach (var row in rows) Collect(row, versions);
                return;
            }
            var fields = value as Dictionary<string, object>;
            if (fields == null) return;
            if (fields.ContainsKey("error") || fields.ContainsKey("errors"))
                throw new InvalidOperationException("RSM agent version lookup failed.");
            object version;
            if (fields.TryGetValue("1990", out version) && version != null)
            {
                var tag = Convert.ToString(version).Trim();
                if (tag.Length > 0) { ValidateTag(tag); versions.Add(tag); }
            }
            foreach (var key in new[] { "items", "data", "result" })
            {
                object wrapped;
                if (fields.TryGetValue(key, out wrapped)) Collect(wrapped, versions);
            }
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
