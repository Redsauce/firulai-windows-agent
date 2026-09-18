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
