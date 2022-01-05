using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.SpeedCalculationService
{
    public interface ISpeedCalculationService
    {
        byte CalculateSpeed(INonPlayableEntity nonPlayableEntity);

        byte CalculateSpeed(ICharacterEntity visualEntity);
    }
}
