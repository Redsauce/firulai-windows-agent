using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;

// Inspect the serializer creation in the installed SendAsync state machine.
// Never invoke SendAsync, the service, logger, configuration or transport.
internal static class InstalledInventoryLimitTests
{
    private static readonly Dictionary<short, OpCode> Opcodes = new Dictionary<short, OpCode>();
    private static List<MethodBase> Calls(MethodInfo method)
    {
        var result = new List<MethodBase>();
        var bytes = method.GetMethodBody().GetILAsByteArray();
        for (int i = 0; i < bytes.Length;) {
            short code = bytes[i++];
            if (code == 0xfe) code = (short)(0xfe00 | bytes[i++]);
            OpCode op;
            if (!Opcodes.TryGetValue(code, out op)) throw new Exception("Unknown IL opcode.");
            int size;
            switch (op.OperandType) {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                default: size = 4; break;
            }
            if (op.OperandType == OperandType.InlineMethod)
                result.Add(method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i)));
            i += size;
        }
        return result;
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Expected an agent binary copy.");
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            var opcode = (OpCode)field.GetValue(null);
            Opcodes[opcode.Value] = opcode;
        }
        var assembly = Assembly.LoadFrom(args[0]);
        var client = assembly.GetType("RsAgent.RsmClient", true);
        var send = client.GetMethod("SendAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (send == null) throw new Exception("Unknown binary: no SendAsync. Nothing executed.");
        var asyncState = send.GetCustomAttribute<AsyncStateMachineAttribute>();
        var inspected = asyncState == null ? send : asyncState.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var calls = Calls(inspected);
        JavaScriptSerializer serializer = null;
        bool defaultConstructor = false, localSetter = false;
        foreach (var call in calls) {
            if (call.DeclaringType == typeof(JavaScriptSerializer) && call.Name == ".ctor") defaultConstructor = true;
            if (call.DeclaringType == typeof(JavaScriptSerializer) && call.Name == "set_MaxJsonLength") localSetter = true;
            if (call.DeclaringType.FullName == "RsAgent.InventoryCollector" && call.Name == "CreateInventorySerializer") {
                var factory = (MethodInfo)call;
                // Only invoke the known pure factory, never arbitrary agent code.
                if (factory.DeclaringType.TypeInitializer != null || factory.ReturnType != typeof(JavaScriptSerializer))
                    throw new Exception("Unknown serializer factory. Nothing executed.");
                foreach (var nested in Calls(factory))
                    if (nested.DeclaringType != typeof(JavaScriptSerializer) || (nested.Name != ".ctor" && nested.Name != "set_MaxJsonLength"))
                        throw new Exception("Serializer factory has unexpected calls. Nothing executed.");
                serializer = (JavaScriptSerializer)factory.Invoke(null, null);
            }
        }
        if (serializer == null) {
            if (!defaultConstructor || localSetter) throw new Exception("Unrecognized upload serializer configuration. Nothing sent.");
            serializer = new JavaScriptSerializer();
        }
        Console.WriteLine("Binary assembly version: " + assembly.GetName().Version);
        Console.WriteLine("SendAsync serializer limit: " + serializer.MaxJsonLength + " characters");
        serializer.DeserializeObject("{\"components\":[]}");
        var payload = "{\"components\":[{\"name\":\"" + new string('x', 2100000) + "\",\"version\":\"1.0\"}]}";
        Console.WriteLine("Simulated inventory: " + payload.Length + " characters");
        try {
            var parsed = serializer.DeserializeObject(payload);
            serializer.Serialize(parsed);
            Console.WriteLine("CORRECTED: the installed binary's upload serializer accepts the large inventory.");
            Console.WriteLine("Serialization check only; no HTTP or RSM ingestion was tested.");
            return 0;
        }
        catch (ArgumentException) {
            if (payload.Length <= serializer.MaxJsonLength) throw;
            Console.WriteLine("LIMITED: the installed binary still has the old JSON size cap. The large inventory is rejected before HTTP.");
            Console.WriteLine("Expected result for the old release. No network or installed state was changed.");
            return 2;
        }
    }
}
