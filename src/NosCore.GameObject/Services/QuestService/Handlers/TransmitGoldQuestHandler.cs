//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Core.I18N;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the NPC to pay, SecondData: the amount in units of 10 000 gold.
[UsedImplicitly]
public sealed class TransmitGoldQuestHandler(IGameLanguageLocalizer gameLanguageLocalizer) : IQuestTypeHandler
{
    public const long GoldUnit = 10_000;

    public QuestType QuestType => QuestType.TransmitGold;

    public async Task<bool> OnNpcTalkedAsync(ICharacterEntity character, NpcMonsterDto npc, CharacterQuest quest)
    {
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == npc.NpcMonsterVNum))
        {
            var units = objective.SecondData ?? 0;
            if (quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId) >= units)
            {
                continue;
            }

            var gold = units * GoldUnit;
            if (character.Gold < gold)
            {
                await character.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughGold));
                return false;
            }

            if (!quest.ObjectiveProgress.TryAdd(objective.QuestObjectiveId, units))
            {
                continue;
            }

            await character.RemoveGoldAsync(gold, gameLanguageLocalizer);
        }

        return quest.AreObjectivesComplete();
    }
}
