using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;
using System.Reflection;

[assembly: AssemblyProduct("Synthetic metadata suite")]
[assembly: AssemblyCompany("Synthetic publisher")]
[assembly: AssemblyFileVersion("1.2.3.4")]

namespace RsAgent
{
    internal static class LargeInventoryTests
    {
        private static int passed;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            passed++;
        }

        private static void Inventory(int characters)
        {
            var config = new AgentConfig { token = "fake-upload-token", uuid = "550e8400-e29b-41d4-a716-446655440000" };
            var serializer = InventoryCollector.CreateInventorySerializer();
            var payload = new Dictionary<string, object> {
                {"system", new Dictionary<string, object> { {"uuid", config.Uuid}, {"agent_version", "0.19.1"} }},
                {"components", new object[] { new Dictionary<string, object> { {"name", new string('x', characters)}, {"version", "1.2.3"} } }},
                {"packages", new object[] { new Dictionary<string, object> { {"name", "Aplicación 日本語 中文"}, {"version", "2.0"} } }},
                {"RStoken", "obsolete-token"},
                {"extra", new Dictionary<string, object> { {"enabled", true}, {"optional", null}, {"count", 42} }}
            };
            var json = serializer.Serialize(payload);
            ApiEndpoint.Calls = 0;
            ApiEndpoint.Status = 200;
            RsmClient.SendAsync(config, json).GetAwaiter().GetResult();
            Check(ApiEndpoint.Calls == 1, "Upload should reach transport exactly once.");
            Check(ApiEndpoint.Trigger == "newServerData", "Inventory event unchanged.");
            var actual = serializer.Deserialize<Dictionary<string, object>>(ApiEndpoint.Json);
            payload["RStoken"] = config.Token;
            Check(serializer.Serialize(actual) == serializer.Serialize(payload), "All nested fields, Unicode, arrays and upload token must survive.");
            Check((string)actual["RStoken"] == config.Token, "Configured token must replace an old payload token.");
            if (characters > 2097152) {
                bool rejected = false;
                try { new JavaScriptSerializer().DeserializeObject(json); }
                catch (ArgumentException) { rejected = true; }
                Check(rejected, "Control serializer must reproduce the former size failure.");
            }
        }

