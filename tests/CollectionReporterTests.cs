using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal static class CollectionReporterTests
    {
        private sealed class Capture : HttpMessageHandler
        {
            private readonly Action<string> receive;
            public Capture(Action<string> receive) { this.receive = receive; }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (request.RequestUri.Scheme != "https") throw new Exception("Unencrypted telemetry");
                var body = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(await request.Content.ReadAsStringAsync());
                if (body["platform"] != "windows" || string.IsNullOrEmpty(body["runId"])) throw new Exception("Invalid body");
                receive(body["state"]);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        public static int Main()
        {
            Environment.SetEnvironmentVariable("RS_AGENT_COLLECTION_URL", "https://fixture.invalid/status");
            var config = new AgentConfig { uuid = "fixture", token = "test-token" };
            var states = new List<string>();
            using (var heartbeat = new ManualResetEventSlim())
            {
                using (var reporter = new CollectionReporter(config, () => new Capture(state => {
                    lock (states) states.Add(state);
                    if (state == "heartbeat") heartbeat.Set();
                }), TimeSpan.FromMilliseconds(10)))
                {
                    if (!heartbeat.Wait(5000)) throw new Exception("Heartbeat missing");
                }
            }
            if (states[0] != "start" || states[states.Count - 1] != "finish") throw new Exception("Invalid lifecycle");
            if (states.FindAll(s => s == "finish").Count != 1) throw new Exception("Duplicate finish");
            var failed = 0;
            using (var reporter = new CollectionReporter(config, () => new Capture(state => {
                failed++;
                throw new HttpRequestException("offline fixture");
            }))) { }
            if (failed != 2) throw new Exception("Network failure prevented start/finish attempt");
            Environment.SetEnvironmentVariable("RS_AGENT_COLLECTION_URL", "http://fixture.invalid/status");
            var insecure = 0;
            using (var reporter = new CollectionReporter(config, () => { insecure++; return new Capture(state => {}); })) { }
            if (insecure != 0) throw new Exception("HTTP endpoint accepted credentials");
            Console.WriteLine("PASS: collection start, heartbeat, ordered finish, offline tolerance and HTTPS enforcement.");
            return 0;
        }
    }
}
