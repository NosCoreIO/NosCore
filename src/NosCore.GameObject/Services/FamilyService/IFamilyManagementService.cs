//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Networking.ClientSession;
using GenderType = NosCore.Packets.Enumerations.GenderType;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyManagementService
    {
        Task ChangeAuthorityAsync(ClientSession session, string? characterName, FamilyAuthority authority);

        Task<FamilyAuthorityChange> ChangeAuthorityAsync(ClientSession session, long targetId, FamilyAuthority authority);

        Task SendMemberListAsync(ClientSession session);

        Task OfferCreationAsync(ClientSession session);

        Task CreateAsync(ClientSession session, string? name);

        Task OfferDisbandAsync(ClientSession session);

        Task DisbandAsync(ClientSession session);

        Task SetNoticeAsync(ClientSession session, string? message);

        Task SetDailyMessageAsync(ClientSession session, string? message);

        Task SetTitleAsync(ClientSession session, string? characterName, FamilyMemberRank rank);

        Task SetHeadGenderAsync(ClientSession session, GenderType gender);
    }
}
