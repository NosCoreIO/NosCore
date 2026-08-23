//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyService(IDao<FamilyDto, long> familyDao,
        IDao<FamilyCharacterDto, long> familyCharacterDao,
        IDao<CharacterDto, long> characterDao) : IFamilyService
    {
        public async Task<FamilyCharacter?> GetMembershipAsync(long characterId)
        {
            var membership = await familyCharacterDao
                .FirstOrDefaultAsync(s => s.CharacterId == characterId).ConfigureAwait(false);
            if (membership == null)
            {
                return null;
            }

            var familyDto = await familyDao
                .FirstOrDefaultAsync(s => s.FamilyId == membership.FamilyId).ConfigureAwait(false);
            if (familyDto == null)
            {
                return null;
            }

            var family = familyDto.Adapt<Family>();
            var memberRows = familyCharacterDao.Where(s => s.FamilyId == family.FamilyId)?.ToList()
                ?? new List<FamilyCharacterDto>();

            family.Members = memberRows.Select(row =>
            {
                var member = row.Adapt<FamilyCharacter>();
                member.Family = family;
                return member;
            }).ToList();

            // Only the head's name is ever printed, so only the head's name is fetched.
            var head = family.Members.FirstOrDefault(s => s.Authority == FamilyAuthority.Head);
            if (head != null)
            {
                var headCharacter = await characterDao
                    .FirstOrDefaultAsync(s => s.CharacterId == head.CharacterId).ConfigureAwait(false);
                family.HeadCharacterName = headCharacter?.Name ?? string.Empty;
            }

            return family.Members.First(s => s.CharacterId == characterId);
        }
    }
}
