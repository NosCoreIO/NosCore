//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NosCore.Data.CommandPackets;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Interfaces;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The scenarios read and write packets as objects; NosCore.Packets turns them into lines and back.
    public static class Wire
    {
        private static readonly List<Type> Packets = typeof(IPacket).Assembly.GetTypes()
            .Concat(typeof(HelpPacket).Assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IPacket).IsAssignableFrom(type))
            .ToList();

        // Where a client and a server packet share a header, the client reads the server's; client packets are
        // still known for the ones a server packet carries, such as the answers of a dialog.
        private static readonly Deserializer FromServer = new(Packets.Where(IsServerPacket)
            .Concat(Packets.Where(type => !IsServerPacket(type) && !ServerHeaders.Contains(HeaderOf(type)))));

        private static readonly Serializer Writer = new(Packets);

        public static string Serialize(IPacket packet) =>
            packet is UndecodedPacket undecoded ? $"{undecoded.Line} (unreadable: {undecoded.Reason})" : Writer.Serialize(packet);

        public static IPacket Deserialize(string line)
        {
            try
            {
                return FromServer.Deserialize(line);
            }
            catch (ArgumentException ex)
            {
                return new UndecodedPacket { Header = line.Split(' ')[0], Line = line, Reason = ex.InnerException?.Message ?? ex.Message };
            }
        }

        private static HashSet<string> ServerHeaders => Packets.Where(IsServerPacket).Select(HeaderOf).ToHashSet();

        private static string HeaderOf(Type type) => type.GetCustomAttribute<PacketHeaderAttribute>()?.Identification ?? type.Name;

        private static bool IsServerPacket(Type type) => type.Namespace?.Contains(".ServerPackets", StringComparison.Ordinal) == true;
    }

    // A line NosCore.Packets could not read into its packet class.
    public sealed class UndecodedPacket : PacketBase
    {
        public string Line { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;
    }
}
