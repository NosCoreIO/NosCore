//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class GroupDisconnectHandler : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter || session.Character.Group == null)
        {
            return;
        }

        await session.Character.LeaveGroupAsync();
    }
}
