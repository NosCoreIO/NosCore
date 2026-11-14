//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MonsterGoldService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quest;
using NosCore.Shared.Helpers;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.QuestService;

public class QuestRewardService(List<QuestRewardDto> questRewards, List<QuestQuestRewardDto> questQuestRewards,
        IItemCatalog itemCatalog, IItemGenerationService itemGenerationService,
        IExperienceProgressionService experienceProgression, IExperienceService experienceService,
        IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService,
        ISkillPointService skillPointService, IMonsterGoldService monsterGoldService, List<NpcMonsterDto> npcMonsters,
        ISessionRegistry sessionRegistry, IGameLanguageLocalizer localizer,
        IOptions<WorldConfiguration> worldConfiguration, ILogger<QuestRewardService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
    : IQuestRewardService
{
    private const int WindowSlots = 4;

    public bool HasRoomFor(PlayerComponentBundle character, short questId) =>
        RewardsOf(questId).Where(r => ItemCandidates(r).Count > 0)
            .All(r => character.InventoryService.CanAddItem(ItemCandidates(r)[0]));

    public async Task GiveAsync(PlayerComponentBundle character, CharacterQuest quest)
    {
        var questId = quest.QuestId;
        var slots = new List<(QuestRewardType Type, int Data, int Amount)>();
        foreach (var reward in RewardsOf(questId))
        {
            if (await GiveAsync(character, quest, reward) is { } slot)
            {
                slots.Add(slot);
            }
        }

        if (slots.Count == 0)
        {
            return;
        }

        var window = new QrPacket { QuestId = questId };
        Fill(window, slots);
        await character.SendPacketAsync(window);
    }

    private IEnumerable<QuestRewardDto> RewardsOf(short questId) =>
        questQuestRewards.Where(l => l.QuestId == questId)
            .Select(l => questRewards.FirstOrDefault(r => r.QuestRewardId == l.QuestRewardId))
            .OfType<QuestRewardDto>();

    private static List<short> ItemCandidates(QuestRewardDto reward) => reward.RewardType switch
    {
        QuestRewardType.EtcMainItem or QuestRewardType.WearItem or QuestRewardType.ClassItem =>
            new int?[] { reward.Data, reward.SecondData, reward.ThirdData, reward.FourthData }
                .OfType<int>().Where(v => v > 0).Select(v => (short)v).ToList(),
        QuestRewardType.ConsecutiveItem => [(short)reward.Data],
        _ => [],
    };

    private async Task<(QuestRewardType, int, int)?> GiveAsync(PlayerComponentBundle character, CharacterQuest quest, QuestRewardDto reward)
    {
        switch (reward.RewardType)
        {
            case QuestRewardType.GoldPerMonster:
                // A quest without a hunted monster values its "monster" at the character's level.
                var monsterLevel = HuntedMonsters(quest).FirstOrDefault().Monster?.Level ?? character.Level;
                return await GiveGoldAsync(character, reward.RewardType, RollGold(monsterLevel, reward.Amount));

            case QuestRewardType.GoldPerKill:
                var killGold = HuntedMonsters(quest).Sum(h => RollGold(h.Monster.Level, h.Kills));
                return await GiveGoldAsync(character, reward.RewardType, Percent(killGold, reward.Amount));

            case QuestRewardType.ExperiencePerMonster:
                if (HuntedMonsters(quest).FirstOrDefault().Monster is not { } hunted)
                {
                    logger.LogWarning(logLanguage[LogLanguageKey.QUEST_EXPERIENCE_WITHOUT_MONSTER], quest.QuestId);
                    return null;
                }
                await experienceProgression.AddExperienceAsync(character, (long)hunted.Xp * reward.Amount, 0, 0, 0, 0);
                return (reward.RewardType, 0, reward.Amount);

            case QuestRewardType.ExperiencePerKill:
                await experienceProgression.AddExperienceAsync(character,
                    Percent(HuntedMonsters(quest).Sum(h => (long)h.Monster.Xp * h.Kills), reward.Amount), 0, 0, 0, 0);
                return (reward.RewardType, 0, reward.Amount);

            case QuestRewardType.JobExperiencePerKill:
                await experienceProgression.AddExperienceAsync(character, 0,
                    Percent(HuntedMonsters(quest).Sum(h => (long)h.Monster.JobXp * h.Kills), reward.Amount), 0, 0, 0);
                return (reward.RewardType, 0, reward.Amount);

            case QuestRewardType.Gold:
                return await GiveGoldAsync(character, reward.RewardType, reward.Amount);

            case QuestRewardType.Reputation:
                await character.SetReputationAsync(character.Reputation + reward.Amount);
                return (reward.RewardType, 0, reward.Amount);

            case QuestRewardType.Experience:
                var level = reward.Data < 0 ? character.Level : reward.Data;
                await experienceProgression.AddExperienceAsync(character,
                    Percent(experienceService.GetExperience((byte)level), reward.Amount), 0, 0, 0, 0);
                return (reward.RewardType, reward.Data, reward.Amount);

            case QuestRewardType.JobExperience:
                var jobXp = reward.Data == 0
                    ? reward.Amount
                    : Percent(jobExperienceService.GetJobExperience(character.Class,
                        (byte)(reward.Data < 0 ? character.JobLevel : reward.Data)), reward.Amount);
                await experienceProgression.AddExperienceAsync(character, 0, jobXp, 0, 0, 0);
                return (reward.RewardType, reward.Data, reward.Amount);

            case QuestRewardType.HeroExperience:
                var heroLevel = reward.Data < 0 ? character.HeroLevel : reward.Data;
                if (heroLevel <= 0)
                {
                    return null;
                }
                await experienceProgression.AddExperienceAsync(character, 0, 0,
                    Percent(heroExperienceService.GetHeroExperience((byte)heroLevel), reward.Amount), 0, 0);
                return (reward.RewardType, reward.Data, reward.Amount);

            case QuestRewardType.Level:
            case QuestRewardType.LevelAndJobLevel:
                if (character.Level < reward.Data)
                {
                    await character.SetLevelAsync((byte)reward.Data, experienceService, jobExperienceService,
                        heroExperienceService, skillPointService, sessionRegistry, localizer);
                }
                if (reward.RewardType == QuestRewardType.LevelAndJobLevel && reward.SecondData is { } jobLevel && character.JobLevel < jobLevel)
                {
                    await character.SetJobLevelAsync((byte)jobLevel, experienceService,
                        jobExperienceService, heroExperienceService, skillPointService);
                }
                return (reward.RewardType, reward.Data, reward.SecondData ?? 0);

            case QuestRewardType.EtcMainItem:
            case QuestRewardType.WearItem:
            case QuestRewardType.ClassItem:
            case QuestRewardType.ConsecutiveItem:
                return await GiveItemAsync(character, reward);

            case QuestRewardType.NotRepeatable:
                return null;

            default:
                logger.LogWarning(logLanguage[LogLanguageKey.QUEST_REWARD_TYPE_UNSUPPORTED], reward.QuestRewardId, reward.RewardType);
                return null;
        }
    }

    private async Task<(QuestRewardType, int, int)?> GiveItemAsync(PlayerComponentBundle character, QuestRewardDto reward)
    {
        var candidates = ItemCandidates(reward);
        if (reward.RewardType == QuestRewardType.ClassItem)
        {
            candidates = candidates.Where(vnum => itemCatalog.GetItem(vnum) is { } item
                && (item.Class == 0 || ((item.Class >> (byte)character.Class) & 1) == 1)).ToList();
        }

        if (candidates.Count == 0)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.QUEST_REWARD_NO_CLASS_ITEM], reward.QuestRewardId, character.Class);
            return null;
        }

        var vnum = candidates[RandomHelper.Instance.RandomNumber(0, candidates.Count)];
        var amount = (short)Math.Max(1, reward.Amount);
        var created = itemGenerationService.Create(vnum, amount);
        var added = character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(created, character.VisualId));
        if (added == null || added.Count == 0)
        {
            logger.LogWarning(logLanguage[LogLanguageKey.QUEST_REWARD_LOST_INVENTORY_FULL],
                vnum, amount, character.CharacterId);
            return null;
        }

        foreach (var inv in added)
        {
            await character.SendPacketAsync(inv.GeneratePocketChange(inv.Type.ToPocketType(), inv.Slot));
        }

        return (reward.RewardType, vnum, amount);
    }

    private async Task<(QuestRewardType, int, int)> GiveGoldAsync(PlayerComponentBundle character, QuestRewardType type, long amount)
    {
        var gold = Math.Min(amount, Math.Max(0, worldConfiguration.Value.MaxGoldAmount - character.Gold));
        await character.AddGoldAsync(gold, localizer);
        return (type, 0, (int)gold);
    }

    private long RollGold(byte monsterLevel, int monsters)
    {
        var (min, max) = monsterGoldService.GetGoldDrop(monsterLevel);
        return Enumerable.Range(0, monsters).Sum(_ => (long)RandomHelper.Instance.RandomNumber(min, max + 1));
    }

    private List<(NpcMonsterDto Monster, int Kills)> HuntedMonsters(CharacterQuest quest)
    {
        if (quest.Quest.QuestType is not (QuestType.Hunt or QuestType.NumberOfKill))
        {
            return [];
        }

        var hunted = new List<(NpcMonsterDto Monster, int Kills)>();
        foreach (var objective in quest.Quest.QuestObjectives)
        {
            if (npcMonsters.FirstOrDefault(m => m.NpcMonsterVNum == objective.FirstData) is { } monster)
            {
                hunted.Add((monster, quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var kills) ? kills : 0));
            }
        }

        return hunted;
    }

    private static long Percent(long total, int percent) => total * percent / 100;

    private static void Fill(QrPacket window, List<(QuestRewardType Type, int Data, int Amount)> slots)
    {
        for (var i = 0; i < Math.Min(slots.Count, WindowSlots); i++)
        {
            var (type, data, amount) = slots[i];
            switch (i)
            {
                case 0: (window.Reward1Type, window.Reward1Data, window.Reward1Amount) = (type, data, amount); break;
                case 1: (window.Reward2Type, window.Reward2Data, window.Reward2Amount) = (type, data, amount); break;
                case 2: (window.Reward3Type, window.Reward3Data, window.Reward3Amount) = (type, data, amount); break;
                default: (window.Reward4Type, window.Reward4Data, window.Reward4Amount) = (type, data, amount); break;
            }
        }
    }
}
