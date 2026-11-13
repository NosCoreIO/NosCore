//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using System.Collections.Generic;
using NosCore.Algorithm.SpeedService;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.SpeedCalculationService
{
    public class SpeedCalculationService(ISpeedService speedService, IBuffService buffService, ICardCatalog cardCatalog,
        List<ItemDto> items) : ISpeedCalculationService
    {
        private const int MaxSpeedOnFoot = 59;

        private byte CalculateSpeed(IAliveEntity aliveEntity, int baseSpeed, int maxSpeed)
        {
            if (buffService.HasEffect(aliveEntity, BCardEffect.MoveMovementImpossible))
            {
                return 0;
            }

            var bonus = 0;
            foreach (var buff in buffService.GetActiveBuffs(aliveEntity))
            {
                foreach (var bCard in buff.BCards)
                {
                    bonus += bCard.Effect() switch
                    {
                        BCardEffect.MoveMovementSpeedIncreased => bCard.FirstData,
                        BCardEffect.MoveMovementSpeedDecreased => -bCard.FirstData,
                        _ => 0,
                    };
                }
            }

            return (byte)Math.Clamp(baseSpeed + bonus, 0, maxSpeed);
        }

        public byte CalculateSpeed(INonPlayableEntity nonPlayableEntity)
        {
            return CalculateSpeed(nonPlayableEntity, nonPlayableEntity.NpcMonster.Speed, MaxSpeedOnFoot);
        }

        public byte CalculateSpeed(PlayerComponentBundle characterEntity)
        {
            if (characterEntity.IsVehicled)
            {
                var boost = buffService.GetActiveBuffs(characterEntity)
                        .Any(b => cardCatalog.IsGrantedBy(b.CardId, ItemEffectType.SpeedBooster))
                    ? items.Find(i => i.VNum == characterEntity.VehicleVNum)?.SpeedBoost ?? 0
                    : 0;
                return CalculateSpeed(characterEntity, characterEntity.VehicleSpeed + boost, byte.MaxValue);
            }

            return CalculateSpeed(characterEntity, speedService.GetSpeed(characterEntity.Class), MaxSpeedOnFoot);
        }
    }
}
