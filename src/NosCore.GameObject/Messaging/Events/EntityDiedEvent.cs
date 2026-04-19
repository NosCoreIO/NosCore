//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Messaging.Events
{
    // Published once per kill. The reward handler converts HitList contributions to XP /
    // gold / drops; AI handlers clear aggro and schedule respawns.
    public sealed record EntityDiedEvent(IAliveEntity Victim, IAliveEntity? Killer);
}
