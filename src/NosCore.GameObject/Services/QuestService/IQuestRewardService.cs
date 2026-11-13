//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.QuestService;

public interface IQuestRewardService
{
    bool HasRoomFor(PlayerComponentBundle character, short questId);

    // Grants every prize of the quest and opens the reward window.
    Task GiveAsync(PlayerComponentBundle character, short questId);
}
