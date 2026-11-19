//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService;

public sealed class SoundFlowerService(IQuestService questService, List<QuestDto> quests, IRandomProvider random) : ISoundFlowerService
{
    public async Task<bool> StartFlowerQuestAsync(PlayerComponentBundle character)
    {
        var pool = quests.Where(q => q.QuestType == QuestType.FlowerQuest).ToList();
        if (pool.Count == 0)
        {
            return false;
        }

        var quest = pool[random.Next(0, pool.Count)];
        return await questService.AddQuestAsync(character, QuestActionType.Achieve, quest.QuestId);
    }
}
