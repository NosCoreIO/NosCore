//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.ScriptedInstanceService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class TimeSpaceDisconnectHandler(IScriptedInstanceService scriptedInstanceService) : ISessionDisconnectHandler
{
    // After MapDisconnectHandler, so the leaving player no longer counts as inside the room.
    public int Order => 100;

    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter || session.Character.MapInstance is not { } mapInstance)
        {
            return;
        }

        var run = scriptedInstanceService.GetRun(mapInstance.MapInstanceId);
        if (run != null)
        {
            await scriptedInstanceService.DisposeIfEmptyAsync(run);
        }
    }
}
