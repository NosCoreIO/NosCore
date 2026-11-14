//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IRecoveryBonusService
{
    // The HP or MP a recovery item restores, raised by the user's gear and buffs.
    int Boost(IAliveEntity user, int amount);
}
