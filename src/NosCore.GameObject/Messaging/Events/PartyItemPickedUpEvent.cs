//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Raised for each party member on the map when another member picks up a quest item that never
    // reaches a bag.
    public sealed record PartyItemPickedUpEvent(PlayerComponentBundle Character, short VNum) : IGameEvent, IPlayerEvent
    {
        public IAliveEntity? Subject => Character;

        // Read when handled, possibly after a map change replaced a player's entity.
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;

        public bool PlayerHasLeft => Character.HasLeft();
    }
}
