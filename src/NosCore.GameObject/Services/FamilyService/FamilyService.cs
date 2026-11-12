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
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    // Reads come straight from the database. Writes go to the master, the only writer of family rows.
    public class FamilyService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao,
        IDao<CharacterDto, long> characterDao, IFamilyHub familyHub) : IFamilyService
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

        public Task<long?> CreateAsync(string name, byte maxSize, long headId, IEnumerable<long> deputyIds) =>
            familyHub.CreateAsync(name, maxSize, headId, deputyIds.ToList());

        public Task<bool> DisbandAsync(Family family, long headId) => familyHub.DisbandAsync(family.FamilyId, headId);

        public Task<FamilyMemberAddition> AddMemberAsync(long familyId, long characterId) =>
            familyHub.AddMemberAsync(familyId, characterId);

        public Task<bool> RemoveMemberAsync(long familyId, long characterId) =>
            familyHub.RemoveMemberAsync(familyId, characterId);

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

            var changes = new List<FamilyMemberChange> { new(targetId) { Authority = authority } };
            if (authority == FamilyAuthority.Head)
            {
                changes.Add(new FamilyMemberChange(actorId) { Authority = FamilyAuthority.Assistant });
            }

            if (!await familyHub.ChangeAsync(family.FamilyId, null, changes))
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            if (authority == FamilyAuthority.Head)
            {
                family.Members.First(s => s.CharacterId == actorId).Authority = FamilyAuthority.Assistant;
            }

            membership.Authority = authority;
            return FamilyAuthorityChange.Changed;
        }

        public Task<bool> UpdateAsync(long familyId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members) =>
            familyHub.ChangeAsync(familyId, family, members);
    }
}
