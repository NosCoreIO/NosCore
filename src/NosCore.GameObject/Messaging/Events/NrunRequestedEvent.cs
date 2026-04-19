//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by NrunPacketHandler when a player triggers an "nrun" packet (NPC menu action).
    // Wolverine fans out to every handler with a Handle/HandleAsync(NrunRequestedEvent) method;
    // each handler filters by NrunRunnerType internally.
    public sealed record NrunRequestedEvent(ClientSession ClientSession, IAliveEntity? Target, NrunPacket Packet);
}
