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
        private string _nextRunTrigger = AgentText.T("service.triggerNone");

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
            AgentText.SetLocale(AgentConfig.LoadLocaleOrDefault());
            _nextRunTrigger = AgentText.T("service.triggerNone");
            _stopping = false;
            Logger.EventInfo(
                1000,
                AgentText.T(
                    "service.started",
                    AgentConfig.AgentVersion,
                    Environment.MachineName,
                    DateTimeOffset.Now.ToString("o"),
                    TimeZoneInfo.Local.Id,
                    Logger.LogPath));

            Task.Run(async () =>
            {
                var now = DateTime.Now;
                var dailyRunDue = IsDailyRunDue(now);
                var trigger = dailyRunDue ? AgentText.T("service.triggerStartupRecovery") : AgentText.T("service.triggerServiceStart");
                var requiredAfter = dailyRunDue ? GetScheduledTimeForDay(now.Date) : now;
                await ExecuteManagedRunAsync(trigger, requiredAfter).ConfigureAwait(false);
            });
        }

        protected override void OnStop()
        {
            StopService(AgentText.T("service.stopped"));
        }

        protected override void OnShutdown()
        {
            StopService(AgentText.T("service.shutdown"));
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

            Logger.Info(AgentText.T(
                "service.powerEvent",
                powerStatus,
                DateTimeOffset.Now.ToString("o"),
                FormatDate(nextRunAt),
                nextRunTrigger));

            if (powerStatus == PowerBroadcastStatus.ResumeAutomatic ||
                powerStatus == PowerBroadcastStatus.ResumeSuspend)
            {
                var now = DateTime.Now;
                if (IsDailyRunDue(now))
                {
                    var scheduledToday = GetScheduledTimeForDay(now.Date);
                    Logger.EventWarning(
                        1200,
                        AgentText.T("service.resumeMissedRun", FormatDate(scheduledToday), FormatDate(now)));
                    Task.Run(() => ExecuteManagedRunAsync(AgentText.T("service.triggerResumeRecovery"), scheduledToday));
                }
                else
                {
                    Logger.Info(AgentText.T("service.noDailyPendingAfterResume"));
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
            AgentText.SetLocale(AgentConfig.LoadLocaleOrDefault());
            var executionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var stopwatch = Stopwatch.StartNew();
            var phase = AgentText.T("service.phasePreparation");
            Logger.EventInfo(1100, AgentText.T("service.executionStarted", executionId, trigger));

            try
            {
                Directory.CreateDirectory(AgentConfig.DataDir);
                Directory.CreateDirectory(AgentConfig.LogDir);

                phase = AgentText.T("service.phaseConfigLoad");
                var config = AgentConfig.Load();
                Logger.Info(AgentText.T("service.configLoaded", executionId, config.Uuid, GetSafeDestination(config.ApiUrl), config.Locale));

                phase = AgentText.T("service.phaseInventoryCollection");
                Logger.Info(AgentText.T("service.collectingInventory", executionId));
                var inventoryJson = InventoryCollector.Collect(config);
                var outputPath = Path.Combine(AgentConfig.DataDir, "inventory.json");
                File.WriteAllText(outputPath, inventoryJson);
                Logger.Info(AgentText.T("service.inventoryCollected", executionId, inventoryJson.Length, outputPath));

                phase = AgentText.T("service.phaseHttpSend");
                await RsmClient.SendAsync(config, inventoryJson).ConfigureAwait(false);

                phase = AgentText.T("service.phaseStateSave");
                var completedAtUtc = DateTime.UtcNow;
                AgentState.RecordSuccess(completedAtUtc);
                Logger.EventInfo(
                    1101,
                    AgentText.T("service.executionCompleted", executionId, trigger, stopwatch.ElapsedMilliseconds, AgentConfig.StatePath));
            }
            catch (Exception ex)
            {
                Logger.EventError(
                    1102,
                    AgentText.T("service.executionFailed", executionId, trigger, phase, stopwatch.ElapsedMilliseconds),
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
                    Logger.Info(AgentText.T("service.skippedStopping", trigger));
                    return;
                }

                CancelTimer();
                var lastSuccessLocal = GetLastSuccessLocal();
                if (lastSuccessLocal != DateTime.MinValue && lastSuccessLocal >= requiredAfterLocal)
                {
                    Logger.Info(AgentText.T("service.skippedSatisfied", trigger, FormatDate(requiredAfterLocal), FormatDate(lastSuccessLocal)));
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
            ScheduleAt(nextRun, AgentText.T("service.triggerDailyScheduled"), nextRun, true);
        }

        private void ScheduleRetry(DateTime requiredAfterLocal)
        {
            if (_stopping) return;
            ScheduleAt(DateTime.Now.Add(RetryDelay), AgentText.T("service.triggerRetry"), requiredAfterLocal, true);
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
                Logger.Info(AgentText.T("service.scheduled", trigger, FormatDate(targetLocal), MaximumTimerSlice));
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

            Logger.Info(AgentText.T("service.timerElapsed", trigger, FormatDate(targetLocal), FormatDate(now), now - targetLocal));
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
            return value == DateTime.MinValue ? AgentText.T("service.unavailable") : value.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private static string GetSafeDestination(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                ? uri.Scheme + "://" + uri.Authority + uri.AbsolutePath
                : AgentText.T("rsm.invalidUrl");
        }
    }
}
