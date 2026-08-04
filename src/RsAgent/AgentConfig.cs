using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal sealed class AgentConfig
    {
        public const string AgentVersion = "0.13.0";
        public const string DefaultApiUrl = "https://rsm1.redsauce.net/AppController/commands_RSM/api/api.php";

        public string token { get; set; }
        public string uuid { get; set; }
        public string api_url { get; set; }
        public string locale { get; set; }

        public string Token { get { return token ?? ""; } }
        public string Uuid { get { return uuid ?? ""; } }
        public string ApiUrl { get { return string.IsNullOrWhiteSpace(api_url) ? DefaultApiUrl : api_url; } }
        public string Locale { get { return AgentText.NormalizeLocale(locale); } }

        public static string DataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RSAgent"); }
        }

        public static string LogDir
        {
            get { return Path.Combine(DataDir, "logs"); }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(DataDir, "config.json"); }
        }

        public static string StatePath
        {
            get { return Path.Combine(DataDir, "state.json"); }
        }

        public static AgentConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                throw new FileNotFoundException(AgentText.T("config.missing"), ConfigPath);
            }

            var serializer = new JavaScriptSerializer();
            var raw = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));
            var config = new AgentConfig
            {
                token = GetString(raw, "token"),
                uuid = GetString(raw, "uuid"),
                api_url = GetString(raw, "api_url"),
                locale = GetString(raw, "locale")
            };
            AgentText.SetLocale(config.Locale);
            config.Validate();
            return config;
        }

        public static string LoadLocaleOrDefault()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return AgentText.DefaultLocale;
                }

                var serializer = new JavaScriptSerializer();
                var raw = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath));
                return AgentText.NormalizeLocale(GetString(raw, "locale"));
            }
            catch
            {
                return AgentText.DefaultLocale;
            }
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                throw new InvalidOperationException(AgentText.T("config.tokenMissing"));
            }

            if (!Regex.IsMatch(Uuid, "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"))
            {
                throw new InvalidOperationException(AgentText.T("config.uuidInvalid"));
            }
        }

        private static string GetString(Dictionary<string, object> raw, string key)
        {
            object value;
            return raw != null && raw.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
        }
    }
}
