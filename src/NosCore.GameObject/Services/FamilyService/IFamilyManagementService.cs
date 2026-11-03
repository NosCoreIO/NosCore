//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyManagementService
    {
        Task ChangeAuthorityAsync(ClientSession session, string? characterName, FamilyAuthority authority);

        Task<FamilyAuthorityChange> ChangeAuthorityAsync(ClientSession session, long targetId, FamilyAuthority authority);
    }
}
