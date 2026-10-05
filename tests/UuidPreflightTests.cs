using System;
using System.Reflection;
using System.Web.Script.Serialization;

internal static class UuidPreflightTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Expected RsAgent.exe path.");
        var assembly = Assembly.LoadFrom(args[0]);
        var client = assembly.GetType("RsAgent.RsmClient", true);
        var count = client.GetMethod("CountSystemUuidMatches", BindingFlags.NonPublic | BindingFlags.Static);
        Check(count != null, "Missing UUID matcher.");

        const string uuid = "e738584b-93fd-4604-bc2c-9dad0c50d00d";
        var serializer = new JavaScriptSerializer();
        Func<string, int> matches = json => (int)count.Invoke(null, new object[] { serializer.DeserializeObject(json), uuid });
        Check(matches("[]") == 0, "Empty response must not pass.");
        Check(matches("[{\"ID\":\"42\",\"1780\":\"" + uuid + "\"}]") == 1, "One exact UUID must pass.");
        Check(matches("{\"items\":[{\"ID\":\"42\",\"1780\":\"" + uuid + "\"}]}") == 1, "Wrapped response must pass.");
        Check(matches("[{\"1780\":\"" + uuid + "\"},{\"1780\":\"" + uuid + "\"}]") == 2, "Duplicates must be detected.");
        Check(matches("[{\"1780\":\"00000000-0000-0000-0000-000000000000\"}]") == 0, "Another UUID must not pass.");
        Console.WriteLine("UUID preflight matching: OK");
        return 0;
    }
}
