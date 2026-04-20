//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Published once per kill. The reward handler converts HitList contributions to XP /
    // gold / drops; AI handlers clear aggro and schedule respawns; the revival handler
    // branches on `RevivalMode` to decide whether to warp or refill in place.
    public sealed record EntityDiedEvent(IAliveEntity Victim, IAliveEntity? Killer, RevivalMode RevivalMode = RevivalMode.Normal);

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
