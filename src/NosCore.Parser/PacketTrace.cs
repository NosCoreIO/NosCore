//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;

namespace NosCore.Parser
{
    // A recorded session holds what the server sent and what the client answered, so a header both sides
    // use reads as the server's packet.
    public class PacketTrace(ILogger<PacketTrace> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    {
        // The capture stands this in for the recording player's id.
        private const string RecordingPlayer = "MyIdPlayer";

        private static readonly Lazy<Deserializer> Deserializer = new(() =>
        {
            var packets = typeof(IPacket).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IPacket).IsAssignableFrom(t)).ToList();
            var serverPackets = packets.Where(IsServerPacket).ToList();
            var serverHeaders = serverPackets.Select(Header).ToHashSet();
            return new Deserializer(serverPackets.Concat(packets.Where(t => !IsServerPacket(t) && !serverHeaders.Contains(Header(t)))));
        });

        public async Task<List<IPacket>> ReadAsync(string file)
        {
            var packets = new List<IPacket>();
            var unreadable = 0;
            using var reader = new StreamReader(file, Encoding.Default);
            while (await reader.ReadLineAsync() is { } line)
            {
                if (line.Length == 0)
                {
                    continue;
                }

                try
                {
                    var packet = Deserializer.Value.Deserialize(line.Replace(RecordingPlayer, "0", StringComparison.Ordinal));
                    if (packet is not UnresolvedPacket)
                    {
                        packets.Add(packet);
                    }
                }
                catch (ArgumentException)
                {
                    unreadable++;
                }
            }

            if (unreadable > 0)
            {
                logger.LogWarning(logLanguage[LogLanguageKey.PACKET_CAPTURE_UNREADABLE], unreadable, file);
            }

            return packets;
        }

        private static bool IsServerPacket(Type type) => type.Namespace?.Contains(".ServerPackets", StringComparison.Ordinal) == true;

        private static string Header(Type type) => type.GetCustomAttribute<PacketHeaderAttribute>()?.Identification ?? type.Name;
    }
}
