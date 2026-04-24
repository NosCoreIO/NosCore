//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Monster kills are driven by the closing SuPacket (alive=0, hp%=0) — the client
    // plays the death animation from that alone and auto-despawns the sprite. Sending
    // an OutPacket here would cut the animation short, so we only clear aggro and
    // register the respawn timestamp on the map; the map's 400ms life loop reads the
    // pending-respawn table and revives monsters whose time is up.
    [UsedImplicitly]
    public sealed class MonsterRespawnHandler(IAggroService aggroService, IClock clock)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle monster) return Task.CompletedTask;
            if (monster.NpcMonster == null) return Task.CompletedTask;
            if (monster.MapInstance == null) return Task.CompletedTask;

            aggroService.Clear(monster);

            var respawnMs = Math.Max(1000, monster.NpcMonster.RespawnTime);
            monster.MapInstance.ScheduleRespawn(monster, clock.GetCurrentInstant().Plus(Duration.FromMilliseconds(respawnMs)));
            return Task.CompletedTask;
        }
    }
}
