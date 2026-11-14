//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.RaidService
{
    public interface IRaidService
    {
        Task OpenAsync(ClientSession leader, InventoryItemInstance seal);

        Task InviteAsync(ClientSession leader, long characterId);

        Task JoinAsync(ClientSession joiner, long leaderId);

        Task LeaveAsync(ClientSession member);

        Task KickAsync(ClientSession leader, long characterId);

        Task DisbandAsync(ClientSession leader);

        Task<bool> UseGateAsync(ClientSession session);

        Task StartAsync(ClientSession leader);

        Task DisconnectAsync(ClientSession member);

        Task ShowBoardAsync(ClientSession session);

        Task ListAsync(ClientSession leader);

        Task UnlistAsync(ClientSession leader);

        Task JoinListedAsync(ClientSession joiner, string leaderName);
    }
}
