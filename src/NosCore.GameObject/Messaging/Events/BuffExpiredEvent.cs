//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;

namespace NosCore.GameObject.Messaging.Events
{
    // A card on a player ran its full time, as opposed to being taken off early.
    public sealed record BuffExpiredEvent(PlayerComponentBundle Character, short CardId) : IPlayerEvent
    {
        // Read when handled, possibly after a map change replaced a player's entity.
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;

        public bool PlayerHasLeft => Character.HasLeft();
    }
}
