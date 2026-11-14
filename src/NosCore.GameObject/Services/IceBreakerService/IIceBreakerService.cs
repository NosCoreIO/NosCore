//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.IceBreakerService
{
    public interface IIceBreakerService
    {
        bool IsRunning { get; }

        // False when an Icebreaker is already running on this channel.
        bool TryStart(bool withCountdown);

        Task JoinAsync(ClientSession session);
    }
}
