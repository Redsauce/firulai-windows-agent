using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal static class EligibilityTests
    {
        private static int passed;
        private static AgentConfig config = new AgentConfig { uuid = "550e8400-e29b-41d4-a716-446655440000", token = "test", locale = "en_US" };
        private static Dictionary<string, object> Row(string status, string coverage, string account = "6955")
        {
            return new Dictionary<string, object> {
                {"1780", config.Uuid}, {"1751", status}, {"1972", coverage},
                {"1785", account}, {"1752", "Windows"}, {"1749", ""}, {"1750", ""}
            };
        }
        private static void Check(string name, Dictionary<string, object> row, bool installing, string expected)
        {
            string actual = "";
            try { SystemEligibility.Validate(row, config, installing); }
            catch (SystemEligibilityException ex) { actual = ex.Code; }
            if (actual != expected) throw new Exception(name + ": expected " + expected + ", got " + actual);
            passed++;
        }
        private static int Main()
        {
            AgentText.SetLocale("en_US");
            Check("active covered", Row("Activo", "covered"), false, "");
            Check("English active", Row("Active", "covered"), false, "");
            Check("disconnected", Row("Disconnected", "covered"), false, "inactive");
            Check("empty status", Row("", "covered"), false, "inactive");
            Check("unknown status", Row("unknown", "covered"), false, "inactive");
            Check("uncovered", Row("Activo", "uncovered"), false, "uncovered");
            Check("empty coverage", Row("Activo", ""), false, "uncovered");
            Check("missing account", Row("Activo", "covered", ""), false, "uncovered");
            Check("Arsys coverage exception", Row("Activo", "uncovered", "6956"), false, "");
            Check("Arsys cannot bypass disconnected", Row("Disconnected", "covered", "6956"), false, "inactive");
            Check("fresh reserve", Row("", "covered"), true, "");
            Check("reinstall disconnected", Row("Disconnected", "covered"), true, "");
            var installedWindows = Row("Disconnected", "covered");
            installedWindows["1752"] = "Windows 10 Pro";
            installedWindows["1749"] = Environment.MachineName;
            Check("reinstall detected Windows edition", installedWindows, true, "");
            Check("uncovered install", Row("", "uncovered"), true, "uncovered");
            var wrongPlatform = Row("", "covered"); wrongPlatform["1752"] = "Linux";
            Check("wrong platform", wrongPlatform, true, "identity");
            var foreign = Row("", "covered"); foreign["1749"] = "foreign-computer.invalid";
            Check("foreign machine", foreign, true, "identity");
            var serializer = new JavaScriptSerializer();
            var matches = new List<Dictionary<string, object>>();
            SystemEligibility.CollectRows(serializer.DeserializeObject("[]"), config.Uuid, matches);
            if (matches.Count != 0) throw new Exception("missing UUID matched");
            passed++;
            var encoded = serializer.Serialize(Row("Activo", "covered"));
            SystemEligibility.CollectRows(serializer.DeserializeObject("[" + encoded + "," + encoded + "]"), config.Uuid, matches);
            if (matches.Count != 2) throw new Exception("duplicate UUID not detected");
            passed++;
            matches.Clear();
            SystemEligibility.CollectRows(serializer.DeserializeObject("{\"data\":[" + encoded + "]}"), config.Uuid, matches);
            if (matches.Count != 1) throw new Exception("wrapped response not parsed");
            passed++;
            Console.WriteLine(passed + " eligibility checks passed.");
            return 0;
        }
    }
}
