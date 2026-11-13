//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.LandOfDeathService
{
    public interface ILandOfDeathService
    {
        bool IsRunning { get; }

        // The gate closes once the first Flying Fire Devil has gone, while the land itself stays open.
        bool IsGateOpen { get; }

        // False when the Land of Death is already open on this channel.
        bool TryStart();

        // Tells the player why they cannot go in, if they cannot.
        Task<bool> CanEnterAsync(ClientSession session);

        Task EnterAsync(ClientSession session);

        Task AskReviveAsync(PlayerComponentBundle player);

        Task ReturnToEntranceAsync(ClientSession session);
    }
}
