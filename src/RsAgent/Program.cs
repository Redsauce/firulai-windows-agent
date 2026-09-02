using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace RsAgent
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private const uint MessageBoxYesNo = 0x00000004;
        private const uint MessageBoxIconQuestion = 0x00000020;
        private const uint MessageBoxDefaultButton2 = 0x00000100;
        private const int IdYes = 6;

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

        private static int LaunchLocalizedUninstaller(string[] args)
        {
            var locale = ReadInstalledLocale();
            var uninstallerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unins000.exe");

            if (!File.Exists(uninstallerPath))
            {
                Console.Error.WriteLine("Uninstaller not found: " + uninstallerPath);
                return 1;
            }

            var isQuiet = HasArgument(args, "/VERYSILENT");
            if (!isQuiet && MessageBox(IntPtr.Zero, UninstallConfirmationForLocale(locale),
                    UninstallTitleForLocale(locale), MessageBoxYesNo | MessageBoxIconQuestion | MessageBoxDefaultButton2) != IdYes)
            {
                return 0;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = uninstallerPath,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false
            };

            Process.Start(startInfo);
            return 0;
        }

        private static bool HasArgument(string[] args, string expected)
        {
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i].Equals(expected, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string UninstallTitleForLocale(string locale)
        {
            var language = (locale ?? string.Empty).Trim().ToLowerInvariant();
            if (language.StartsWith("es")) return "Desinstalar - Firulai Inventory Agent";
            if (language.StartsWith("ca")) return "Desinstal·lar - Firulai Inventory Agent";
            if (language.StartsWith("eu")) return "Desinstalatu - Firulai Inventory Agent";
            if (language.StartsWith("gl")) return "Desinstalar - Firulai Inventory Agent";
            if (language.StartsWith("fr")) return "Désinstaller - Firulai Inventory Agent";
            if (language.StartsWith("de")) return "Deinstallieren - Firulai Inventory Agent";
            if (language.StartsWith("it")) return "Disinstallare - Firulai Inventory Agent";
            if (language.StartsWith("ja")) return "Firulai Inventory Agent のアンインストール";
            if (language.StartsWith("zh")) return "卸载 - Firulai Inventory Agent";
            return "Uninstall - Firulai Inventory Agent";
        }

        private static string UninstallConfirmationForLocale(string locale)
        {
            var language = (locale ?? string.Empty).Trim().ToLowerInvariant();
            if (language.StartsWith("es")) return "Se va a desinstalar Firulai Inventory Agent.\r\n\r\nEsta acción solo eliminará los archivos locales del agente y el instalador. Los datos de Firulai no se borrarán.\r\n\r\n¿Quieres continuar?";
            if (language.StartsWith("ca")) return "Es desinstal·larà Firulai Inventory Agent.\r\n\r\nAquesta acció només eliminarà els fitxers locals de l'agent i l'instal·lador. Les dades de Firulai no s'eliminaran.\r\n\r\nVols continuar?";
            if (language.StartsWith("eu")) return "Firulai Inventory Agent desinstalatuko da.\r\n\r\nEkintza honek agentearen fitxategi lokalak eta instalatzailea bakarrik kenduko ditu. Firulaiko datuak ez dira ezabatuko.\r\r\n\r\nJarraitu nahi duzu?";
            if (language.StartsWith("gl")) return "Vaise desinstalar Firulai Inventory Agent.\r\n\r\nEsta acción só eliminará os ficheiros locais do axente e o instalador. Os datos de Firulai non se borrarán.\r\n\r\nQueres continuar?";
            if (language.StartsWith("fr")) return "Firulai Inventory Agent va être désinstallé.\r\n\r\nCette action supprimera uniquement les fichiers locaux de l'agent et l'installateur. Les données Firulai ne seront pas supprimées.\r\n\r\nVoulez-vous continuer ?";
            if (language.StartsWith("de")) return "Firulai Inventory Agent wird deinstalliert.\r\n\r\nDiese Aktion entfernt nur die lokalen Agent-Dateien und das Installationsprogramm. Firulai-Daten werden nicht gelöscht.\r\n\r\nMöchten Sie fortfahren?";
            if (language.StartsWith("it")) return "Firulai Inventory Agent verrà disinstallato.\r\n\r\nQuesta azione rimuoverà solo i file locali dell'agente e il programma di installazione. I dati Firulai non verranno eliminati.\r\n\r\nVuoi continuare?";
            if (language.StartsWith("ja")) return "Firulai Inventory Agent をアンインストールします。\r\n\r\nこの操作では、ローカルのエージェント ファイルとインストーラーのみを削除します。Firulai のデータは削除されません。\r\n\r\n続行しますか?";
            if (language.StartsWith("zh")) return "即将卸载 Firulai Inventory Agent。\r\n\r\n此操作只会删除本地代理文件和安装程序。Firulai 数据不会被删除。\r\n\r\n是否继续?";
            return "Firulai Inventory Agent will be uninstalled.\r\n\r\nThis action will only remove the local agent files and the installer. Firulai data will not be deleted.\r\n\r\nDo you want to continue?";
        }

        private static string ReadInstalledLocale()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Redsauce\RSAgent"))
                {
                    var value = key == null ? null : key.GetValue("Locale") as string;
                    if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                }
            }
            catch
            {
                // Fall back to English if the registry value is unavailable.
            }

            return "en_US";
        }

    }
}
