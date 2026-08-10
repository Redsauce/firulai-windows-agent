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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var stopwatch = Stopwatch.StartNew();
            Logger.Info(AgentText.T("rsm.httpStarted", GetSafeDestination(config.ApiUrl), Encoding.UTF8.GetByteCount(json)));

            using (var client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", config.Token);
                form.Add(new StringContent(trigger), "RStrigger");
                form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "RSdata");
                form.Add(new StringContent(config.Token), "RStoken");

                var response = await client.PostAsync(config.ApiUrl, form).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.Info(AgentText.T(
                    "rsm.httpResponse",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    stopwatch.ElapsedMilliseconds,
                    body.Length));
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(AgentText.T("rsm.httpFailed", (int)response.StatusCode, body));
                }

                return body;
            }
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
