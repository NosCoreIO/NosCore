//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Core.Concurrency;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Services.FamilyService
{
    // Runs on the master: members of one family earn from any channel, so the family row is
    // re-read and written under one lock.
    public class FamilyProgressionService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao, IFamilyExperienceService familyExperienceService,
        IDaoTransactionScope daoTransactionScope) : IFamilyProgressionService
    {
        private static readonly AsyncLock Gains = new();

        public async Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience)
        {
            using var _ = await Gains.AcquireAsync();
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);
            var family = membership?.FamilyId == familyId
                ? await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId)
                : null;
            if (family == null || experience <= 0)
            {
                return null;
            }

            var previousLevel = family.FamilyLevel;
            family.FamilyExperience += experience;
            membership!.Experience += experience;
            // The curve has no row past the last level, so the family stops there with a full bar.
            while (familyExperienceService.GetFamilyExperience((byte)(family.FamilyLevel + 1)) != uint.MaxValue
                && family.FamilyExperience >= familyExperienceService.GetFamilyExperience(family.FamilyLevel))
            {
                family.FamilyExperience -= (int)familyExperienceService.GetFamilyExperience(family.FamilyLevel);
                family.FamilyLevel++;
            }

            await using (var transaction = daoTransactionScope.Begin())
            {
                await familyDao.TryInsertOrUpdateAsync(family);
                await familyCharacterDao.TryInsertOrUpdateAsync(membership);
                await transaction.CommitAsync();
            }

            return new FamilyExperienceGain(family.FamilyLevel, family.FamilyLevel > previousLevel);
        }
    }
}
