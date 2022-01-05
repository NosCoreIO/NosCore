using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Algorithm.SpeedService;
using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Services.SpeedCalculationService
{
    public class SpeedCalculationService : ISpeedCalculationService
    {
        private readonly ISpeedService _speedService;

        public SpeedCalculationService(ISpeedService speedService)
        {
            _speedService = speedService;
        }

        private byte CalculateSpeed(IAliveEntity aliveEntity, byte defaultSpeed)
        {
            //    if (HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
            //    {
            //        return 0;
            //    }

            var bonusSpeed = 0; /*(byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovementNegated)[0];*/
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
            var defaultSpeed = _speedService.GetSpeed(characterEntity.Class);
            if (characterEntity.VehicleSpeed != null)
            {
                return (byte)characterEntity.VehicleSpeed;

            }

            return CalculateSpeed(characterEntity, defaultSpeed);
        }
    }
}
