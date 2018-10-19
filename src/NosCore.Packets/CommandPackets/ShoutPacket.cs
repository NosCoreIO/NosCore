using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Shout", Authority = AuthorityType.Moderator)]
    public class ShoutPacket : PacketDefinition
    {
        [PacketIndex(0, SerializeToEnd = true)]
        public string Message { get; set; }
    }
}