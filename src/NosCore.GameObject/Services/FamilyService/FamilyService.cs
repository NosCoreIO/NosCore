//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    /// <inheritdoc cref="IFamilyService" />
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
                // A membership row pointing at a family that is gone. The character is treated as
                // having none, which is what the player already sees; keeping the orphan row
                // would make every family packet name a family that cannot be opened.
                return null;
            }

            var family = familyDto.Adapt<Family>();
            var memberRows = familyCharacterDao.Where(s => s.FamilyId == family.FamilyId)?.ToList()
                ?? new List<FamilyCharacterDto>();

            // One query for the names instead of one per member: the family window names the
            // head, and a large family is seventy rows.
            var memberIds = memberRows.Select(s => s.CharacterId).ToHashSet();
            var names = (characterDao.Where(s => memberIds.Contains(s.CharacterId))?.ToList()
                    ?? new List<CharacterDto>())
                .ToDictionary(s => s.CharacterId, s => s.Name);

            family.Members = memberRows.Select(row =>
            {
                var member = row.Adapt<FamilyCharacter>();
                member.Family = family;
                member.CharacterName = names.TryGetValue(row.CharacterId, out var name)
                    ? name
                    : string.Empty;
                return member;
            }).ToList();

            // Hand back the row belonging to the character that asked, taken from the same list
            // the family holds — so the caller and the family agree about the same object rather
            // than about two copies that can drift.
            return family.Members.First(s => s.CharacterId == characterId);
        }
    }
}
