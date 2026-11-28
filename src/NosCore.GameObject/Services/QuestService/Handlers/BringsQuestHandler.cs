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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// The NPC takes whatever part of the items the bag holds, so a delivery can be split over several talks.
[UsedImplicitly]
public sealed class BringsQuestHandler : HandInQuestHandlerBase
{
    public override QuestType QuestType => QuestType.Brings;

    public override async Task<ItemQuestProgress> OnNpcDialogRequestedAsync(PlayerComponentBundle character, CharacterQuest quest, short npcVNum)
    {
        var delivered = new List<QuestObjectiveDto>();
        var pocketChanges = new List<IPacket>();
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.NpcVNum(QuestType) == npcVNum && o.ItemVNum(QuestType) != null))
        {
            var vNum = objective.ItemVNum(QuestType).GetValueOrDefault();
            var current = quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId);
            var given = Math.Min(objective.RequiredAmount(QuestType) - current, character.InventoryService.CountItem(vNum));
            if (given <= 0)
            {
                continue;
            }

            pocketChanges.AddRange(character.InventoryService.RemoveItemAmountByVNum(vNum, given));
            quest.ObjectiveProgress[objective.QuestObjectiveId] = current + given;
            delivered.Add(objective);
        }

        if (delivered.Count == 0)
        {
            return ItemQuestProgress.None;
        }

        await character.SendPacketsAsync(pocketChanges);
        foreach (var objective in delivered)
        {
            await character.SendPacketAsync(ProgressLine(character, Game18NConstString.Delivered, quest, objective));
        }

        return Outcome(quest, true);
    }
}
