//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Networking;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class MapDisconnectHandler : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        if (session.Channel != null)
        {
            session.Character.MapInstance?.Sessions.Remove(session.Channel);
        }

        if (session.HasPlayerEntity)
        {
            await session.Character.MapInstance!.SendPacketAsync(session.Character.GenerateOut());
        }
    }
}
