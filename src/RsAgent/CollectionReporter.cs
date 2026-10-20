using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RsAgent
{
    // Best-effort telemetry. A missing/older backend must not prevent inventory.
    internal sealed class CollectionReporter : IDisposable
    {
        private readonly AgentConfig config;
        private readonly string runId = Guid.NewGuid().ToString();
        private readonly CancellationTokenSource stopped = new CancellationTokenSource();
        private readonly Task worker;
        private readonly Func<HttpMessageHandler> handlerFactory;

        public CollectionReporter(AgentConfig config, Func<HttpMessageHandler> handlerFactory = null, TimeSpan? heartbeatInterval = null)
        {
            this.config = config;
            this.handlerFactory = handlerFactory ?? (() => new HttpClientHandler { AllowAutoRedirect = false });
            worker = Task.Run(async () => {
                await ReportAsync("start").ConfigureAwait(false);
                while (!stopped.IsCancellationRequested)
                {
                    try { await Task.Delay(heartbeatInterval ?? TimeSpan.FromSeconds(45), stopped.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                    if (!stopped.IsCancellationRequested) await ReportAsync("heartbeat").ConfigureAwait(false);
                }
            });
        }

        private async Task ReportAsync(string state)
        {
            try
            {
                var endpoint = Environment.GetEnvironmentVariable("RS_AGENT_COLLECTION_URL")
                    ?? "https://firulai.redsauce.net/api/agents/collection-status";
                var uri = new Uri(endpoint);
                if (uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)) return;
                using (var handler = handlerFactory())
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) })
                using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", config.Token);
                    request.Content = new StringContent(new JavaScriptSerializer().Serialize(new Dictionary<string, string> {
                        { "uuid", config.Uuid }, { "hostname", Environment.MachineName },
                        { "fqdn", InventoryCollector.GetFqdn() }, { "platform", "windows" },
                        { "runId", runId }, { "state", state }
                    }), System.Text.Encoding.UTF8, "application/json");
                    using (var response = await client.SendAsync(request).ConfigureAwait(false)) { }
                }
            }
            catch { /* Transient status is optional; the lease expires. */ }
        }

        public void Dispose()
        {
            stopped.Cancel();
            // Serialize finish after an in-flight start/heartbeat.
            worker.GetAwaiter().GetResult();
            ReportAsync("finish").GetAwaiter().GetResult();
            stopped.Dispose();
        }
    }
}
