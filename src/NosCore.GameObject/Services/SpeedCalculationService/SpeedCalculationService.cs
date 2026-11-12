//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
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
        private byte CalculateSpeed(IAliveEntity aliveEntity, byte defaultSpeed)
        {
            //    if (HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
            //    {
            //        return 0;
            //    }

            // The movement BCards are still not read. The placeholder that used to sit
            // here reached for subtype 32, which the files say is "movement speed is
            // DECREASED by %s while you are hidden": wrong slot and wrong condition for a
            // general speed bonus. The unconditional flat pair is 41-42, and 21-22 is the
            // percentage.
            var bonusSpeed = 0;
            if (defaultSpeed + bonusSpeed > 59)
            {
                return 59;
            }

            return (byte)(defaultSpeed + bonusSpeed);
        }


        public byte CalculateSpeed(INonPlayableEntity nonPlayableEntity)
        {
            return CalculateSpeed(nonPlayableEntity, nonPlayableEntity.NpcMonster.Speed);
        }

        public byte CalculateSpeed(PlayerComponentBundle characterEntity)
        {
            if (characterEntity.IsVehicled)
            {
                var boost = buffService.GetActiveBuffs(characterEntity)
                        .Any(b => cardCatalog.IsGrantedBy(b.CardId, ItemEffectType.SpeedBooster))
                    ? items.Find(i => i.VNum == characterEntity.VehicleVNum)?.SpeedBoost ?? 0
                    : 0;
                return (byte)(characterEntity.VehicleSpeed + boost);
            }

            return CalculateSpeed(characterEntity, speedService.GetSpeed(characterEntity.Class));
        }
    }
}
