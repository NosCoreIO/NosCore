//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;

namespace NosCore.GameObject.Messaging.Events
{
    // Raised for each player still inside when a time-space run ends in any way but success.
    public sealed record TimeSpaceFailedEvent(PlayerComponentBundle Character, short TimeSpaceId)
        : IGameEvent, IPlayerEvent
    {
        public PlayerComponentBundle Character { get => field.Live(); init; } = Character;

        public bool PlayerHasLeft => Character.HasLeft();
    }
}
