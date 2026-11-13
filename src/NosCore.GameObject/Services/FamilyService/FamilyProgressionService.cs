//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    // Runs on the master: members of one family earn from any channel, so the family row is
    // re-read and written under one lock.
    public class FamilyProgressionService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao, IDao<FamilyQuestProgressDto, long> familyQuestProgressDao,
        IDao<FamilyQuestContributionDto, long> familyQuestContributionDao,
        List<FamilyQuestDto> familyQuests, IFamilyExperienceService familyExperienceService,
        IDaoTransactionScope daoTransactionScope, IClock clock) : IFamilyProgressionService
    {
        public async Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);
            var family = membership?.FamilyId == familyId
                ? await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId)
                : null;
            if (membership == null || family == null || experience <= 0)
            {
                return null;
            }

            membership.Experience += experience;
            var progress = new Progress(family, LoadQuests(familyId), [], null, clock.GetCurrentInstant());
            Gain(progress, experience);
            return await SaveAsync(progress, membership);
        }

        public async Task<FamilyExperienceGain?> AdvanceQuestsAsync(long familyId, long? characterId,
            FamilyQuestObjective objective, int amount)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var family = await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId);
            if (family == null || amount <= 0)
            {
                return null;
            }

            var contributions = characterId is { } id
                ? (familyQuestContributionDao.Where(s => s.FamilyId == familyId && s.CharacterId == id) ?? []).ToDictionary(s => s.QuestId)
                : [];
            var progress = new Progress(family, LoadQuests(familyId), contributions, characterId, clock.GetCurrentInstant());
            Gain(progress, Advance(progress, objective, amount));
            return await SaveAsync(progress, null);
        }

        private Dictionary<short, FamilyQuestProgressDto> LoadQuests(long familyId) =>
            (familyQuestProgressDao.Where(s => s.FamilyId == familyId) ?? []).ToDictionary(s => s.QuestId);

        private void Gain(Progress progress, int experience)
        {
            var family = progress.Family;
            while (experience > 0)
            {
                var level = family.FamilyLevel;
                family.FamilyExperience += experience;
                // The curve has no row past the last level, so the family stops there with a full bar.
                while (familyExperienceService.GetFamilyExperience((byte)(family.FamilyLevel + 1)) != uint.MaxValue
                    && family.FamilyExperience >= familyExperienceService.GetFamilyExperience(family.FamilyLevel))
                {
                    family.FamilyExperience -= (int)familyExperienceService.GetFamilyExperience(family.FamilyLevel);
                    family.FamilyLevel++;
                }

                experience = family.FamilyLevel > level ? Advance(progress, FamilyQuestObjective.FamilyLevel, 0) : 0;
            }
        }

        // Quests are walked in id order, which is also the order of each chain, so an achievement
        // unlocked here opens the next one in the same pass. A counted chain carries its count on:
        // the next achievement starts where the previous one stopped.
        private int Advance(Progress progress, FamilyQuestObjective objective, int amount)
        {
            var experience = 0;
            foreach (var quest in familyQuests.Where(s => s.Objective == objective).OrderBy(s => s.QuestId))
            {
                progress.Quests.TryGetValue(quest.QuestId, out var row);
                if (row != null && FamilyQuestPeriod.HasExpired(quest, row, progress.Now))
                {
                    row = FamilyQuestPeriod.Current(quest, row, progress.Now);
                    progress.Quests[quest.QuestId] = row;
                }

                var previous = quest.PreviousQuestId is { } previousId ? progress.Quests.GetValueOrDefault(previousId) : null;
                if (row?.CompletedAt != null || progress.Family.FamilyLevel < quest.FamilyLevelMinimum
                    || (quest.PreviousQuestId != null && previous?.CompletedAt == null))
                {
                    continue;
                }

                progress.Contributions.TryGetValue(quest.QuestId, out var contribution);
                if (quest.OncePerMemberPerDay && (progress.CharacterId == null
                    || contribution?.ContributedAt.InUtc().Date == progress.Now.InUtc().Date))
                {
                    continue;
                }

                var count = objective switch
                {
                    FamilyQuestObjective.FamilyLevel => progress.Family.FamilyLevel >= quest.Parameter ? quest.Target : 0,
                    _ => (row?.Progress ?? previous?.Progress ?? 0)
                        + (previous != null && progress.Completed.Contains(previous.QuestId) ? 0 : amount)
                };
                if (count <= (row?.Progress ?? 0))
                {
                    continue;
                }

                row ??= new FamilyQuestProgressDto { FamilyId = progress.Family.FamilyId, QuestId = quest.QuestId };
                row.Progress = Math.Min(count, quest.Target);
                row.ProgressedAt = progress.Now;
                if (quest.OncePerMemberPerDay && progress.CharacterId is { } characterId)
                {
                    contribution ??= new FamilyQuestContributionDto
                    {
                        FamilyId = progress.Family.FamilyId, QuestId = quest.QuestId, CharacterId = characterId
                    };
                    contribution.ContributedAt = progress.Now;
                    progress.Contributions[quest.QuestId] = contribution;
                }

                progress.Quests[quest.QuestId] = row;
                progress.Changed.Add(row);
                if (count < quest.Target)
                {
                    continue;
                }

                row.CompletedAt = progress.Now;
                row.TotalCompletions++;
                progress.Completed.Add(quest.QuestId);
                experience += quest.Experience;
            }

            return experience;
        }

        private async Task<FamilyExperienceGain?> SaveAsync(Progress progress, FamilyCharacterDto? membership)
        {
            await using (var transaction = daoTransactionScope.Begin())
            {
                if (await familyDao.TryInsertOrUpdateAsync(progress.Family) == null
                    || (membership != null && await familyCharacterDao.TryInsertOrUpdateAsync(membership) == null))
                {
                    return null;
                }

                foreach (var row in progress.Changed.Distinct())
                {
                    if (await familyQuestProgressDao.TryInsertOrUpdateAsync(row) == null)
                    {
                        return null;
                    }
                }

                foreach (var contribution in progress.Contributions.Values.Where(s => s.ContributedAt == progress.Now))
                {
                    if (await familyQuestContributionDao.TryInsertOrUpdateAsync(contribution) == null)
                    {
                        return null;
                    }
                }

                await transaction.CommitAsync();
            }

            return new FamilyExperienceGain(progress.Family.FamilyLevel, progress.Family.FamilyLevel > progress.PreviousLevel)
            {
                CompletedQuestIds = progress.Completed
            };
        }

        private sealed class Progress(FamilyDto family, Dictionary<short, FamilyQuestProgressDto> quests,
            Dictionary<short, FamilyQuestContributionDto> contributions, long? characterId, Instant now)
        {
            public FamilyDto Family { get; } = family;
            public byte PreviousLevel { get; } = family.FamilyLevel;
            public Dictionary<short, FamilyQuestProgressDto> Quests { get; } = quests;
            public Dictionary<short, FamilyQuestContributionDto> Contributions { get; } = contributions;
            public long? CharacterId { get; } = characterId;
            public Instant Now { get; } = now;
            public List<FamilyQuestProgressDto> Changed { get; } = [];
            public List<short> Completed { get; } = [];
        }
    }
}
