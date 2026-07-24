using System;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace RsAgent
{
    internal sealed class AgentStateData
    {
        public string last_success_utc { get; set; }
    }

    internal static class AgentState
    {
        private static readonly object Sync = new object();

        public static DateTime GetLastSuccessUtc()
        {
            lock (Sync)
            {
                if (!File.Exists(AgentConfig.StatePath))
                {
                    return DateTime.MinValue;
                }

                try
                {
                    var serializer = new JavaScriptSerializer();
                    var state = serializer.Deserialize<AgentStateData>(File.ReadAllText(AgentConfig.StatePath));
                    DateTime parsed;
                    if (state == null ||
                        string.IsNullOrWhiteSpace(state.last_success_utc) ||
                        !DateTime.TryParse(state.last_success_utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
                    {
                        Logger.Warn("state.json no contiene una fecha de última ejecución válida. Se considerará que la ejecución está pendiente.");
                        return DateTime.MinValue;
                    }

                    return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
                }
                catch (Exception ex)
                {
                    Logger.Error("No se pudo leer state.json. Se considerará que la ejecución está pendiente", ex);
                    return DateTime.MinValue;
                }
            }
        }

        public static void RecordSuccess(DateTime completedAtUtc)
        {
            lock (Sync)
            {
                Directory.CreateDirectory(AgentConfig.DataDir);
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(new AgentStateData
                {
                    last_success_utc = completedAtUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                });
                var temporaryPath = AgentConfig.StatePath + ".tmp";
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(AgentConfig.StatePath))
                {
                    File.Replace(temporaryPath, AgentConfig.StatePath, null);
                }
                else
                {
                    File.Move(temporaryPath, AgentConfig.StatePath);
                }
            }
        }
    }
}
