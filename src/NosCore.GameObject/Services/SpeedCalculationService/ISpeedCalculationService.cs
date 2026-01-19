//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;

namespace NosCore.GameObject.Services.SpeedCalculationService
{
    public interface ISpeedCalculationService
    {
        byte CalculateSpeed(INonPlayableEntity nonPlayableEntity);

        byte CalculateSpeed(ICharacterEntity visualEntity);
    }
}
