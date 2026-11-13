//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyService
    {
        Task<Family?> GetFamilyAsync(long characterId);

        Task<bool> IsNameTakenAsync(string name);

        Task<long?> CreateAsync(string name, byte maxSize, long headId, IEnumerable<long> deputyIds);

        Task<bool> DisbandAsync(Family family, long headId);

        Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId);

        Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId);

        Task<CharacterDto?> FindCharacterAsync(string name);
        Task<IReadOnlyList<CharacterDto>> GetMemberCharactersAsync(Family family);
        Task<FamilyAuthorityChange> ChangeAuthorityAsync(Family family, long actorId, long targetId, FamilyAuthority authority);
        Task<bool> UpdateAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members);
    }
}
