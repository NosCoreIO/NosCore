//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;

namespace NosCore.GameObject.Messaging.Events
{
    // Published when items land in a character's inventory from the world (a picked-up drop).
    public sealed record ItemsReceivedEvent(PlayerComponentBundle Character, short VNum, short Amount) : IPlayerEvent
    {
        // Read when handled, possibly after a map change replaced a player's entity.
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;

        public bool PlayerHasLeft => Character.HasLeft();
    }
}
