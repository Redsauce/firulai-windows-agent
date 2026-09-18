using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

// Loads the installed assembly; no agent code is recompiled for this probe.
internal static class InstalledApiTests
{
    private const string Initial = "https://rsm1.invalid/AppController/";
    private const string Destination = "https://httpbingo.org/anything/rsm2/AppController/";
    private static Type endpoint;
    private static object store;

    private sealed class Adapter : DelegatingHandler
    {
        public int Status;
        public int Calls;
        public string First;
        public string Path;
        public Adapter() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            string url = request.RequestUri.AbsoluteUri;
            if (Calls++ == 0) First = url;
            Check(request.Method == HttpMethod.Post, "POST");
            string body = await request.Content.ReadAsStringAsync();
            Check(body.Contains("fake-token") && body.Contains("test-payload"), "fictitious payload");
            request.Headers.UserAgent.ParseAdd("RsAgent-Installed-Test/1.0");
            if (url == Initial + Path)
                request.RequestUri = new Uri("https://httpbingo.org/redirect-to?url=" + Uri.EscapeDataString(Destination + Path) + "&status_code=" + Status);
            else Check(url == Destination + Path, "controlled destination");
            return await base.SendAsync(request, cancel);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
    }
    private static string Read() { return (string)store.GetType().GetMethod("Read").Invoke(store, null); }
    private static void Write(string value) { store.GetType().GetMethod("Write").Invoke(store, new object[] { value }); }

    private static void Send(int status, bool second)
    {
        string path = (string)endpoint.GetField("Path").GetValue(null);
        string before = Read();
        var adapter = new Adapter { Status = status, Path = path };
        var task = (Task)endpoint.GetMethod("PostAsync").Invoke(null,
            new object[] { before + path, "fake-token", "test-event", "test-payload", new Action<string>(Write), adapter });
        task.GetAwaiter().GetResult();
        object response = task.GetType().GetProperty("Result").GetValue(task, null);
        Check((int)response.GetType().GetField("Status").GetValue(response) == 200, "destination HTTP 200");
        string body = (string)response.GetType().GetField("Body").GetValue(response);
        Check(body.Contains("POST") && body.Contains("test-payload"), "server received POST data");
        string expected = status == 301 ? Destination : Initial;
        Check(Read() == expected, "stored base");
        if (second)
            Check(before == expected && adapter.First == expected + path && adapter.Calls == (status == 301 ? 1 : 2), "new process uses persisted base");
        Console.WriteLine("HTTP {0}, {1}: base = {2}; requests = {3}", status, second ? "new process" : "first process", Read(), adapter.Calls);
    }

    public static int Main(string[] args)
    {
        bool child = args.Length == 4;
        string keyPath = child ? args[2] : @"SOFTWARE\Redsauce\RSAgentTests\" + Guid.NewGuid().ToString("N");
        try
        {
            Assembly assembly = Assembly.LoadFrom(args[0]);
            endpoint = assembly.GetType("RsAgent.ApiEndpoint", true);
            Type storeType = assembly.GetType("RsAgent.ApiBaseStore", true);
            store = Activator.CreateInstance(storeType, new object[] { RegistryHive.CurrentUser, keyPath });
            if (child) { Send(int.Parse(args[3]), true); return 0; }
            Console.WriteLine("Installed binary copy: " + args[0]);
            Console.WriteLine("Temporary registry: HKCU\\" + keyPath);
            foreach (int status in new[] { 301, 302 })
            {
                Write(Initial);
                Send(status, false);
                using (var next = Process.Start(new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = "\"" + args[0] + "\" --next " + keyPath + " " + status,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }))
                {
                    Task<string> output = next.StandardOutput.ReadToEndAsync();
                    Task<string> error = next.StandardError.ReadToEndAsync();
                    if (!next.WaitForExit(120000)) { next.Kill(); next.WaitForExit(); throw new Exception("Child timed out"); }
                    Console.Write(output.GetAwaiter().GetResult());
                    Check(next.ExitCode == 0, error.GetAwaiter().GetResult());
                }
            }
            Console.WriteLine("PASS: installed module, registry persistence and independent process.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            if (!child)
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
                    root.DeleteSubKey(keyPath, false);
        }
    }
}
