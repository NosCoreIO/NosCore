//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Packets.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyRankingService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao,
        IDao<CharacterDto, long> characterDao) : IFamilyRankingService
    {
        private const int Shown = 100;

        public IReadOnlyList<FamilyRankingEntry> GetRanking(FamilyRankingType type)
        {
            var families = familyDao.LoadAll().ToList();
            return type switch
            {
                FamilyRankingType.Experience or FamilyRankingType.DailyExperience =>
                    Rank(families, s => (s.FamilyLevel, s.FamilyExperience), s => s.FamilyExperience),
                FamilyRankingType.DailyReputation => RankByReputation(families),
                _ => new List<FamilyRankingEntry>()
            };
        }

        private IReadOnlyList<FamilyRankingEntry> RankByReputation(IReadOnlyCollection<FamilyDto> families)
        {
            var familyIds = families.Select(s => s.FamilyId).ToHashSet();
            var memberships = familyCharacterDao.Where(s => familyIds.Contains(s.FamilyId))?.ToList()
                ?? new List<FamilyCharacterDto>();
            var characterIds = memberships.Select(s => s.CharacterId).ToHashSet();
            var reputations = characterDao.Where(s => characterIds.Contains(s.CharacterId))?
                .ToDictionary(s => s.CharacterId, s => s.Reput) ?? new Dictionary<long, long>();
            var byFamily = memberships
                .GroupBy(s => s.FamilyId)
                .ToDictionary(g => g.Key, g => g.Sum(m => reputations.GetValueOrDefault(m.CharacterId)));
            return Rank(families, s => (byFamily.GetValueOrDefault(s.FamilyId), 0), s => byFamily.GetValueOrDefault(s.FamilyId));
        }

        private static IReadOnlyList<FamilyRankingEntry> Rank(IEnumerable<FamilyDto> families,
            Func<FamilyDto, (long Primary, long Secondary)> order, Func<FamilyDto, long> shown)
        {
            var ordered = families.Select(s => (Family: s, Key: order(s)))
                .OrderByDescending(s => s.Key.Primary)
                .ThenByDescending(s => s.Key.Secondary)
                .ThenBy(s => s.Family.Name, StringComparer.Ordinal)
                .Take(Shown)
                .ToList();
            var entries = new List<FamilyRankingEntry>(ordered.Count);
            for (var i = 0; i < ordered.Count; i++)
            {
                var rank = i > 0 && ordered[i].Key == ordered[i - 1].Key ? entries[i - 1].Rank : i + 1;
                entries.Add(new FamilyRankingEntry(rank, ordered[i].Family.Name, ordered[i].Family.FamilyLevel, shown(ordered[i].Family)));
            }

            return entries;
        }
    }
}
