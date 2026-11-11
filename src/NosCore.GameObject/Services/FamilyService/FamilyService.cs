//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Family;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao,
        IDao<FamilyLogDto, long> familyLogDao,
        IDao<CharacterDto, long> characterDao) : IFamilyService
    {
        private const int MaxAssistants = 2;

        public async Task<Family?> GetFamilyAsync(long characterId)
        {
            var membership = await familyCharacterDao
                .FirstOrDefaultAsync(s => s.CharacterId == characterId);
            if (membership == null)
            {
                return null;
            }

            var familyDto = await familyDao
                .FirstOrDefaultAsync(s => s.FamilyId == membership.FamilyId);
            if (familyDto == null)
            {
                return null;
            }

            var family = familyDto.Adapt<Family>();
            family.Members = familyCharacterDao.Where(s => s.FamilyId == family.FamilyId)?.ToList()
                ?? new List<FamilyCharacterDto>();

            var head = family.Members.FirstOrDefault(s => s.Authority == FamilyAuthority.Head);
            if (head != null)
            {
                var headCharacter = await characterDao
                    .FirstOrDefaultAsync(s => s.CharacterId == head.CharacterId);
                family.HeadCharacterName = headCharacter?.Name ?? string.Empty;
            }

            return family;
        }

        public async Task<bool> IsNameTakenAsync(string name) =>
            await familyDao.FirstOrDefaultAsync(s => s.Name == name) != null;

        public async Task<long?> CreateAsync(string name, byte maxSize, long headId, IEnumerable<long> deputyIds)
        {
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

            var members = deputyIds
                .Select(id => (id, FamilyAuthority.Assistant))
                .Prepend((headId, FamilyAuthority.Head))
                .Select(member => new FamilyCharacterDto
                {
                    FamilyId = family.FamilyId,
                    CharacterId = member.Item1,
                    Authority = member.Item2,
                    DailyMessage = string.Empty
                });
            return await familyCharacterDao.TryInsertOrUpdateAsync(members) ? family.FamilyId : null;
        }

        public async Task<bool> DisbandAsync(Family family)
        {
            var logs = familyLogDao.Where(s => s.FamilyId == family.FamilyId)?.Select(s => s.FamilyLogId).ToList()
                ?? new List<long>();
            var members = familyCharacterDao.Where(s => s.FamilyId == family.FamilyId)?.Select(s => s.FamilyCharacterId).ToList()
                ?? new List<long>();
            return (logs.Count == 0 || await familyLogDao.TryDeleteAsync(logs) != null)
                && (members.Count == 0 || await familyCharacterDao.TryDeleteAsync(members) != null)
                && await familyDao.TryDeleteAsync(family.FamilyId) != null;
        }

        public Task AddMemberAsync(long familyId, long characterId) =>
            familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = familyId,
                CharacterId = characterId,
                Authority = FamilyAuthority.Member,
                DailyMessage = string.Empty
            });

        public async Task RemoveMemberAsync(long characterId)
        {
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);
            if (membership != null)
            {
                await familyCharacterDao.TryDeleteAsync(membership.FamilyCharacterId);
            }
        }

        public async Task<CharacterDto?> FindCharacterAsync(string name) =>
            await characterDao.FirstOrDefaultAsync(s => s.Name == name && s.State == CharacterState.Active);

        public Task<IReadOnlyList<CharacterDto>> GetMemberCharactersAsync(Family family)
        {
            var ids = family.Members.Select(s => s.CharacterId).ToHashSet();
            IReadOnlyList<CharacterDto> characters = characterDao.Where(s => ids.Contains(s.CharacterId))?.ToList()
                ?? new List<CharacterDto>();
            return Task.FromResult(characters);
        }

        public async Task<FamilyAuthorityChange> ChangeAuthorityAsync(Family family, long actorId, long targetId, FamilyAuthority authority)
        {
            var membership = family.Members.FirstOrDefault(s => s.CharacterId == targetId);
            if (membership == null)
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            var actorAuthority = family.AuthorityOf(actorId);
            if (actorAuthority is not (FamilyAuthority.Head or FamilyAuthority.Assistant)
                || targetId == actorId || membership.Authority == FamilyAuthority.Head)
            {
                return FamilyAuthorityChange.NotAuthorized;
            }

            switch (authority)
            {
                case FamilyAuthority.Head when actorAuthority != FamilyAuthority.Head:
                case FamilyAuthority.Assistant when actorAuthority != FamilyAuthority.Head:
                    return FamilyAuthorityChange.NotAuthorized;
                case FamilyAuthority.Head when membership.Authority != FamilyAuthority.Assistant:
                    return FamilyAuthorityChange.OnlyAssistantCanBecomeHead;
                case FamilyAuthority.Assistant when family.Members.Count(s => s.Authority == FamilyAuthority.Assistant) >= MaxAssistants:
                    return FamilyAuthorityChange.TooManyAssistants;
                case FamilyAuthority.Manager or FamilyAuthority.Member
                    when membership.Authority == FamilyAuthority.Assistant && actorAuthority != FamilyAuthority.Head:
                    return FamilyAuthorityChange.NotAuthorized;
            }

            if (authority == FamilyAuthority.Head)
            {
                if (!await UpdateMemberAsync(actorId, s => s.Authority = FamilyAuthority.Assistant))
                {
                    return FamilyAuthorityChange.NotInFamily;
                }

                family.Members.First(s => s.CharacterId == actorId).Authority = FamilyAuthority.Assistant;
            }

            if (!await UpdateMemberAsync(targetId, s => s.Authority = authority))
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            membership.Authority = authority;
            return FamilyAuthorityChange.Changed;
        }

        // The copy held by a session can be hours old, so a change is applied to the stored row rather
        // than writing that copy back over what the master has saved since.
        public async Task<bool> UpdateAsync(long familyId, Action<FamilyDto> change)
        {
            var family = await familyDao.FirstOrDefaultAsync(s => s.FamilyId == familyId);
            if (family == null)
            {
                return false;
            }

            change(family);
            return await familyDao.TryInsertOrUpdateAsync(family) != null;
        }

        public async Task<bool> UpdateMemberAsync(long characterId, Action<FamilyCharacterDto> change)
        {
            var membership = await familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);
            if (membership == null)
            {
                return false;
            }

            change(membership);
            return await familyCharacterDao.TryInsertOrUpdateAsync(membership) != null;
        }
    }
}
