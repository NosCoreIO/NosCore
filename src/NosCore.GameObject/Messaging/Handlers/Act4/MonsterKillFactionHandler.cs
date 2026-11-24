//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.FactionWarService;

namespace NosCore.GameObject.Messaging.Handlers.Act4
{
    // A monster killed on a Glacernon map moves its killer's side, or its owner's for a pet's kill.
    [UsedImplicitly]
    public sealed class MonsterKillFactionHandler(IFactionWarService factionWarService, ISessionRegistry sessionRegistry)
    {
        [UsedImplicitly]
        public Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim is not MonsterComponentBundle { MapInstance: { IsAct4: true, MapInstanceType: MapInstanceType.BaseMapInstance } } monster
                || monster.NpcMonster.Role == MonsterRole.FactionGuardian)
            {
                return Task.CompletedTask;
            }

            return Killer(evt) is { } killer ? factionWarService.RecordMonsterKillAsync(killer) : Task.CompletedTask;
        }

        private PlayerComponentBundle? Killer(EntityDiedEvent evt) => evt.Killer switch
        {
            PlayerComponentBundle player => player,
            MateComponentBundle mate when sessionRegistry.TryGetCharacter(s => s.CharacterId == mate.OwnerId, out var owner) => owner,
            _ => null,
        };
    }
}
