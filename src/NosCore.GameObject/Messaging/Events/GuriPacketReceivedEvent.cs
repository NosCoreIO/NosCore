//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;

namespace NosCore.GameObject.Messaging.Events
{
    // Raised by GuriPacketHandler when a guri packet arrives. Every handler with a
    // Handle/HandleAsync(GuriPacketReceivedEvent) method runs and filters it itself.
    public sealed record GuriPacketReceivedEvent(ClientSession ClientSession, GuriPacket Packet) : IGameEvent, IPlayerEvent
    {
        public bool PlayerHasLeft => !ClientSession.HasPlayerEntity;
    }
}
