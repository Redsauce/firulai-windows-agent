using System;
using System.Collections.Generic;

namespace RsAgent
{
    internal sealed class SystemEligibilityException : InvalidOperationException
    {
        public readonly string Code;
        public SystemEligibilityException(string code, string message) : base(message) { Code = code; }
    }

    internal static class SystemEligibility
    {
        private static string Field(Dictionary<string, object> row, string key)
        {
            object value;
            return row.TryGetValue(key, out value) && value != null ? Convert.ToString(value).Trim() : "";
        }

        public static void Validate(Dictionary<string, object> row, AgentConfig config, bool installing)
        {
            var account = Field(row, "1785");
            if (string.IsNullOrEmpty(account) ||
                (account != "6956" && !Field(row, "1972").Equals("covered", StringComparison.OrdinalIgnoreCase)))
                throw new SystemEligibilityException("uncovered", AgentText.T("rsm.systemNotCovered"));

            if (installing)
            {
                var os = Field(row, "1752");
                var host = Field(row, "1749");
                var fqdn = Field(row, "1750");
                var localHost = Environment.MachineName;
                var localFqdn = InventoryCollector.GetFqdn();
                if (!(os.Equals("windows", StringComparison.OrdinalIgnoreCase) ||
                    os.StartsWith("windows ", StringComparison.OrdinalIgnoreCase)) ||
                    ((!string.IsNullOrEmpty(host) || !string.IsNullOrEmpty(fqdn)) &&
                    !MatchesIdentity(host, localHost, localFqdn) &&
                    !MatchesIdentity(fqdn, localHost, localFqdn)))
                    throw new SystemEligibilityException("identity", AgentText.T("rsm.systemIdentityMismatch"));
            }
            else
            {
                var status = Field(row, "1751").ToLowerInvariant();
                if (status != "activo" && status != "active" && status != "connected")
                    throw new SystemEligibilityException("inactive", AgentText.T("rsm.systemNotActive"));
            }
        }

        private static bool MatchesIdentity(string value, string hostname, string fqdn)
        {
            return !string.IsNullOrEmpty(value) &&
                ((!string.IsNullOrEmpty(hostname) && value.Equals(hostname, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrEmpty(fqdn) && value.Equals(fqdn, StringComparison.OrdinalIgnoreCase)));
        }

        public static void CollectRows(object value, string uuid, List<Dictionary<string, object>> matches)
        {
            var rows = value as object[];
            if (rows != null)
            {
                foreach (var row in rows) CollectRows(row, uuid, matches);
                return;
            }
            var fields = value as Dictionary<string, object>;
            if (fields == null) return;
            if (fields.ContainsKey("error") || fields.ContainsKey("errors"))
                throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", 200, "RSM error"));
            if (Field(fields, "1780").Equals(uuid, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(fields);
                return;
            }
            object wrapped;
            foreach (var key in new[] { "items", "data", "result" })
                if (fields.TryGetValue(key, out wrapped))
                {
                    CollectRows(wrapped, uuid, matches);
                    return;
                }
        }
    }
}
