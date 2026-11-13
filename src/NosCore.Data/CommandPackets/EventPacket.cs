//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Event;
using NosCore.Packets.Attributes;
using NosCore.Shared.Enumerations;

namespace NosCore.Data.CommandPackets
{
    [CommandPacketHeader("$Event", AuthorityType.GameMaster)]
    public class EventPacket : CommandPacket
    {
        [PacketIndex(0)]
        public EventType EventType { get; set; }

        public override string Help()
        {
            return "$Event EventType";
        }
    }
}
