//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Published after every landed hit. Aggro, quest trackers and anything else that
    // wants to react to damage without sitting on the hot path subscribes here.
    public sealed record EntityDamagedEvent(IAliveEntity Attacker, IAliveEntity Target, int Damage, bool Killed)
    {
        // Read when handled, possibly after a map change replaced a player's entity.
        public IAliveEntity Attacker { get => field.Live(); init; } = Attacker;

        public IAliveEntity Target { get => field.Live(); init; } = Target;
    }
}
