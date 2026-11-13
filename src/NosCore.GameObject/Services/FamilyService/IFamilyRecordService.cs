//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyRecordService
    {
        Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds);

        Task<bool> DisbandAsync(long familyId, long headId);

        Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId);

        Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId);

        Task<bool> ChangeAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members);
    }
}
