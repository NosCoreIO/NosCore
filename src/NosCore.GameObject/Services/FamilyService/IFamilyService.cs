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

        Task AddMemberAsync(long familyId, long characterId);

        Task RemoveMemberAsync(long characterId);

        Task<CharacterDto?> FindCharacterAsync(string name);
        Task<IReadOnlyList<CharacterDto>> GetMemberCharactersAsync(Family family);
        Task<FamilyAuthorityChange> ChangeAuthorityAsync(Family family, long actorId, long targetId, FamilyAuthority authority);
        Task SaveAsync(Family family);
    }
}
