using System;
using System.Reflection;
using System.ServiceProcess;
[assembly: AssemblyVersion("0.20.19")]
[assembly: AssemblyFileVersion("0.20.19")]
// Disposable CI service. No inventory, credentials or networking.
internal sealed class UpgradeServiceFixture : ServiceBase
{
    private UpgradeServiceFixture() { ServiceName = "firulaiAgent"; }
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--hold-image")
        {
            Console.WriteLine("ready");
            Console.ReadLine();
        }
        else ServiceBase.Run(new UpgradeServiceFixture());
    }
}
