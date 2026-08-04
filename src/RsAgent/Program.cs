using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace RsAgent
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var isUninstallStatusUpdateRequest =
                args.Length > 0 &&
                (args[0].Equals("--mark-disconnected-on-uninstall", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("--request-delete-on-uninstall", StringComparison.OrdinalIgnoreCase));
            var isLegacyUninstallRequest =
                args.Length > 0 &&
                args[0].Equals("--request-delete-on-uninstall", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (args.Length > 0 && args[0].Equals("--run-once", StringComparison.OrdinalIgnoreCase))
                {
                    Task.Run(() => RsAgentService.RunInventoryOnceAsync()).GetAwaiter().GetResult();
                    return 0;
                }

                if (isUninstallStatusUpdateRequest)
                {
                    return Task.Run(() => MarkDisconnectedOnUninstallAsync(isLegacyUninstallRequest)).GetAwaiter().GetResult();
                }

                if (Environment.UserInteractive)
                {
                    AgentText.SetLocale(AgentConfig.LoadLocaleOrDefault());
                    Console.WriteLine("RSAgent Windows " + AgentConfig.AgentVersion);
                    Console.WriteLine(AgentText.T("program.usageRunOnce"));
                    Console.WriteLine(AgentText.T("program.usageService"));
                    return 0;
                }

                ServiceBase.Run(new RsAgentService());
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(AgentText.T("program.fatal"), ex);
                if (Environment.UserInteractive) Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<int> MarkDisconnectedOnUninstallAsync(bool isLegacyUninstallRequest)
        {
            var config = AgentConfig.Load();

            var result = await RsmClient.MarkSystemDisconnectedOnUninstallAsync(config).ConfigureAwait(false);
            if (!result.SystemFound)
            {
                Logger.Info(AgentText.T("program.uninstallNoSystemLog", config.Uuid));
                if (Environment.UserInteractive) Console.WriteLine(AgentText.T("program.uninstallNoSystemConsole", config.Uuid));
                return isLegacyUninstallRequest ? 0 : 2;
            }

            Logger.Info(AgentText.T("program.uninstallDisconnectedLog", config.Uuid, result.Message));
            if (Environment.UserInteractive) Console.WriteLine(AgentText.T("program.uninstallDisconnectedConsole", config.Uuid));
            return 0;
        }
    }
}
