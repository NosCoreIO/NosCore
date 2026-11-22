//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// Counts each item as it is picked up, since most of these are map items that never reach the bag. An item a
// party member picks up on the same map counts for every member collecting it.
[UsedImplicitly]
public sealed class CollectInRaidQuestHandler : ItemQuestHandlerBase
{
    public override QuestType QuestType => QuestType.CollectInRaid;

    public override async Task<ItemQuestProgress> OnItemsReceivedAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum)
    {
        var progressed = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.ItemVNum(QuestType) == vNum))
        {
            var current = quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId) + 1;
            if (!RaiseProgress(quest, objective, current))
            {
                continue;
            }

            progressed = true;
            await character.SendPacketAsync(character.GenerateSayi2(SayColorType.Red, Game18NConstString.Collected, 7,
                vNum, $"{current}/{objective.RequiredAmount(QuestType)}"));
        }

        return Outcome(quest, progressed);
    }

    public override Task<ItemQuestProgress> OnPartyItemPickedUpAsync(PlayerComponentBundle character, CharacterQuest quest, short vNum) =>
        OnItemsReceivedAsync(character, quest, vNum);
}
