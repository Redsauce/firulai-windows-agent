using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RsAgent
{
    internal static class ApiEndpointTests
    {
        private const string Initial = "https://rsm1.invalid/AppController/";
        private const string Destination = "https://httpbingo.org/anything/rsm2/AppController/";

        private sealed class Transport : DelegatingHandler
        {
            public bool Live;
            public int Redirect;
            public int FinalStatus = 200;
            public int SecondRedirect;
            public string Location = Destination + ApiEndpoint.Path;
            public readonly List<string> Requests = new List<string>();

            public Transport() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
            {
                string url = request.RequestUri.AbsoluteUri;
                Requests.Add(url);
                Assert(request.Method == HttpMethod.Post, "POST retained");
                Assert(string.Join("", request.Headers.GetValues("Authorization")) == "fake-token", "authorization retained");
                string body = await request.Content.ReadAsStringAsync();
                Assert(body.Contains("test-payload") && body.Contains("fake-token") && body.Contains("test-event"), "multipart data retained");
                if (Live)
                {
                    request.Headers.UserAgent.ParseAdd("RsAgent-Redirect-Test/1.0");
                    if (url == ApiEndpoint.BuildUrl(Initial))
                        request.RequestUri = new Uri("https://httpbingo.org/redirect-to?url=" + Uri.EscapeDataString(Location) + "&status_code=" + Redirect);
                    return await base.SendAsync(request, cancel);
                }
                bool second = SecondRedirect != 0 && url == ApiEndpoint.BuildUrl(Destination);
                var response = new HttpResponseMessage((HttpStatusCode)(url == ApiEndpoint.BuildUrl(Initial) ? Redirect : second ? SecondRedirect : FinalStatus))
                {
                    Content = new StringContent("{\"method\":\"POST\",\"data\":\"test-payload\"}")
                };
                if (url == ApiEndpoint.BuildUrl(Initial) && Location != null)
                    response.Headers.Location = new Uri(Location, UriKind.RelativeOrAbsolute);
                if (second) response.Headers.Location = new Uri("https://rsm3.invalid/AppController/" + ApiEndpoint.Path);
                return response;
            }
        }

        private static void Assert(bool value, string message)
        {
            if (!value) throw new Exception("FAIL: " + message);
        }

        private static ApiResponse Send(ApiBaseStore store, Transport transport)
        {
            return ApiEndpoint.PostAsync(ApiEndpoint.BuildUrl(store.Read()), "fake-token", "test-event", "test-payload", store.Write, transport).GetAwaiter().GetResult();
        }

        public static void Main(string[] args)
        {
            if (args.Length == 4 && args[0] == "--next")
            {
                int status = int.Parse(args[3]);
                string expected = status == 301 || status == 308 ? Destination : Initial;
                var reopened = new ApiBaseStore(RegistryHive.CurrentUser, args[2]);
                Assert(reopened.Read() == expected, "new process reads persisted base");
                var next = new Transport { Live = args[1] == "--live", Redirect = status };
                Assert(Send(reopened, next).Status == 200, "next execution");
                Assert(next.Requests[0] == ApiEndpoint.BuildUrl(expected), "new process starts from stored base");
                Assert(next.Requests.Count == (expected == Destination ? 1 : 2), "new process redirect count");
                Console.WriteLine("New process: {0} ({1} request(s))", next.Requests[0], next.Requests.Count);
                return;
            }
            bool live = args.Length > 0 && args[0] == "--live";
            string keyPath = @"SOFTWARE\Redsauce\RSAgentTests\" + Guid.NewGuid().ToString("N");
            var store = new ApiBaseStore(RegistryHive.CurrentUser, keyPath);
            Console.WriteLine("Isolated test registry: HKCU\\" + keyPath);
            try
            {
                foreach (int status in new[] { 301, 302, 307, 308 })
                {
                    store.Write(Initial);
                    Console.WriteLine("HTTP {0}: base before = {1}", status, store.Read());
                    var transport = new Transport { Live = live, Redirect = status };
                    var result = Send(store, transport);
                    Assert(result.Status == 200 && result.Body.Contains("POST") && result.Body.Contains("test-payload"), "successful destination with POST data: HTTP " + result.Status + " " + result.Body);
                    string expected = status == 301 || status == 308 ? Destination : Initial;
                    var freshStore = new ApiBaseStore(RegistryHive.CurrentUser, keyPath);
                    Assert(freshStore.Read() == expected, "persisted base");
                    Console.WriteLine("HTTP {0}: base read from registry = {1}", status, freshStore.Read());
                    using (var nextProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = Assembly.GetExecutingAssembly().Location,
                        Arguments = "--next " + (live ? "--live " : "--offline ") + keyPath + " " + status,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }))
                    {
                        Task<string> output = nextProcess.StandardOutput.ReadToEndAsync();
                        Task<string> error = nextProcess.StandardError.ReadToEndAsync();
                        if (!nextProcess.WaitForExit(120000))
                        {
                            nextProcess.Kill();
                            nextProcess.WaitForExit();
                            throw new Exception("Next process timed out");
                        }
                        Console.Write(output.GetAwaiter().GetResult());
                        Assert(nextProcess.ExitCode == 0, "next process: " + error.GetAwaiter().GetResult());
                    }
                }
                if (!live)
                {
                    foreach (string target in new[] { "http://bad.example/" + ApiEndpoint.Path, "https://user@bad.example/" + ApiEndpoint.Path, "https://bad.example/prefix" + ApiEndpoint.Path, "https://bad.example/other.php", null })
                    {
                        store.Write(Initial);
                        bool rejected = false;
                        var transport = new Transport { Redirect = 301, Location = target };
                        try { Send(store, transport); } catch (InvalidOperationException) { rejected = true; }
                        Assert(rejected && transport.Requests.Count == 1 && store.Read() == Initial, "invalid redirect rejected without persistence");
                    }
                    store.Write(Initial);
                    Assert(Send(store, new Transport { Redirect = 301, FinalStatus = 500 }).Status == 500 && store.Read() == Initial, "failed destination does not persist");
                    var loop = new Transport { Redirect = 301, Location = ApiEndpoint.BuildUrl(Initial) };
                    bool loopRejected = false;
                    try { Send(store, loop); } catch (InvalidOperationException) { loopRejected = true; }
                    Assert(loopRejected && loop.Requests.Count == 6 && store.Read() == Initial, "bounded redirect loop");
                    Assert(ApiEndpoint.ResolveRedirect(ApiEndpoint.BuildUrl(Initial), "/other/" + ApiEndpoint.Path) == "https://rsm1.invalid/other/" + ApiEndpoint.Path, "relative Location");
                    store.Write(Initial);
                    Assert(Send(store, new Transport { Redirect = 302, SecondRedirect = 301 }).Status == 200 && store.Read() == Initial, "temporary then permanent");
                    store.Write(Initial);
                    Assert(Send(store, new Transport { Redirect = 301, SecondRedirect = 302 }).Status == 200 && store.Read() == Destination, "permanent then temporary");
                }
                Console.WriteLine("PASS: persisted base and next execution. Test registry key removed on exit.");
            }
            finally
            {
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
                    root.DeleteSubKey(keyPath, false);
            }
        }
    }
}