        private static void FileEvidence()
        {
            var directory = Path.Combine(Path.GetTempPath(), "rsagent-file-evidence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var first = Path.Combine(directory, "first.dll");
            var second = Path.Combine(directory, "second.dll");
            try
            {
                File.Copy(Assembly.GetExecutingAssembly().Location, first);
                File.Copy(Assembly.GetExecutingAssembly().Location, second);
                var collect = typeof(InventoryCollector).GetMethod("CollectApplicationLibraries", BindingFlags.Static | BindingFlags.NonPublic);
                var files = new List<Dictionary<string, string>>((IEnumerable<Dictionary<string, string>>)collect.Invoke(null, new object[] { directory }));
                Check(files.Count == 2, "Both physical file observations must survive.");
                Check(files.TrueForAll(item => item["product_name"] == "Synthetic metadata suite"), "ProductName must come from actual file metadata.");
                Check(files.TrueForAll(item => item["company_name"] == "Synthetic publisher"), "Publisher evidence must survive.");
                Check(files.TrueForAll(item => item["version"] == "1.2.3.4"), "Observed version must survive without a parent substitution.");
                Check(files.TrueForAll(item => item["version_source"] == "ProductVersion" && item.ContainsKey("file_version")), "Both raw versions and the selected origin must survive.");
                var components = new List<Dictionary<string, string>>();
                var seen = new HashSet<string>();
                var add = typeof(InventoryCollector).GetMethod("AddComponent", BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var file in files)
                    add.Invoke(null, new object[] { components, seen, file["name"], file["version"], "file", null, file });
                Check(components.Count == 2, "Same metadata at distinct paths must not be discarded by deduplication.");
                Check(components.TrueForAll(item => Path.IsPathRooted(item["source_path"])), "Physical provenance must remain available in the upload payload.");
            }
            finally
            {
                // Only the exact two fixture files and empty fixture directory.
                if (File.Exists(first)) File.Delete(first);
                if (File.Exists(second)) File.Delete(second);
                Directory.Delete(directory);
            }
        }

        public static int Main()
        {
            FileEvidence();
            string selectedSource;
            Check(InventoryCollector.SelectFileMetadataVersion("Suite Extensions", "2.3.4.5", out selectedSource) == "2.3.4.5" && selectedSource == "FileVersion", "Text ProductVersion must not hide a valid FileVersion.");
            Check(InventoryCollector.SelectFileMetadataVersion("1, 2, 3, 4", "9.0", out selectedSource) == "1.2.3.4" && selectedSource == "ProductVersion", "Numeric resource tuples preserve all fields.");
            Check(InventoryCollector.SelectFileMetadataVersion("1.2.0 (2025-10-27)", "9.0", out selectedSource) == "1.2.0 (2025-10-27)", "Version metadata must survive.");
            Check(InventoryCollector.SelectFileMetadataVersion("Extensions", "Library", out selectedSource) == "" && selectedSource == "", "No invented version for descriptions.");

            Check(InventoryCollector.NormalizeFileMetadataVersion("\x0e") == "", "Invalid ProductVersion must allow FileVersion fallback.");
            Check(InventoryCollector.NormalizeFileMetadataVersion("1.\x0e" + "2.3") == "1.2.3", "XML-invalid controls removed from file metadata.");
            Check(InventoryCollector.NormalizeFileMetadataVersion("1.2.3+abcdef1234567") == "1.2.3+abcdef1234567", "Valid version metadata preserved.");

            Inventory(128);
            Inventory(2097000);
            Inventory(2100000);
            Inventory(8 * 1024 * 1024);
            var serializer = InventoryCollector.CreateInventorySerializer();
            var records = new List<Dictionary<string, object>>();
            for (int i = 0; i < 15000; i++) records.Add(new Dictionary<string, object> {
                {"name", "component-" + i + "-" + new string('n', 160)}, {"version", "1.0." + i}, {"installed", true}
            });
            var manyJson = serializer.Serialize(new Dictionary<string, object> { {"components", records}, {"packages", records} });
            Check(manyJson.Length > 2097152, "Many-record inventory must exceed the former limit.");
            ApiEndpoint.Status = 200;
            RsmClient.SendAsync(new AgentConfig { token = "fake" }, manyJson).GetAwaiter().GetResult();
            var many = serializer.Deserialize<Dictionary<string, object>>(ApiEndpoint.Json);
            Check(((object[])serializer.DeserializeObject(serializer.Serialize(many["components"]))).Length == records.Count, "No components truncated.");
            Check(((object[])serializer.DeserializeObject(serializer.Serialize(many["packages"]))).Length == records.Count, "No packages truncated.");
            var appxJson = "[{\"name\":\"" + new string('a', 2100000) + "\",\"version\":\"1.0\"}]";
            var appx = InventoryCollector.CreateInventorySerializer().DeserializeObject(appxJson) as object[];
            Check(appx != null && appx.Length == 1, "Large Appx output must parse rather than being omitted due to the default limit.");

            foreach (var status in new[] { 413, 500 }) {
                ApiEndpoint.Status = status;
                bool rejected = false;
                try { RsmClient.SendAsync(new AgentConfig { token = "fake" }, "{\"components\":[]}").GetAwaiter().GetResult(); }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "Non-success HTTP responses must still fail, including size rejection.");
            }
            ApiEndpoint.Calls = 0;
            bool malformed = false;
            try { RsmClient.SendAsync(new AgentConfig { token = "fake" }, "not-json").GetAwaiter().GetResult(); }
            catch (ArgumentException) { malformed = true; }
            Check(malformed && ApiEndpoint.Calls == 0, "Invalid JSON must not reach transport.");
            Console.WriteLine("Large inventory: " + passed + " checks passed (no network or service changes).");
            return 0;
        }
    }

    // Replace only the network boundary and logger in this standalone test.
    // RsmClient, inventory serialization and all other agent source is real.
    internal static class ApiEndpoint
    {
        internal static string Url = "https://rsm.invalid/AppController/commands_RSM/api/api.php";
        internal static TestBaseStore Store = new TestBaseStore();
        internal static int Calls, Status = 200;
        internal static string Json, Trigger;
        internal static Task<ApiResponse> PostAsync(string url, string token, string trigger, string json, Action<string> saveBase)
        {
            Calls++;
            Json = json;
            Trigger = trigger;
            return Task.FromResult(new ApiResponse { Status = Status, Reason = "test", Body = "" });
        }
    }
    internal sealed class TestBaseStore { internal void Write(string value) { throw new Exception("Unexpected endpoint mutation."); } }
    internal sealed class ApiResponse { internal int Status; internal string Reason, Body; }
    internal static class Logger
    {
        internal static string LogPath { get { return "test-only"; } }
        internal static void Info(string value) { }
        internal static void Warn(string value) { }
        internal static void Error(string value, Exception error = null) { }
        internal static void EventInfo(int id, string value) { }
        internal static void EventWarning(int id, string value) { }
        internal static void EventError(int id, string value, Exception error) { }
    }
}
