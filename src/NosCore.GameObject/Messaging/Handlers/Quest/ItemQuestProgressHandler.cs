//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.QuestService;

namespace NosCore.GameObject.Messaging.Handlers.Quest
{
    [UsedImplicitly]
    public sealed class ItemQuestProgressHandler(
        IQuestService questService,
        IEnumerable<IQuestTypeHandler> questTypeHandlers,
        ILogger<ItemQuestProgressHandler> logger)
    {
        [UsedImplicitly]
        public Task Handle(ItemsReceivedEvent evt) =>
            AdvanceAsync(evt.Character, (h, q) => h.OnItemsReceivedAsync(evt.Character, q, evt.VNum));

        [UsedImplicitly]
        public Task Handle(ItemProducedEvent evt) =>
            AdvanceAsync(evt.Character, (h, q) => h.OnItemProducedAsync(evt.Character, q, evt.VNum, evt.Amount));

        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            ICharacterEntity character = evt.ClientSession.Character;
            var vNum = evt.InventoryItem.ItemInstance.ItemVNum;
            return AdvanceAsync(character, (h, q) => h.OnItemUsedAsync(character, q, vNum));
        }

        [UsedImplicitly]
        public Task Handle(ItemWornEvent evt) =>
            AdvanceAsync(evt.Character, (h, q) => h.OnItemWornAsync(evt.Character, q, evt.VNum));

        [UsedImplicitly]
        public Task Handle(NpcTalkedEvent evt) =>
            evt.Npc.NpcMonster is { } npc
                ? AdvanceAsync(evt.Character, (h, q) => h.OnNpcDialogRequestedAsync(evt.Character, q, npc.NpcMonsterVNum))
                : Task.CompletedTask;

        private async Task AdvanceAsync(ICharacterEntity character,
            Func<IItemQuestTypeHandler, CharacterQuest, Task<ItemQuestProgress>> step)
        {
            foreach (var quest in character.Quests.Values.Where(q => q.ObjectivesCompletedOn is null && q.CompletedOn is null).ToList())
            {
                var handler = questTypeHandlers.OfType<IItemQuestTypeHandler>().FirstOrDefault(h => h.QuestType == quest.Quest.QuestType);
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    switch (await step(handler, quest))
                    {
                        case ItemQuestProgress.Progressed:
                            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
                            break;
                        case ItemQuestProgress.Completed:
                            await questService.CompleteQuestAsync(character, quest);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to progress quest {QuestId} for character {CharacterId}",
                        quest.QuestId, character.CharacterId);
                }
            }
        }
    }
}
