using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal sealed class UninstallStatusUpdateResult
    {
        public bool SystemFound { get; set; }
        public string Message { get; set; }
    }

    internal static class RsmClient
    {
        public static async Task ValidateSystemUuidExistsAsync(AgentConfig config, bool installing = false, string apiUrlOverride = null)
        {
            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new Dictionary<string, object>
            {
                { "itemTypeID", "191" },
                { "propertyIDs", new[] { "1780", "1751", "1972", "1785", "1752", "1749", "1750" } },
                { "translateIDs", false },
                { "filterRules", new[] { new Dictionary<string, string>
                    { { "propertyID", "1780" }, { "value", config.Uuid }, { "operation", "=" } } } }
            });

            var decoded = await RequestItemsAsync(config, payload, apiUrlOverride).ConfigureAwait(false);
            var matches = new List<Dictionary<string, object>>();
            SystemEligibility.CollectRows(decoded, config.Uuid, matches);
            if (matches.Count == 0)
                throw new InvalidOperationException(AgentText.T("rsm.inventoryUuidMissing", config.Uuid));
            if (matches.Count != 1)
                throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", 200, "ambiguous UUID"));
            SystemEligibility.Validate(matches[0], config, installing);
        }

        private static async Task<object> RequestItemsAsync(AgentConfig config, string payload, string apiUrlOverride = null, HttpMessageHandler handler = null)
        {
            const string apiSuffix = "/api.php";
            var apiUrl = apiUrlOverride ?? ApiEndpoint.Url;
            if (!apiUrl.EndsWith(apiSuffix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(AgentText.T("rsm.invalidUrl"));
            var itemsUrl = apiUrl.Substring(0, apiUrl.Length - apiSuffix.Length) + "/v2/items/get.php";
            var serializer = new JavaScriptSerializer();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                string body = null;
                HttpStatusCode status = 0;
                for (var hop = 0; hop <= 5; hop++)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, itemsUrl))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", config.Token);
                        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using (var response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            status = response.StatusCode;
                            if ((int)status >= 300 && (int)status < 400)
                            {
                                if (hop == 5 || response.Headers.Location == null)
                                    throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", (int)status, "invalid redirect"));
                                var next = new Uri(new Uri(itemsUrl), response.Headers.Location);
                                if (next.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(next.UserInfo) ||
                                    !string.IsNullOrEmpty(next.Query) || !string.IsNullOrEmpty(next.Fragment) ||
                                    !next.AbsolutePath.EndsWith("/commands_RSM/api/v2/items/get.php", StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", (int)status, "unsafe redirect"));
                                itemsUrl = next.AbsoluteUri;
                                continue;
                            }
                            body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                    break;
                }
                if ((int)status < 200 || (int)status >= 300)
                    throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", (int)status, "HTTP error"));

                object decoded;
                try { decoded = serializer.DeserializeObject(body); }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", (int)status, "invalid response"));
                }
            return decoded;
            }
        }

        private static int CountSystemUuidMatches(object value, string uuid)
        {
            var rows = value as object[];
            if (rows != null)
            {
                var count = 0;
                foreach (var row in rows) count += CountSystemUuidMatches(row, uuid);
                return count;
            }
            var fields = value as Dictionary<string, object>;
            if (fields == null) return 0;
            if (fields.ContainsKey("error") || fields.ContainsKey("errors"))
                throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", 200, "RSM error"));
            object storedUuid;
            if (fields.TryGetValue("1780", out storedUuid))
                return string.Equals(Convert.ToString(storedUuid), uuid, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            object wrappedRows;
            foreach (var key in new[] { "items", "data", "result" })
                if (fields.TryGetValue(key, out wrappedRows)) return CountSystemUuidMatches(wrappedRows, uuid);
            return 0;
        }

        public static async Task SendAsync(AgentConfig config, string inventoryJson)
        {
            var serializer = InventoryCollector.CreateInventorySerializer();
            var inventory = serializer.Deserialize<Dictionary<string, object>>(inventoryJson);
            inventory["RStoken"] = config.Token;
            await SendEventAsync(config, "newServerData", serializer.Serialize(inventory)).ConfigureAwait(false);
        }

        internal static Dictionary<string, string> BuildAgentVersionReport(AgentConfig config)
        {
            return new Dictionary<string, string>
            {
                { "uuid", config.Uuid },
                { "action", "report_version" },
                { "agent_version", AgentConfig.AgentVersion },
                { "hostname", Environment.MachineName },
                { "fqdn", InventoryCollector.GetFqdn() },
                { "RStoken", config.Token }
            };
        }

        internal static async Task ReportAgentVersionAsync(AgentConfig config)
        {
            var serializer = new JavaScriptSerializer();
            var body = await SendEventAsync(config, "changeSystemStatus", serializer.Serialize(BuildAgentVersionReport(config))).ConfigureAwait(false);
            // An empty HTTP response means queued, not that RSM has already updated the version.
            if (string.IsNullOrWhiteSpace(body))
            {
                Logger.Info("Agent version report queued: " + AgentConfig.AgentVersion + ".");
                return;
            }
            var response = serializer.Deserialize<Dictionary<string, object>>(body);
            bool updated;
            if (!TryGetBoolean(response, "updated", out updated) || !updated)
                throw new InvalidOperationException("Agent version report was not accepted.");
            Logger.Info("Agent version report confirmed: " + AgentConfig.AgentVersion + ".");
        }

        private static string GetSafeDestination(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                ? uri.Scheme + "://" + uri.Authority + uri.AbsolutePath
                : AgentText.T("rsm.invalidUrl");
        }

        public static async Task<UninstallStatusUpdateResult> MarkSystemDisconnectedOnUninstallAsync(AgentConfig config)
        {
            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new Dictionary<string, string>
            {
                { "uuid", config.Uuid },
                { "action", "uninstall" },
                { "hostname", Environment.MachineName },
                { "fqdn", InventoryCollector.GetFqdn() },
                { "RStoken", config.Token }
            });
            var body = await SendEventAsync(config, "changeSystemStatus", payload).ConfigureAwait(false);
            return await WaitForUninstallationAsync(body,
                () => GetUninstallationConfirmationAsync(config),
                () => Task.Delay(1000)).ConfigureAwait(false);
        }

        // Keep timing and transport injectable for isolated lifecycle tests.
        internal static async Task<UninstallStatusUpdateResult> WaitForUninstallationAsync(
            string body, Func<Task<int>> confirm, Func<Task> wait)
        {
            var serializer = new JavaScriptSerializer();

            // Acknowledging a queued event is not a confirmation of uninstallation.
            if (!string.IsNullOrWhiteSpace(body))
            {
                var response = serializer.Deserialize<Dictionary<string, object>>(body);
                object error;
                if (response.TryGetValue("error", out error) && !string.IsNullOrEmpty(Convert.ToString(error)) && Convert.ToString(error) != "system_not_found")
                    throw new InvalidOperationException(AgentText.T("rsm.disconnectResponseInvalid", "RSM rejected request"));
            }
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var confirmation = await confirm().ConfigureAwait(false);
                if (confirmation != 0)
                    return new UninstallStatusUpdateResult { SystemFound = confirmation == 1, Message = "Agent uninstalled or system already deleted; no coverage retained." };
                await wait().ConfigureAwait(false);
            }
            throw new InvalidOperationException(AgentText.T("rsm.disconnectResponseInvalid", "Uninstallation was not confirmed by RSM."));
        }

        internal static bool IsUninstalledRow(Dictionary<string, object> row)
        {
            object status, coverage;
            return row.TryGetValue("1751", out status) && row.TryGetValue("1972", out coverage) &&
                Convert.ToString(status).Equals("Agente desinstalado", StringComparison.OrdinalIgnoreCase) &&
                Convert.ToString(coverage).Equals("uncovered", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<int> GetUninstallationConfirmationAsync(AgentConfig config)
        {
            var url = ApiEndpoint.Url.Substring(0, ApiEndpoint.Url.Length - "/api.php".Length) + "/v2/items/get.php";
            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new Dictionary<string, object> {
                { "itemTypeID", "191" }, { "propertyIDs", new[] { "1780", "1751", "1972" } },
                { "translateIDs", false }, { "filterRules", new[] { new Dictionary<string, string> {
                    { "propertyID", "1780" }, { "value", config.Uuid }, { "operation", "=" } } } }
            });
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                request.Headers.TryAddWithoutValidation("Authorization", config.Token);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Uninstallation confirmation unavailable.");
                    var rows = new List<Dictionary<string, object>>();
                    var decoded = serializer.DeserializeObject(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    SystemEligibility.CollectRows(decoded, config.Uuid, rows);
                    var fields = decoded as Dictionary<string, object>;
                    var array = decoded as object[];
                    if (rows.Count == 0 && ((fields != null && fields.Count == 0) || (array != null && array.Length == 0)))
                        return await VerifyMissingSystemAsync(config).ConfigureAwait(false) ? 2 : 0;
                    return rows.Count == 1 && IsUninstalledRow(rows[0]) ? 1 : 0;
                }
            }
        }

        internal static bool IsVerifiedMissingResponse(object decoded)
        {
            var fields = decoded as Dictionary<string, object>;
            object missing, verified;
            return fields != null && fields.Count == 2 &&
                fields.TryGetValue("missing", out missing) && missing is bool && (bool)missing &&
                fields.TryGetValue("verified", out verified) && verified is bool && (bool)verified;
        }

        private static async Task<bool> VerifyMissingSystemAsync(AgentConfig config)
        {
            var serializer = new JavaScriptSerializer();
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            using (var request = new HttpRequestMessage(HttpMethod.Post,
                "https://firulai.redsauce.net/api/agents/uninstall/missing-system"))
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                request.Headers.TryAddWithoutValidation("Authorization", config.Token);
                request.Content = new StringContent(serializer.Serialize(new Dictionary<string, string> {
                    { "uuid", config.Uuid }
                }), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidOperationException("Missing System could not be independently verified.");
                    return IsVerifiedMissingResponse(serializer.DeserializeObject(
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false)));
                }
            }
        }

        private static async Task<string> SendEventAsync(AgentConfig config, string trigger, string json)
        {
            string url = ApiEndpoint.Url;
            var stopwatch = Stopwatch.StartNew();
            Logger.Info(AgentText.T("rsm.httpStarted", GetSafeDestination(url), Encoding.UTF8.GetByteCount(json)));
            var response = await ApiEndpoint.PostAsync(url, config.Token, trigger, json, ApiEndpoint.Store.Write).ConfigureAwait(false);
            Logger.Info(AgentText.T("rsm.httpResponse", response.Status, response.Reason, stopwatch.ElapsedMilliseconds, response.Body.Length));
            if (response.Status < 200 || response.Status >= 300)
                throw new InvalidOperationException(AgentText.T("rsm.httpFailed", response.Status, response.Body));
            return response.Body;
        }

        private static bool TryGetBoolean(Dictionary<string, object> values, string key, out bool result)
        {
            result = false;
            object value;
            if (values == null || !values.TryGetValue(key, out value) || value == null)
            {
                return false;
            }

            if (value is bool)
            {
                result = (bool)value;
                return true;
            }

            return bool.TryParse(Convert.ToString(value), out result);
        }
    }
}
