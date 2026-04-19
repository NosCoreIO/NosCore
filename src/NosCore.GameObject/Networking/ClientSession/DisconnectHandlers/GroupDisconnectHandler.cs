//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class GroupDisconnectHandler : ISessionDisconnectHandler
{
    public Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return Task.CompletedTask;
        }

        var character = session.Character;
        if (character.Group == null)
        {
            return Task.CompletedTask;
        }

        character.Group = null;
        return Task.CompletedTask;
    }
}
