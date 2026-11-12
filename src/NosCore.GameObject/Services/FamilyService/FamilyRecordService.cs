//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Services.WarehouseService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    // Runs on the master. Channels and the master's own features change the same family rows, and a
    // lock held in one process does not stop another, so every write to them is made here.
    public class FamilyRecordService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao, IDao<FamilyLogDto, long> familyLogDao,
        IDaoTransactionScope daoTransactionScope, IWarehouseService warehouseService) : IFamilyRecordService
    {
        public async Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var founderIds = deputyIds.Prepend(headId).ToList();
            if (await familyDao.FirstOrDefaultAsync(s => s.Name == name) != null
                || familyCharacterDao.Where(s => founderIds.Contains(s.CharacterId))?.Any() == true)
            {
                return null;
            }

            await using var transaction = daoTransactionScope.Begin();
            var family = await familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = name,
                FamilyLevel = 1,
                MaxSize = maxSize,
                FamilyMessage = string.Empty
            });
            if (family == null)
            {
                return null;
            }

            var members = founderIds.Select(id => new FamilyCharacterDto
            {
                FamilyId = family.FamilyId,
                CharacterId = id,
                Authority = id == headId ? FamilyAuthority.Head : FamilyAuthority.Assistant,
                DailyMessage = string.Empty
            });
            if (!await familyCharacterDao.TryInsertOrUpdateAsync(members))
            {
                return null;
            }

            await transaction.CommitAsync();
            return family.FamilyId;
        }

        public async Task<bool> DisbandAsync(long familyId, long headId)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var head = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == headId && s.FamilyId == familyId);
            if (head?.Authority != FamilyAuthority.Head)
            {
                return false;
            }

            foreach (var stored in await warehouseService.GetItemsAsync(null, familyId, WarehouseType.FamilyWareHouse, null))
            {
                await warehouseService.WithdrawItemAsync(stored.WarehouseItemId);
            }

            var logs = familyLogDao.Where(s => s.FamilyId == familyId)?.Select(s => s.FamilyLogId).ToList()
                ?? new List<long>();
            var members = familyCharacterDao.Where(s => s.FamilyId == familyId)?.Select(s => s.FamilyCharacterId).ToList()
                ?? new List<long>();
            await using var transaction = daoTransactionScope.Begin();
            if ((logs.Count != 0 && await familyLogDao.TryDeleteAsync(logs) == null)
                || (members.Count != 0 && await familyCharacterDao.TryDeleteAsync(members) == null)
                || await familyDao.TryDeleteAsync(familyId) == null)
            {
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }

        public async Task<FamilyMemberAddition> AddMemberAsync(long familyId, long characterId)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var family = await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId);
            if (family == null || await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId) != null)
            {
                return FamilyMemberAddition.NotAdded;
            }

            if ((familyCharacterDao.Where(s => s.FamilyId == familyId)?.Count() ?? 0) >= family.MaxSize)
            {
                return FamilyMemberAddition.FamilyFull;
            }

            return await familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = familyId,
                CharacterId = characterId,
                Authority = FamilyAuthority.Member,
                DailyMessage = string.Empty
            }) != null
                ? FamilyMemberAddition.Added
                : FamilyMemberAddition.NotAdded;
        }

        public async Task<bool> RemoveMemberAsync(long familyId, long characterId)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId && s.FamilyId == familyId);
            return membership != null && await familyCharacterDao.TryDeleteAsync(membership.FamilyCharacterId) != null;
        }

        public async Task<bool> ChangeAsync(long familyId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            FamilyDto? stored = null;
            if (family != null)
            {
                stored = await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId);
                if (stored == null)
                {
                    return false;
                }

                family.ApplyTo(stored);
            }

            var memberships = new List<FamilyCharacterDto>();
            foreach (var change in members)
            {
                var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == change.CharacterId && s.FamilyId == familyId);
                if (membership == null)
                {
                    return false;
                }

                change.ApplyTo(membership);
                memberships.Add(membership);
            }

            // A channel decides from its own copy of the family, which can be behind: whatever it asks,
            // the family keeps exactly one head.
            if (members.Any(s => s.Authority != null))
            {
                var changed = memberships.Select(s => s.FamilyCharacterId).ToHashSet();
                var heads = memberships.Count(s => s.Authority == FamilyAuthority.Head)
                    + (familyCharacterDao.Where(s => s.FamilyId == familyId && s.Authority == FamilyAuthority.Head)
                        ?.Count(s => !changed.Contains(s.FamilyCharacterId)) ?? 0);
                if (heads != 1)
                {
                    return false;
                }
            }

            await using var transaction = daoTransactionScope.Begin();
            if (stored != null && await familyDao.TryInsertOrUpdateAsync(stored) == null)
            {
                return false;
            }

            foreach (var membership in memberships)
            {
                if (await familyCharacterDao.TryInsertOrUpdateAsync(membership) == null)
                {
                    return false;
                }
            }

            await transaction.CommitAsync();
            return true;
        }
    }
}
