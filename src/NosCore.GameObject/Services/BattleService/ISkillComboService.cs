//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

public interface ISkillComboService
{
    // Counts this use of the skill and returns the combo row its streak has reached, if any.
    ComboDto? Advance(PlayerComponentBundle player, short skillVnum);
}
