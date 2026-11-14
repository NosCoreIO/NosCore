//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface IInstanceExitService
    {
        // Takes a player out of the scripted instance they stand in, the way its own exit would.
        Task LeaveAsync(ClientSession session);
    }
}
