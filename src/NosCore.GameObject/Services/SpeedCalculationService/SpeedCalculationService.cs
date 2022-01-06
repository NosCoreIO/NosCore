//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
