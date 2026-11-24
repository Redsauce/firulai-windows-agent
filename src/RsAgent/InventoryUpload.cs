using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RsAgent
{
    // One durable compressed snapshot, replayed in full after uncertain delivery.
    // HTTP success means queued; receiver deduplication owns processing completion.
    internal static class InventoryUpload
    {
        internal const int PartBytes = 3 * 1024 * 1024;
        internal const int MaxRawBytes = 64 * 1024 * 1024;
        internal static string TestSpoolDirectory { get; set; }
        internal static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }; }
        internal static string Hash(byte[] data)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        internal static byte[] Compress(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true)) gzip.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }
        private static void SecureSpool(string root)
        {
            if (TestSpoolDirectory == null && (File.GetAttributes(AgentConfig.DataDir) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Inventory data directory is a reparse point.");
            if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Inventory staging directory is a reparse point.");
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(true, false);
            var owner = TestSpoolDirectory == null ? new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) : WindowsIdentity.GetCurrent().User;
            acl.SetOwner(owner);
            foreach (var sid in new[] { new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), owner })
                acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.CreateDirectory(root, acl);
            Directory.SetAccessControl(root, acl);
        }

        internal static async Task SendAsync(AgentConfig config, string json, Func<string, Task> send)
        {
            var raw = Encoding.UTF8.GetBytes(json);
            if (raw.Length > MaxRawBytes) throw new InvalidOperationException("Inventory exceeds the 64 MiB transport limit.");
            var serializer = Serializer();
            var inventory = serializer.Deserialize<Dictionary<string, object>>(json);
            var identity = config.Uuid + "|" + config.Token;
            var root = TestSpoolDirectory ?? Path.Combine(AgentConfig.DataDir, "inventory-uploads");
            SecureSpool(root);
            var scope = Path.Combine(root, Hash(Encoding.UTF8.GetBytes(identity)));
            foreach (var suffix in new[] { ".lock", ".pending", ".pending.tmp" })
                if (File.Exists(scope + suffix) && (File.GetAttributes(scope + suffix) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Inventory staging file is a reparse point.");
            using (var gate = new FileStream(scope + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var pending = scope + ".pending";
                string resumedHash = null;
                if (File.Exists(pending))
                {
                    resumedHash = await SendPending(pending, config.Token, send).ConfigureAwait(false);
                    File.Delete(pending);
                }
                if (resumedHash == Hash(raw)) return;
                if (raw.Length <= 6 * 1024 * 1024)
                {
                    await send(RsmClient.PrepareInventoryTransport(json)).ConfigureAwait(false);
                    return;
                }
                object systemValue;
                if (!inventory.TryGetValue("system", out systemValue)) throw new InvalidOperationException("Inventory system identity is required.");
                var system = systemValue as Dictionary<string, object>;
                var safeSystem = new Dictionary<string, object>();
                foreach (var key in new[] { "uuid", "hostname", "fqdn" })
                {
                    object value;
                    if (system == null || !system.TryGetValue(key, out value) || !(value is string) || string.IsNullOrWhiteSpace((string)value))
                        throw new InvalidOperationException("Inventory system identity is incomplete.");
                    safeSystem[key] = value;
                }
                if ((string)safeSystem["uuid"] != config.Uuid) throw new InvalidOperationException("Inventory UUID does not match agent configuration.");
                var packed = Compress(raw);
                var rawHash = Hash(raw);
                var packedHash = Hash(packed);
                var metadata = new Dictionary<string, object> {
                    { "inventory_transport", "gzip-base64-chunks-v1" },
                    { "upload_id", Hash(Encoding.ASCII.GetBytes(rawHash + packedHash)) },
                    { "uncompressed_bytes", raw.Length }, { "sha256", rawHash },
                    { "compressed_bytes", packed.Length }, { "compressed_sha256", packedHash },
                    { "total_parts", (packed.Length + PartBytes - 1) / PartBytes },
                    { "system", safeSystem }
                };
                var header = Encoding.UTF8.GetBytes(serializer.Serialize(metadata));
                var temporary = pending + ".tmp";
                try
                {
                    using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new BinaryWriter(file))
                    {
                        writer.Write(header.Length); writer.Write(header); writer.Write(packed); file.Flush(true);
                    }
                    File.Move(temporary, pending);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                await SendPending(pending, config.Token, send).ConfigureAwait(false);
                File.Delete(pending);
            }
        }

        private static async Task<string> SendPending(string path, string token, Func<string, Task> send)
        {
            var serializer = Serializer();
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(file))
            {
                var headerLength = reader.ReadInt32();
                if (headerLength <= 0 || headerLength > 65536) throw new InvalidOperationException("Invalid pending inventory header.");
                var metadata = serializer.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(reader.ReadBytes(headerLength)));
                var offset = file.Position;
                var total = Convert.ToInt32(metadata["compressed_bytes"]);
                if (total <= 0 || total > 65 * 1024 * 1024 || file.Length - offset != total)
                    throw new InvalidOperationException("Pending inventory is truncated or too large.");
                using (var sha = SHA256.Create())
                {
                    var actual = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                    if (actual != (string)metadata["compressed_sha256"]) throw new InvalidOperationException("Pending inventory hash mismatch.");
                }
                file.Position = offset;
                var count = (total + PartBytes - 1) / PartBytes;
                if (Convert.ToInt32(metadata["total_parts"]) != count) throw new InvalidOperationException("Invalid pending inventory part count.");
                for (var index = 0; index < count; index++)
                {
                    var bytes = reader.ReadBytes(Math.Min(PartBytes, total - index * PartBytes));
                    if (bytes.Length != Math.Min(PartBytes, total - index * PartBytes)) throw new InvalidOperationException("Truncated pending inventory.");
                    var frame = new Dictionary<string, object>(metadata) {
                        { "part_index", index }, { "part_sha256", Hash(bytes) },
                        { "RStoken", token }, { "data", Convert.ToBase64String(bytes) }
                    };
                    await send(serializer.Serialize(frame)).ConfigureAwait(false);
                }
                return (string)metadata["sha256"];
            }
        }
    }
}
