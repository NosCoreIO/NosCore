//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
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

            var logs = familyLogDao.Where(s => s.FamilyId == familyId)?.Select(s => s.FamilyLogId).ToList()
                ?? new List<long>();
            var members = familyCharacterDao.Where(s => s.FamilyId == familyId)?.Select(s => s.FamilyCharacterId).ToList()
                ?? new List<long>();
            // The warehouse goes in the same transaction: a disband that fails keeps its contents.
            await using var transaction = daoTransactionScope.Begin();
            foreach (var stored in await warehouseService.GetItemsAsync(null, familyId, WarehouseType.FamilyWareHouse, null))
            {
                if (!await warehouseService.WithdrawItemAsync(stored.WarehouseItemId))
                {
                    return false;
                }
            }

            if ((logs.Count != 0 && await familyLogDao.TryDeleteAsync(logs) == null)
                || (members.Count != 0 && await familyCharacterDao.TryDeleteAsync(members) == null)
                || await familyDao.TryDeleteAsync(familyId) == null)
            {
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }

        public async Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var family = await LoadAsync(familyId);
            if (family == null || !family.CanInvite(inviterId)
                || await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId) != null)
            {
                return FamilyMemberAddition.NotAdded;
            }

            if (family.Members.Count >= family.MaxSize)
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

        public async Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var family = await LoadAsync(familyId);
            var membership = family?.Members.FirstOrDefault(s => s.CharacterId == characterId);
            if (family == null || membership == null)
            {
                return false;
            }

            // Leaving is open to everyone but the head; dismissing follows the ranks.
            var allowed = actorId == characterId
                ? membership.Authority != FamilyAuthority.Head
                : family.CanDismiss(actorId, characterId);
            return allowed && await familyCharacterDao.TryDeleteAsync(membership.FamilyCharacterId) != null;
        }

        // A channel checks rights against the copy of the family its session holds, which can be behind. The
        // rank and permissions stored here decide.
        public async Task<bool> ChangeAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members)
        {
            using var _ = await FamilyRowLock.Instance.AcquireAsync();
            var stored = await LoadAsync(familyId);
            if (stored == null || !IsAllowed(stored, actorId, family, members))
            {
                return false;
            }

            family?.ApplyTo(stored);
            var memberships = new List<FamilyCharacterDto>();
            foreach (var change in members)
            {
                var membership = stored.Members.First(s => s.CharacterId == change.CharacterId);
                change.ApplyTo(membership);
                memberships.Add(membership);
            }

            if (stored.Members.Count(s => s.Authority == FamilyAuthority.Head) != 1)
            {
                return false;
            }

            await using var transaction = daoTransactionScope.Begin();
            if (family != null && await familyDao.TryInsertOrUpdateAsync(stored.Adapt<FamilyDto>()) == null)
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

        private static bool IsAllowed(Family stored, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members)
        {
            var actor = stored.Members.FirstOrDefault(s => s.CharacterId == actorId);
            if (actor == null || members.Any(change => stored.Members.All(s => s.CharacterId != change.CharacterId)))
            {
                return false;
            }

            var isHead = actor.Authority == FamilyAuthority.Head;
            if (family != null)
            {
                if (family.FamilyMessage != null && !stored.CanNotice(actorId))
                {
                    return false;
                }

                // Everything else on the family row is the head's: its gender and what each rank may do.
                if (family with { FamilyMessage = null } != new FamilyChange() && !isHead)
                {
                    return false;
                }
            }

            var newHead = members.FirstOrDefault(s => s.Authority == FamilyAuthority.Head);
            foreach (var change in members)
            {
                if (change.DailyMessage != null && change.CharacterId != actorId)
                {
                    return false;
                }

                if (change.Rank != null && !isHead)
                {
                    return false;
                }

                if (change.Authority == null)
                {
                    continue;
                }

                // Passing the head makes the old head a deputy in the same request.
                var steppingDown = change.CharacterId == actorId && change.Authority == FamilyAuthority.Assistant
                    && newHead != null && newHead.CharacterId != actorId;
                if (!steppingDown && stored.CheckAuthorityChange(actorId, change.CharacterId, change.Authority.Value)
                    != FamilyAuthorityChange.Changed)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<Family?> LoadAsync(long familyId)
        {
            var dto = await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId);
            if (dto == null)
            {
                return null;
            }

            var family = dto.Adapt<Family>();
            family.Members = familyCharacterDao.Where(s => s.FamilyId == familyId)?.ToList() ?? new List<FamilyCharacterDto>();
            return family;
        }
    }
}
