//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Fired by RequestNpcPacketHandler when a character opens an NPC or map object.
    public sealed record NpcTalkedEvent(ICharacterEntity Character, INonPlayableEntity Npc)
    {
        // Read when handled, possibly after a map change replaced a player's entity.
        public ICharacterEntity Character { get => field.Live(); init; } = Character;
    }
}
