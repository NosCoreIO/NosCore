//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattleService;

public interface IStaticBuffService
{
    // Puts back the timed static buffs the character left the world with, each with the time it had left.
    Task RestoreAsync(ICharacterEntity character);

    // Stores the seconds left on each timed static buff. The time only runs while the character is in the world.
    Task<bool> SaveAsync(ICharacterEntity character);
}
