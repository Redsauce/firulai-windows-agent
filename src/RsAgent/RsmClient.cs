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
            const string apiSuffix = "/api.php";
            var apiUrl = apiUrlOverride ?? ApiEndpoint.Url;
            if (!apiUrl.EndsWith(apiSuffix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(AgentText.T("rsm.invalidUrl"));

            var itemsUrl = apiUrl.Substring(0, apiUrl.Length - apiSuffix.Length) + "/v2/items/get.php";
            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new Dictionary<string, object>
            {
                { "itemTypeID", "191" },
                { "propertyIDs", new[] { "1780", "1751", "1972", "1785", "1752", "1749", "1750" } },
                { "translateIDs", false },
                { "filterRules", new[] { new Dictionary<string, string>
                    { { "propertyID", "1780" }, { "value", config.Uuid }, { "operation", "=" } } } }
            });

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
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
                var matches = new List<Dictionary<string, object>>();
                SystemEligibility.CollectRows(decoded, config.Uuid, matches);
                if (matches.Count == 0)
                    throw new InvalidOperationException(AgentText.T("rsm.inventoryUuidMissing", config.Uuid));
                if (matches.Count != 1)
                    throw new InvalidOperationException(AgentText.T("rsm.uuidSearchFailed", (int)status, "ambiguous UUID"));
                SystemEligibility.Validate(matches[0], config, installing);
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
            var serializer = new JavaScriptSerializer();
            var inventory = serializer.Deserialize<Dictionary<string, object>>(inventoryJson);
            inventory["RStoken"] = config.Token;
            await SendEventAsync(config, "newServerData", serializer.Serialize(inventory)).ConfigureAwait(false);
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
                { "action", "disconnect" },
                { "hostname", Environment.MachineName },
                { "fqdn", InventoryCollector.GetFqdn() },
                { "RStoken", config.Token }
            });
            var body = await SendEventAsync(config, "changeSystemStatus", payload).ConfigureAwait(false);

            // Events Handler may accept and execute the event with HTTP 2xx
            // without forwarding the script stdout to the caller.
            if (string.IsNullOrWhiteSpace(body))
            {
                return new UninstallStatusUpdateResult
                {
                    SystemFound = true,
                    Message = "changeSystemStatus accepted with an empty response body."
                };
            }

            var response = serializer.Deserialize<Dictionary<string, object>>(body);
            bool systemFound;
            bool disconnected;

            if (!TryGetBoolean(response, "systemFound", out systemFound) ||
                !TryGetBoolean(response, "disconnected", out disconnected))
            {
                throw new InvalidOperationException(AgentText.T("rsm.disconnectResponseInvalid", body));
            }

            if (!systemFound && !disconnected)
            {
                return new UninstallStatusUpdateResult
                {
                    SystemFound = false,
                    Message = AgentText.T("rsm.noSystemForUuid", config.Uuid)
                };
            }

            if (!systemFound || !disconnected)
            {
                throw new InvalidOperationException(AgentText.T("rsm.disconnectResponseInvalid", body));
            }

            return new UninstallStatusUpdateResult
            {
                SystemFound = true,
                Message = body
            };
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
