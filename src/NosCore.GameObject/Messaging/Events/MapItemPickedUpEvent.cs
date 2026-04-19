//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Drops;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by GetPacketHandler when a player picks up a map item. Wolverine fans out to every
    // handler with a Handle/HandleAsync(MapItemPickedUpEvent) method; each handler filters by
    // item type / vnum internally.
    public sealed record MapItemPickedUpEvent(ClientSession ClientSession, MapItemComponentBundle MapItem, GetPacket Packet);
}
