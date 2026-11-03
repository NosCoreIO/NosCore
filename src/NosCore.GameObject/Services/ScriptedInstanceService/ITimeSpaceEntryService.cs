//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface ITimeSpaceEntryService
    {
        Task ShowAsync(ClientSession session, ScriptedInstance entrance);

        Task EnterAsync(ClientSession session, ScriptedInstance entrance);
    }
}
