using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace RsAgent
{
    internal static class InventoryFragmentTests
    {
        private static int checks;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        public static void Main(string[] args)
        {
            var root = Path.Combine(Path.GetTempPath(), "firulai-fragments-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            InventoryUpload.TestSpoolDirectory = root;
            var serializer = InventoryUpload.Serializer();
            var bytes = new byte[10 * 1024 * 1024];
            new Random(2026).NextBytes(bytes);
            var config = new AgentConfig { token = "fragment-test-token", uuid = "fragment-test-system" };
            var document = new Dictionary<string, object> {
                {"RStoken", config.Token},
                {"system", new Dictionary<string, object> { {"uuid", config.Uuid}, {"hostname", "Synthetic"}, {"fqdn", "synthetic.invalid"}, {"os", new Dictionary<string, object>()}, {"collected_at", "2026-11-24 10:00:00"} }},
                {"components", new object[] {new Dictionary<string, object> { {"name", "Unicode-\u6f22\u5b57-\u00f1"}, {"version", "1.2.0 (2025-10-27)"}, {"evidence", Convert.ToBase64String(bytes)} }}},
                {"packages", new object[0]}
            };
            var json = serializer.Serialize(document);
            var first = new List<string>();
            try
            {
                bool failed = false;
                try
                {
                    InventoryUpload.SendAsync(config, json, payload => {
                        first.Add(payload);
                        if (first.Count == 2) throw new IOException("Simulated lost response after delivery");
                        return Task.FromResult(0);
                    }).GetAwaiter().GetResult();
                }
                catch (IOException) { failed = true; }
                Check(failed && first.Count == 2, "Connection interruption is observable.");
                Check(Directory.GetFiles(root, "*.pending").Length == 1, "Pending compressed snapshot survives an uncertain send.");
                var replay = new List<string>();
                InventoryUpload.SendAsync(config, json, payload => { replay.Add(payload); return Task.FromResult(0); }).GetAwaiter().GetResult();
                Check(replay.Count >= 3, "High entropy inventory actually spans multiple requests.");
                Check(first[0] == replay[0] && first[1] == replay[1], "Retry sends identical upload IDs and bytes, including possibly delivered parts.");
                Check(Directory.GetFiles(root, "*.pending").Length == 0, "Queued complete send clears local pending snapshot.");
                using (var compressed = new MemoryStream())
                {
                    for (var index = 0; index < replay.Count; index++)
                    {
                        var frame = serializer.Deserialize<Dictionary<string, object>>(replay[index]);
                        var part = Convert.FromBase64String((string)frame["data"]);
                        Check(Encoding.UTF8.GetByteCount(replay[index]) < 6 * 1024 * 1024, "Encoded frame leaves room for multipart overhead below 7 MiB.");
                        Check(Convert.ToInt32(frame["part_index"]) == index, "Part index is stable.");
                        Check(Convert.ToInt32(frame["total_parts"]) == replay.Count, "Part count is complete.");
                        Check(InventoryUpload.Hash(part) == (string)frame["part_sha256"], "Each transmitted part has the correct hash.");
                        compressed.Write(part, 0, part.Length);
                    }
                    compressed.Position = 0;
                    using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                    using (var raw = new MemoryStream())
                    {
                        gzip.CopyTo(raw);
                        Check(Encoding.UTF8.GetString(raw.ToArray()) == json, "All fields, tokens, Unicode and versions reconstruct exactly.");
                    }
                }
                if (args.Length == 2 && args[0] == "--export")
                    File.WriteAllText(args[1], serializer.Serialize(new Dictionary<string, object> { {"frames", replay}, {"expected_sha256", InventoryUpload.Hash(Encoding.UTF8.GetBytes(json))} }), new UTF8Encoding(false));
                Console.WriteLine("Inventory fragments: " + checks + " checks passed (no HTTP or service changes).");
            }
            finally
            {
                // Every file is a direct child of the uniquely created fixture root.
                foreach (var file in Directory.GetFiles(root)) File.Delete(file);
                Directory.Delete(root);
            }
        }
    }
}
