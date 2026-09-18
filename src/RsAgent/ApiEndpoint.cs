using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace RsAgent
{
    internal static class ApiEndpoint
    {
        public static readonly string DefaultBase;
        public static readonly string Path;
        public static readonly ApiBaseStore Store = new ApiBaseStore(RegistryHive.LocalMachine, @"SOFTWARE\Redsauce\RSAgent");

        static ApiEndpoint()
        {
            using (var stream = typeof(ApiEndpoint).Assembly.GetManifestResourceStream("RsAgent.ApiEndpoint.txt"))
            using (var reader = new StreamReader(stream))
            {
                DefaultBase = reader.ReadLine();
                Path = reader.ReadLine();
            }
        }

        public static string Url { get { return BuildUrl(Store.Read()); } }

        public static string NormalizeBase(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != "https" ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
                throw new InvalidOperationException("Invalid API base URL.");
            return uri.AbsoluteUri.TrimEnd('/') + "/";
        }

        public static string BuildUrl(string baseUrl) { return NormalizeBase(baseUrl) + Path; }

        public static string BaseFromUrl(string url)
        {
            if (!url.EndsWith(Path, StringComparison.Ordinal))
                throw new InvalidOperationException("API redirect does not end with the expected endpoint.");
            string baseUrl = url.Substring(0, url.Length - Path.Length);
            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                throw new InvalidOperationException("API endpoint must follow a base ending in '/'.");
            return NormalizeBase(baseUrl);
        }

        public static string ResolveRedirect(string currentUrl, string location)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(location) || !Uri.TryCreate(new Uri(currentUrl), location, out uri))
                throw new InvalidOperationException("Missing or invalid API redirect Location.");
            // Location must be the complete endpoint; do not guess another path.
            return BuildUrl(BaseFromUrl(uri.AbsoluteUri));
        }

        // The same transport is used by the agent and the standalone tests.
        public static async Task<ApiResponse> PostAsync(string url, string token, string trigger, string json,
            Action<string> saveBase, HttpMessageHandler handler = null)
        {
            BaseFromUrl(url);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                string permanentBase = null;
                bool permanentPrefix = true;
                for (int hop = 0; hop <= 5; hop++)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    using (var form = new MultipartFormDataContent())
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", token);
                        form.Add(new StringContent(trigger), "RStrigger");
                        form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "RSdata");
                        form.Add(new StringContent(token), "RStoken");
                        request.Content = form;
                        using (var response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            int status = (int)response.StatusCode;
                            if (status == 301 || status == 302 || status == 307 || status == 308)
                            {
                                if (hop == 5) throw new InvalidOperationException("Too many API redirects.");
                                url = ResolveRedirect(url, response.Headers.Location == null ? null : response.Headers.Location.ToString());
                                permanentPrefix = permanentPrefix && (status == 301 || status == 308);
                                if (permanentPrefix) permanentBase = BaseFromUrl(url);
                                continue;
                            }
                            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (response.IsSuccessStatusCode && permanentBase != null) saveBase(permanentBase);
                            return new ApiResponse { Status = status, Reason = response.ReasonPhrase, Body = body };
                        }
                    }
                }
                throw new InvalidOperationException("Too many API redirects.");
            }
        }
    }

    internal sealed class ApiResponse
    {
        public int Status;
        public string Reason;
        public string Body;
    }

    // Production uses HKLM; tests explicitly provide an isolated HKCU key.
    internal sealed class ApiBaseStore
    {
        private readonly RegistryHive hive;
        private readonly string keyPath;

        public ApiBaseStore(RegistryHive hive, string keyPath) { this.hive = hive; this.keyPath = keyPath; }

        private RegistryKey OpenRoot()
        {
            return RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
        }

        public string Read()
        {
            using (var root = OpenRoot())
            using (var key = root.OpenSubKey(keyPath))
                return ApiEndpoint.NormalizeBase(key == null ? ApiEndpoint.DefaultBase : Convert.ToString(key.GetValue("ApiBaseUrl", ApiEndpoint.DefaultBase)));
        }

        public void Write(string baseUrl)
        {
            baseUrl = ApiEndpoint.NormalizeBase(baseUrl);
            using (var root = OpenRoot())
            using (var key = root.CreateSubKey(keyPath))
                key.SetValue("ApiBaseUrl", baseUrl, RegistryValueKind.String);
        }
    }
}
