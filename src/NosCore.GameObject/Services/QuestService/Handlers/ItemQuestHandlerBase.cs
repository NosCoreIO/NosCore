//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class ItemQuestHandlerBase : IItemQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    public Task<bool> ValidateAsync(ICharacterEntity character, CharacterQuest quest) =>
        Task.FromResult(quest.AreObjectivesComplete());

    public virtual Task<ItemQuestProgress> OnItemsReceivedAsync(ICharacterEntity character, CharacterQuest quest, short vNum) =>
        Task.FromResult(ItemQuestProgress.None);

    public virtual Task<ItemQuestProgress> OnItemProducedAsync(ICharacterEntity character, CharacterQuest quest, short vNum, short amount) =>
        OnItemsReceivedAsync(character, quest, vNum);

    public virtual Task<ItemQuestProgress> OnItemUsedAsync(ICharacterEntity character, CharacterQuest quest, short vNum) =>
        Task.FromResult(ItemQuestProgress.None);

    public virtual Task<ItemQuestProgress> OnItemWornAsync(ICharacterEntity character, CharacterQuest quest, short vNum) =>
        Task.FromResult(ItemQuestProgress.None);

    public virtual Task<ItemQuestProgress> OnNpcDialogRequestedAsync(ICharacterEntity character, CharacterQuest quest, short npcVNum) =>
        Task.FromResult(ItemQuestProgress.None);

    protected static int CountCarried(ICharacterEntity character, int vNum) =>
        character.InventoryService.Values
            .Where(s => s.Type != NoscorePocketType.Wear && s.ItemInstance.ItemVNum == vNum)
            .Sum(s => s.ItemInstance.Amount);

    protected static List<IPacket> TakeCarried(ICharacterEntity character, int vNum, int amount)
    {
        var packets = new List<IPacket>();
        foreach (var stack in character.InventoryService.Values
                     .Where(s => s.Type != NoscorePocketType.Wear && s.ItemInstance.ItemVNum == vNum)
                     .OrderBy(s => s.Slot)
                     .ToList())
        {
            if (amount <= 0)
            {
                break;
            }

            var taken = (short)Math.Min(amount, stack.ItemInstance.Amount);
            var left = character.InventoryService.RemoveItemAmountFromInventory(taken, stack.ItemInstanceId);
            amount -= taken;
            packets.Add(left.GeneratePocketChange(stack.Type.ToPocketType(), stack.Slot));
        }

        return packets;
    }

    protected static bool RaiseProgress(CharacterQuest quest, QuestObjectiveDto objective, int value)
    {
        var required = objective.RequiredAmount(quest.Quest.QuestType);
        var capped = required > 0 ? Math.Min(value, required) : value;
        var previous = quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var p) ? p : 0;
        if (capped <= previous)
        {
            return false;
        }

        quest.ObjectiveProgress[objective.QuestObjectiveId] = capped;
        return true;
    }

    protected static ItemQuestProgress Outcome(CharacterQuest quest, bool progressed) =>
        !progressed ? ItemQuestProgress.None
        : quest.AreObjectivesComplete() ? ItemQuestProgress.Completed
        : ItemQuestProgress.Progressed;
}
