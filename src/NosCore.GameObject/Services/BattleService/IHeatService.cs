//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IHeatService
{
    // Applies the card of the map type the character stands in unless heat resistance is
    // active, and removes it once they leave.
    Task RefreshAsync(ICharacterEntity character);
}
