//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    public sealed record EntityDiedEvent(
        IAliveEntity Victim,
        IAliveEntity? Killer,
        IReadOnlyDictionary<Entity, int> HitSnapshot,
        RevivalMode RevivalMode = RevivalMode.Normal)
    {
        public EntityDiedEvent(IAliveEntity victim, IAliveEntity? killer, RevivalMode revivalMode = RevivalMode.Normal)
            : this(victim, killer, SnapshotHits(victim), revivalMode) { }

        private static IReadOnlyDictionary<Entity, int> SnapshotHits(IAliveEntity victim)
            => victim.HitList is { } list
                ? list.ToArray().ToDictionary(kv => kv.Key, kv => kv.Value)
                : new Dictionary<Entity, int>();
    }

    // Normal = in-session death → town warp after the death-pose delay (OpenNos
    // default when the revive dialog's "return to town" option is picked / the
    // dialog times out).
    // ResumeInPlace = we detected a persisted-dead character on login: the player
    // already "declined" the dialog by disconnecting, so just refill and leave
    // them at the saved position instead of warping.
    public enum RevivalMode
    {
        Normal,
        ResumeInPlace,
    }
}
