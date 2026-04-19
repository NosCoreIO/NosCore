//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by GuriPacketHandler when a guri packet arrives. Wolverine fans out to every handler
    // with a Handle/HandleAsync(GuriPacketReceivedEvent) method; each handler filters internally
    // (no central Condition() registry).
    public sealed record GuriPacketReceivedEvent(ClientSession ClientSession, GuriPacket Packet);
}
