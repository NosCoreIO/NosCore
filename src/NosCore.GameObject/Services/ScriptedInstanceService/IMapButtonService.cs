//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface IMapButtonService
    {
        // Starts the pull bar on a button within reach; false when the id is not a button.
        Task<bool> PressAsync(PlayerComponentBundle character, long visualId);

        // Pulls the button once its bar has run.
        Task UseAsync(PlayerComponentBundle character, int buttonId);
    }
}
