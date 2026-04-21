//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class DeathBCardHandler(INpcCombatCatalog catalog)
    {
        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Killer is not PlayerComponentBundle killer) return;
            if (evt.Victim is not INonPlayableEntity victim || victim.NpcMonster == null) return;

            var bcards = catalog.GetDeathBCards(victim.NpcMonster.NpcMonsterVNum);
            if (bcards.Count == 0) return;

            var changed = false;
            foreach (var bcard in bcards)
            {
                if ((BCardType.CardType)bcard.Type != BCardType.CardType.SpecialEffects) continue;

                var sub = (AdditionalTypes.SpecialEffects)bcard.SubType;
                switch (sub)
                {
                    case AdditionalTypes.SpecialEffects.DecreaseKillerHp:
                        var damage = killer.MaxHp * bcard.FirstData / 100;
                        killer.Hp = Math.Max(1, killer.Hp - damage);
                        changed = true;
                        break;
                    case AdditionalTypes.SpecialEffects.IncreaseKillerHp:
                        var heal = killer.MaxHp * bcard.FirstData / 100;
                        killer.Hp = Math.Min(killer.MaxHp, killer.Hp + heal);
                        changed = true;
                        break;
                }
            }

            if (!changed) return;

            await killer.SendPacketAsync(killer.GenerateStat()).ConfigureAwait(false);
            if (killer.MapInstance != null)
            {
                await killer.MapInstance.SendPacketAsync(killer.GenerateStatInfo()).ConfigureAwait(false);
            }
        }
    }
}
