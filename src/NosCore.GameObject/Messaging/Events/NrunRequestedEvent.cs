//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;

namespace NosCore.GameObject.Messaging.Events
{
    // Published by NrunPacketHandler when a player triggers an "nrun" packet (NPC menu action).
    // Wolverine fans out to every handler with a Handle/HandleAsync(NrunRequestedEvent) method;
    // each handler filters by NrunRunnerType internally.
    //
    // Target is the entity the runner targets (NPC bundle, character, or null) — kept loosely typed
    // because handlers expect different shapes (NpcComponentBundle for shop/teleport, etc.).
    public sealed record NrunRequestedEvent(ClientSession ClientSession, object? Target, NrunPacket Packet);
}
