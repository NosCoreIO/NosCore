//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface ITimeSpaceRewardService
    {
        // Gives the player the reward chest of the time-space they just won. False when there is
        // no chest for them: no time-space, a failed run, or a chest already opened.
        Task<bool> OpenChestAsync(ClientSession session);
    }
}
