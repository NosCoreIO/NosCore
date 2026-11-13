//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.InstantCombatService
{
    public interface IInstantCombatService
    {
        bool IsRunning { get; }

        // False when a combat is already running on this channel.
        bool TryStart(bool withCountdown);

        void SignUp(ClientSession session);
    }
}
