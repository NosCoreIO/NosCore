//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    // Runs on the master: members of one family can buy from any channel, so the family row is
    // re-read, checked and written under one lock.
    public class FamilyExtensionService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao, IDao<FamilyUpgradeDto, short> familyUpgradeDao,
        IFamilyUpgradeService capacities) : IFamilyExtensionService
    {

        public async Task<FamilyExtensionPurchase> PurchaseAsync(long familyId, long characterId, short itemVNum)
        {
            var upgrade = await familyUpgradeDao.FirstOrDefaultAsync(s => s.ItemVNum == itemVNum);
            if (upgrade == null || !Enum.IsDefined(upgrade.UpgradeType))
            {
                return FamilyExtensionPurchase.NotSold;
            }

            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);
            var family = membership?.FamilyId == familyId
                ? await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId)
                : null;
            if (membership == null || family == null)
            {
                return FamilyExtensionPurchase.NotInFamily;
            }

            if (membership.Authority != FamilyAuthority.Head)
            {
                return FamilyExtensionPurchase.NotHead;
            }

            if (family.IsOwned(upgrade, capacities))
            {
                return FamilyExtensionPurchase.AlreadyPurchased;
            }

            if (family.FamilyLevel < upgrade.FamilyLevelMinimum)
            {
                return FamilyExtensionPurchase.FamilyLevelInsufficient;
            }

            if (!family.HasPrevious(upgrade, capacities))
            {
                return FamilyExtensionPurchase.PreviousNotPurchased;
            }

            family.Apply(upgrade, capacities);
            return await familyDao.TryInsertOrUpdateAsync(family) != null
                ? FamilyExtensionPurchase.Purchased
                : FamilyExtensionPurchase.NotSaved;
        }
    }
}
