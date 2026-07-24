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
        private const string ItemsGetUrl = "https://rsm1.redsauce.net/AppController/commands_RSM/api/v2/items/get.php";
        private const string ItemsUpdateUrl = "https://rsm1.redsauce.net/AppController/commands_RSM/api/v2/items/update.php";
        private const string SystemUuidPropertyId = "1780";
        private const string HostnameStatusPropertyId = "1751";
        private const string DisconnectedStatus = "Disconnected";

        public static async Task SendAsync(AgentConfig config, string inventoryJson)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var stopwatch = Stopwatch.StartNew();
            Logger.Info("Envío HTTP iniciado. Destino=" + GetSafeDestination(config.ApiUrl) + ", timeout=30s, payloadBytes=" + Encoding.UTF8.GetByteCount(inventoryJson) + ".");

            using (var client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                form.Add(new StringContent("newServerData"), "RStrigger");
                form.Add(new StringContent(inventoryJson), "RSdata");
                form.Add(new StringContent(config.Token), "RStoken");

                var response = await client.PostAsync(config.ApiUrl, form).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.Info("Respuesta HTTP recibida. Estado=" + (int)response.StatusCode + " " + response.ReasonPhrase + ", duraciónMs=" + stopwatch.ElapsedMilliseconds + ", respuestaCaracteres=" + body.Length + ".");
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Firulai respondio " + (int)response.StatusCode + ": " + body);
                }
            }
        }

        private static string GetSafeDestination(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                ? uri.Scheme + "://" + uri.Authority + uri.AbsolutePath
                : "URL no válida";
        }

        public static async Task<UninstallStatusUpdateResult> MarkSystemDisconnectedOnUninstallAsync(AgentConfig config)
        {
            var systemItemId = await GetSystemItemIdByUuidAsync(config).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(systemItemId))
            {
                return new UninstallStatusUpdateResult
                {
                    SystemFound = false,
                    Message = "No existe ningún System en Firulai para el UUID " + config.Uuid + ". Se omite la actualización remota y se permite la desinstalación local."
                };
            }

            var response = await UpdateHostnameStatusAsync(config, systemItemId, DisconnectedStatus).ConfigureAwait(false);
            return new UninstallStatusUpdateResult
            {
                SystemFound = true,
                Message = response
            };
        }

        private static Task<string> GetSystemItemIdByUuidAsync(AgentConfig config)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new Dictionary<string, object>
            {
                { "propertyIDs", new[] { "1749", "1750", SystemUuidPropertyId, "1827" } },
                { "translateIDs", true },
                { "filterRules", new[]
                    {
                        new Dictionary<string, string>
                        {
                            { "propertyID", SystemUuidPropertyId },
                            { "value", config.Uuid },
                            { "operation", "=" }
                        }
                    }
                }
            });

            var httpType = Type.GetTypeFromProgID("WinHttp.WinHttpRequest.5.1");
            if (httpType == null)
            {
                throw new InvalidOperationException("No se pudo inicializar WinHTTP para consultar Firulai.");
            }

            dynamic http = Activator.CreateInstance(httpType);
            http.Open("GET", ItemsGetUrl, false);
            http.SetTimeouts(5000, 5000, 20000, 20000);
            http.SetRequestHeader("Authorization", config.Token);
            http.SetRequestHeader("Content-Type", "application/json");
            http.Send(payload);

            int status = Convert.ToInt32(http.Status);
            string body = Convert.ToString(http.ResponseText);
            if (status != 200 && status != 201)
            {
                throw new InvalidOperationException("Firulai respondio " + status + " al buscar el UUID: " + body);
            }

            if (body.IndexOf(config.Uuid, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return Task.FromResult("");
            }

            var parsed = serializer.DeserializeObject(body);
            return Task.FromResult(FindFirstScalarValue(parsed, "ID") ?? FindFirstScalarValue(parsed, "id") ?? "");
        }

        private static async Task<string> UpdateHostnameStatusAsync(AgentConfig config, string systemItemId, string status)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var serializer = new JavaScriptSerializer();
            var payload = serializer.Serialize(new[]
            {
                new Dictionary<string, string>
                {
                    { "ID", systemItemId },
                    { HostnameStatusPropertyId, status }
                }
            });

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), ItemsUpdateUrl))
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                request.Headers.TryAddWithoutValidation("Authorization", config.Token);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Firulai respondio " + (int)response.StatusCode + " al actualizar el estado del sistema: " + body);
                }

                return body;
            }
        }

        private static string FindFirstScalarValue(object value, string key)
        {
            var dict = value as Dictionary<string, object>;
            if (dict != null)
            {
                object direct;
                if (dict.TryGetValue(key, out direct) && direct != null)
                {
                    return Convert.ToString(direct);
                }

                foreach (var child in dict.Values)
                {
                    var found = FindFirstScalarValue(child, key);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            var array = value as object[];
            if (array != null)
            {
                foreach (var child in array)
                {
                    var found = FindFirstScalarValue(child, key);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            return "";
        }
    }
}
