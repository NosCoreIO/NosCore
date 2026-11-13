//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    public sealed record EntityDiedEvent(
        IAliveEntity Victim,
        IAliveEntity? Killer,
        IReadOnlyDictionary<Entity, int> HitSnapshot)
    {
        public EntityDiedEvent(IAliveEntity victim, IAliveEntity? killer)
            : this(victim, killer, SnapshotHits(victim)) { }

        // Read when handled, possibly after a map change replaced a player's entity.
        public IAliveEntity Victim { get => field.Live(); init; } = Victim;

        public IAliveEntity? Killer { get => field?.Live(); init; } = Killer;

        private static IReadOnlyDictionary<Entity, int> SnapshotHits(IAliveEntity victim)
            => victim.HitList is { } list
                ? list.ToArray().ToDictionary(kv => kv.Key, kv => kv.Value)
                : new Dictionary<Entity, int>();
    }
}
