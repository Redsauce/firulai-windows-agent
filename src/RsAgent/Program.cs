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
                    Console.WriteLine("RSAgent Windows " + AgentConfig.AgentVersion);
                    Console.WriteLine("Uso: RsAgent.exe --run-once");
                    Console.WriteLine("Como servicio, instálalo con el instalador Inno Setup o sc.exe.");
                    return 0;
                }

                ServiceBase.Run(new RsAgentService());
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Fallo fatal", ex);
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
                Logger.Info("Desinstalacion: no existe ningun System en Firulai para UUID " + config.Uuid + ". Se permite la desinstalacion local.");
                if (Environment.UserInteractive) Console.WriteLine("No hay ningun System enlazado en Firulai para UUID " + config.Uuid + ". La desinstalacion local continuara.");
                return isLegacyUninstallRequest ? 0 : 2;
            }

            Logger.Info("Desinstalacion: System marcado como Disconnected en Firulai para UUID " + config.Uuid + ". Respuesta Firulai: " + result.Message);
            if (Environment.UserInteractive) Console.WriteLine("Sistema marcado como inactivo en Firulai para UUID " + config.Uuid + ".");
            return 0;
        }
    }
}
