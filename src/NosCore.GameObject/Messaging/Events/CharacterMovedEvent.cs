//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Fired by WalkPacketHandler after the character's position has been updated
    // on the server. Subscribers decide what to do with the new coordinates:
    // GoTo quest validators, proximity triggers, aggro range checks, etc. The
    // packet handler itself knows nothing about any of them.
    public sealed record CharacterMovedEvent(PlayerComponentBundle Character, short X, short Y, short MapId)
    {
        // Read when handled, possibly after a map change replaced a player's entity.
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;
    }
}
