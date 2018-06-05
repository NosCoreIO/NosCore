using NosCore.Core.Serializing;
using NosCore.Shared.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Help", Authority = AuthorityType.GameMaster)]
    public class HelpPacket : PacketDefinition { }
}