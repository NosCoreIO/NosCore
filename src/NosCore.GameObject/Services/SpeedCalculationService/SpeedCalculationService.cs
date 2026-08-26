//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.SpeedService;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.SpeedCalculationService
{
    public class SpeedCalculationService(ISpeedService speedService) : ISpeedCalculationService
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

        public byte CalculateSpeed(ICharacterEntity characterEntity)
        {
            // IsVehicled and not "VehicleSpeed is not null": the component declares that field as
            // a plain byte and the interface widens it to byte?, so the null branch could never
            // be taken - the service answered VehicleSpeed always, which is 0 on foot. Nothing
            // reported it because nothing called the service at all.
            if (characterEntity.IsVehicled)
            {
                return characterEntity.VehicleSpeed ?? 0;
            }

            return CalculateSpeed(characterEntity, speedService.GetSpeed(characterEntity.Class));
        }
    }
}
