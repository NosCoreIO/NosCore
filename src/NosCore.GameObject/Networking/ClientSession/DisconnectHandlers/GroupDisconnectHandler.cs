//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Services.GroupService;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class GroupDisconnectHandler(IGroupLeaveService groupLeaveService) : ISessionDisconnectHandler
{
    // The rest of the group is told while the character is still on its map.
    public int Order => -50;

    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        await groupLeaveService.LeaveAsync(session.Character);
    }
}
