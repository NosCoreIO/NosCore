//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

        private static readonly Regex SessionStart = new(@"^=== session started (\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) ===$", RegexOptions.Compiled);

        private static readonly Regex WorldLine = new(@"^\[[^\]]*\] \[World\] \[(?:Server|Client)\] (.*)$", RegexOptions.Compiled);

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
                if (!TryRead(line, packets))
                {
                    unreadable++;
                }
            }

            WarnUnreadable(unreadable, file);
            return packets;
        }

        // A capture log holds one or more sessions, each opened by "=== session started <date> <time> ===",
        // with one "[time] [World] [Server|Client] <packet>" line per packet. Anything else in the folder is
        // not a capture and yields no session.
        public async Task<List<CapturedSession>> ReadCaptureLogAsync(string file)
        {
            var sessions = new List<CapturedSession>();
            var unreadable = 0;
            using var reader = new StreamReader(file, Encoding.Default);
            while (await reader.ReadLineAsync() is { } line)
            {
                var start = SessionStart.Match(line);
                if (start.Success)
                {
                    sessions.Add(new CapturedSession(DateTime.ParseExact(start.Groups[1].Value, "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture), []));
                    continue;
                }

                if (sessions.Count == 0)
                {
                    return sessions;
                }

                var world = WorldLine.Match(line);
                if (world.Success && !TryRead(world.Groups[1].Value, sessions[^1].Packets))
                {
                    unreadable++;
                }
            }

            WarnUnreadable(unreadable, file);
            return sessions;
        }

        private static bool TryRead(string line, List<IPacket> packets)
        {
            if (line.Length == 0)
            {
                return true;
            }

            try
            {
                var packet = Deserializer.Value.Deserialize(line.Replace(RecordingPlayer, "0", StringComparison.Ordinal));
                if (packet is not UnresolvedPacket)
                {
                    packets.Add(packet);
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void WarnUnreadable(int unreadable, string file)
        {
            if (unreadable > 0)
            {
                logger.LogWarning(logLanguage[LogLanguageKey.PACKET_CAPTURE_UNREADABLE], unreadable, file);
            }
        }

        private static bool IsServerPacket(Type type) => type.Namespace?.Contains(".ServerPackets", StringComparison.Ordinal) == true;

        private static string Header(Type type) => type.GetCustomAttribute<PacketHeaderAttribute>()?.Identification ?? type.Name;
    }

    public sealed record CapturedSession(DateTime Start, List<IPacket> Packets);
}
