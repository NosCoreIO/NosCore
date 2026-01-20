//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking.SessionGroup;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class GroupDisconnectHandler(ISessionGroupFactory sessionGroupFactory, ISessionRegistry sessionRegistry) : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter || session.Character.Group == null)
        {
            return;
        }

        await session.Character.LeaveGroupAsync(sessionGroupFactory, sessionRegistry);
    }
}
