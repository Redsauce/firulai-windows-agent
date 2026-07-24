using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace RsAgent
{
    public sealed class RsAgentService : ServiceBase
    {
        private const int ScheduledHour = 3;
        private const int ScheduledMinute = 0;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MaximumTimerSlice = TimeSpan.FromMinutes(5);
        private readonly object _timerLock = new object();
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
        private Timer _timer;
        private volatile bool _stopping;
        private DateTime _nextRunAtLocal;
        private string _nextRunTrigger = "ninguno";

        public RsAgentService()
        {
            ServiceName = "RSAgent";
            CanStop = true;
            CanShutdown = true;
            CanHandlePowerEvent = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _stopping = false;
            Logger.EventInfo(
                1000,
                "Servicio iniciado. Version=" + AgentConfig.AgentVersion +
                ", equipo=" + Environment.MachineName +
                ", horaLocal=" + DateTimeOffset.Now.ToString("o") +
                ", zonaHoraria=" + TimeZoneInfo.Local.Id +
                ", log=" + Logger.LogPath + ".");

            Task.Run(async () =>
            {
                var now = DateTime.Now;
                var dailyRunDue = IsDailyRunDue(now);
                var trigger = dailyRunDue ? "recuperación-arranque" : "inicio-servicio";
                var requiredAfter = dailyRunDue ? GetScheduledTimeForDay(now.Date) : now;
                await ExecuteManagedRunAsync(trigger, requiredAfter).ConfigureAwait(false);
            });
        }

        protected override void OnStop()
        {
            StopService("Servicio detenido.");
        }

        protected override void OnShutdown()
        {
            StopService("Windows se está apagando; servicio detenido.");
            base.OnShutdown();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            DateTime nextRunAt;
            string nextRunTrigger;
            lock (_timerLock)
            {
                nextRunAt = _nextRunAtLocal;
                nextRunTrigger = _nextRunTrigger;
            }

            Logger.Info(
                "Evento de energía recibido. Estado=" + powerStatus +
                ", horaLocal=" + DateTimeOffset.Now.ToString("o") +
                ", próximaEjecución=" + FormatDate(nextRunAt) +
                ", tipo=" + nextRunTrigger + ".");

            if (powerStatus == PowerBroadcastStatus.ResumeAutomatic ||
                powerStatus == PowerBroadcastStatus.ResumeSuspend)
            {
                var now = DateTime.Now;
                if (IsDailyRunDue(now))
                {
                    var scheduledToday = GetScheduledTimeForDay(now.Date);
                    Logger.EventWarning(
                        1200,
                        "Windows se reanudó después de perder la ejecución de las 03:00. " +
                        "Se iniciará una ejecución de recuperación. Prevista=" + FormatDate(scheduledToday) +
                        ", reanudación=" + FormatDate(now) + ".");
                    Task.Run(() => ExecuteManagedRunAsync("recuperación-reanudación", scheduledToday));
                }
                else
                {
                    Logger.Info("No hay una ejecución diaria pendiente después de la reanudación.");
                }
            }

            return true;
        }

        public static async Task RunInventoryOnceAsync()
        {
            await RunInventoryOnceAsync("manual").ConfigureAwait(false);
        }

        private static async Task RunInventoryOnceAsync(string trigger)
        {
            var executionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();
            var phase = "preparación";
            Logger.EventInfo(1100, "Ejecución iniciada. Id=" + executionId + ", origen=" + trigger + ".");

            try
            {
                Directory.CreateDirectory(AgentConfig.DataDir);
                Directory.CreateDirectory(AgentConfig.LogDir);

                phase = "carga-configuración";
                var config = AgentConfig.Load();
                Logger.Info("Configuración cargada. Id=" + executionId + ", uuid=" + config.Uuid + ", api=" + GetSafeDestination(config.ApiUrl) + ".");

                phase = "recopilación-inventario";
                Logger.Info("Recopilando inventario. Id=" + executionId + ".");
                var inventoryJson = InventoryCollector.Collect(config);
                var outputPath = Path.Combine(AgentConfig.DataDir, "inventory.json");
                File.WriteAllText(outputPath, inventoryJson);
                Logger.Info("Inventario recopilado. Id=" + executionId + ", caracteres=" + inventoryJson.Length + ", fichero=" + outputPath + ".");

                phase = "envío-http";
                await RsmClient.SendAsync(config, inventoryJson).ConfigureAwait(false);

                phase = "guardado-estado";
                var completedAtUtc = DateTime.UtcNow;
                AgentState.RecordSuccess(completedAtUtc);
                Logger.EventInfo(
                    1101,
                    "Ejecución completada correctamente. Id=" + executionId +
                    ", origen=" + trigger +
                    ", duraciónMs=" + stopwatch.ElapsedMilliseconds +
                    ", estado=" + AgentConfig.StatePath + ".");
            }
            catch (Exception ex)
            {
                Logger.EventError(
                    1102,
                    "Ejecución fallida. Id=" + executionId +
                    ", origen=" + trigger +
                    ", fase=" + phase +
                    ", duraciónMs=" + stopwatch.ElapsedMilliseconds + ".",
                    ex);
                throw;
            }
        }

        private async Task ExecuteManagedRunAsync(string trigger, DateTime requiredAfterLocal)
        {
            await _executionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_stopping)
                {
                    Logger.Info("Ejecución omitida porque el servicio se está deteniendo. Origen=" + trigger + ".");
                    return;
                }

                CancelTimer();
                var lastSuccessLocal = GetLastSuccessLocal();
                if (lastSuccessLocal != DateTime.MinValue && lastSuccessLocal >= requiredAfterLocal)
                {
                    Logger.Info(
                        "Ejecución omitida porque la solicitud ya está satisfecha. Origen=" + trigger +
                        ", requeridaDesde=" + FormatDate(requiredAfterLocal) +
                        ", últimaCorrecta=" + FormatDate(lastSuccessLocal) + ".");
                    ScheduleNextRun();
                    return;
                }

                try
                {
                    await RunInventoryOnceAsync(trigger).ConfigureAwait(false);
                    ScheduleNextRun();
                }
                catch (Exception)
                {
                    ScheduleRetry(requiredAfterLocal);
                }
            }
            finally
            {
                _executionLock.Release();
            }
        }

        private void ScheduleNextRun()
        {
            if (_stopping) return;
            var nextRun = GetNextScheduledTime(DateTime.Now);
            ScheduleAt(nextRun, "programada-diaria", nextRun, true);
        }

        private void ScheduleRetry(DateTime requiredAfterLocal)
        {
            if (_stopping) return;
            ScheduleAt(DateTime.Now.Add(RetryDelay), "reintento", requiredAfterLocal, true);
        }

        private void ScheduleAt(DateTime targetLocal, string trigger, DateTime requiredAfterLocal, bool announce)
        {
            if (_stopping) return;

            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                }

                var remaining = targetLocal - DateTime.Now;
                var interval = remaining <= TimeSpan.Zero
                    ? TimeSpan.FromSeconds(1)
                    : (remaining > MaximumTimerSlice ? MaximumTimerSlice : remaining);
                var timer = new Timer(Math.Max(interval.TotalMilliseconds, 1000));
                timer.AutoReset = false;
                timer.Elapsed += async (sender, eventArgs) =>
                    await HandleTimerElapsedAsync(timer, targetLocal, trigger, requiredAfterLocal).ConfigureAwait(false);
                _timer = timer;
                _nextRunAtLocal = targetLocal;
                _nextRunTrigger = trigger;
                timer.Start();
            }

            if (announce)
            {
                Logger.Info(
                    "Ejecución programada. Tipo=" + trigger +
                    ", prevista=" + FormatDate(targetLocal) +
                    ", comprobaciónMáxima=" + MaximumTimerSlice + ".");
            }
        }

        private async Task HandleTimerElapsedAsync(Timer elapsedTimer, DateTime targetLocal, string trigger, DateTime requiredAfterLocal)
        {
            lock (_timerLock)
            {
                if (!ReferenceEquals(_timer, elapsedTimer) || _stopping)
                {
                    return;
                }

                _timer = null;
                elapsedTimer.Dispose();
            }

            var now = DateTime.Now;
            if (now < targetLocal)
            {
                ScheduleAt(targetLocal, trigger, requiredAfterLocal, false);
                return;
            }

            Logger.Info(
                "Temporizador vencido. Tipo=" + trigger +
                ", previsto=" + FormatDate(targetLocal) +
                ", real=" + FormatDate(now) +
                ", retraso=" + (now - targetLocal) + ".");
            await ExecuteManagedRunAsync(trigger, requiredAfterLocal).ConfigureAwait(false);
        }

        private void CancelTimer()
        {
            lock (_timerLock)
            {
                if (_timer == null) return;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private void StopService(string message)
        {
            _stopping = true;
            CancelTimer();
            Logger.EventInfo(1001, message);
        }

        private static bool IsDailyRunDue(DateTime nowLocal)
        {
            var scheduledToday = GetScheduledTimeForDay(nowLocal.Date);
            if (nowLocal < scheduledToday) return false;
            var lastSuccessLocal = GetLastSuccessLocal();
            return lastSuccessLocal == DateTime.MinValue || lastSuccessLocal < scheduledToday;
        }

        private static DateTime GetLastSuccessLocal()
        {
            var lastSuccessUtc = AgentState.GetLastSuccessUtc();
            return lastSuccessUtc == DateTime.MinValue ? DateTime.MinValue : lastSuccessUtc.ToLocalTime();
        }

        private static DateTime GetNextScheduledTime(DateTime nowLocal)
        {
            var next = GetScheduledTimeForDay(nowLocal.Date);
            return next <= nowLocal ? next.AddDays(1) : next;
        }

        private static DateTime GetScheduledTimeForDay(DateTime day)
        {
            return day.Date.AddHours(ScheduledHour).AddMinutes(ScheduledMinute);
        }

        private static string FormatDate(DateTime value)
        {
            return value == DateTime.MinValue ? "no-disponible" : value.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private static string GetSafeDestination(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                ? uri.Scheme + "://" + uri.Authority + uri.AbsolutePath
                : "URL no válida";
        }
    }
}
