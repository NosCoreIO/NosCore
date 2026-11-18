//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SoundFlowerHandler(IQuestService questService, List<QuestDto> quests, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.SoundFlower)
            {
                return;
            }

            var pool = quests.Where(q => q.QuestType == QuestType.FlowerQuest).ToList();
            if (pool.Count == 0)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            var quest = pool[random.Next(0, pool.Count)];
            if (await questService.AddQuestAsync(character, QuestActionType.Achieve, quest.QuestId))
            {
                await character.UseUpOneAsync(evt.InventoryItem);
            }
        }
    }
}
