//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.QuestService;

public interface ISoundFlowerService
{
    // False when the character cannot take the quest; the flower is then left where it was.
    Task<bool> StartFlowerQuestAsync(PlayerComponentBundle character);
}
