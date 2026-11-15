//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.RainbowBattleService
{
    public interface IRainbowBattleService
    {
        bool IsRunning { get; }

        // False when a Rainbow Battle is already open on this channel.
        bool TryStart(bool withCountdown);

        Task RegisterAsync(ClientSession session);

        Task CancelAsync(ClientSession session);

        Task StartCaptureAsync(PlayerComponentBundle player, NpcComponentBundle crystal);

        Task CompleteCaptureAsync(PlayerComponentBundle player, int crystalId);

        Task StartRescueAsync(PlayerComponentBundle player, PlayerComponentBundle frozen);

        Task CompleteRescueAsync(PlayerComponentBundle player, long frozenId);

        Task PlayerDownAsync(PlayerComponentBundle player, IAliveEntity? killer);
    }
}
