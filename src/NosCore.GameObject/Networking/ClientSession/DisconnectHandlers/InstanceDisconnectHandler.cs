//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.RaidService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

// Runs after the map has dropped the session, so a run left empty can be torn down.
public class InstanceDisconnectHandler(IRaidService raidService) : ISessionDisconnectHandler
{
    public int Order => 200;

    public Task HandleDisconnectAsync(ClientSession session)
    {
        return session.HasPlayerEntity ? raidService.DisconnectAsync(session) : Task.CompletedTask;
    }
}
