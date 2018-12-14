using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Account;

namespace NosCore.Packets.CommandPackets
{
    [PacketHeader("$Kick", Authority = AuthorityType.Moderator)]
    public class KickPacket : PacketDefinition, ICommandPacket
    {
        [PacketIndex(0)]
        public string Name { get; set; }

        public string Help()
        {
            return "$Kick VALUE";
        }
    }
}