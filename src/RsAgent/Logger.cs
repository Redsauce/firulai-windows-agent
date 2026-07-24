using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RsAgent
{
    internal static class Logger
    {
        public const string WindowsEventSource = "RSAgent";
        private const long MaxBytes = 5L * 1024L * 1024L;
        private static readonly object Sync = new object();

        public static string LogPath
        {
            get { return Path.Combine(AgentConfig.LogDir, "rs_agent.log"); }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + " :: " + ex);
        }

        public static void EventInfo(int eventId, string message)
        {
            Info(message);
            WriteWindowsEvent(eventId, message, EventLogEntryType.Information);
        }

        public static void EventWarning(int eventId, string message)
        {
            Warn(message);
            WriteWindowsEvent(eventId, message, EventLogEntryType.Warning);
        }

        public static void EventError(int eventId, string message, Exception ex)
        {
            var fullMessage = ex == null ? message : message + Environment.NewLine + ex;
            Error(message, ex);
            WriteWindowsEvent(eventId, fullMessage, EventLogEntryType.Error);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(AgentConfig.LogDir);
                    RotateIfNeeded();
                    File.AppendAllText(
                        LogPath,
                        string.Format(
                            "{0:yyyy-MM-dd HH:mm:ss.fff zzz} [{1}] [PID:{2} TID:{3}] {4}{5}",
                            DateTimeOffset.Now,
                            level,
                            Process.GetCurrentProcess().Id,
                            Thread.CurrentThread.ManagedThreadId,
                            message,
                            Environment.NewLine));
                }
            }
            catch
            {
                if (Environment.UserInteractive)
                {
                    Console.Error.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss.fff zzz} [{1}] {2}", DateTimeOffset.Now, level, message));
                }
            }
        }

        private static void WriteWindowsEvent(int eventId, string message, EventLogEntryType entryType)
        {
            try
            {
                EventLog.WriteEntry(WindowsEventSource, message, entryType, eventId);
            }
            catch (Exception ex)
            {
                Write("WARN", "No se pudo escribir el evento " + eventId + " en el registro Aplicación de Windows: " + ex.Message);
            }
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaxBytes)
            {
                return;
            }

            var oldest = LogPath + ".3";
            if (File.Exists(oldest)) File.Delete(oldest);

            for (var i = 2; i >= 1; i--)
            {
                var src = LogPath + "." + i;
                var dst = LogPath + "." + (i + 1);
                if (File.Exists(src)) File.Move(src, dst);
            }

            File.Move(LogPath, LogPath + ".1");
        }
    }
}
