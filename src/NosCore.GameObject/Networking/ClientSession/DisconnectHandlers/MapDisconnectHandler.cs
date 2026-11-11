//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.MapInstanceGenerationService;
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

        // A character whose entity is already gone reads back a default component, with no map.
        MapInstance? current = session.Character.MapInstance;
        if (session.Channel != null)
        {
            current?.Sessions.Remove(session.Channel);
        }

        if (session.HasPlayerEntity)
        {
            var mapInstance = session.Character.MapInstance;
            await mapInstance.SendPacketAsync(session.Character.GenerateOut());
            foreach (var mate in session.Character.Mates.Values)
            {
                await mate.DespawnAsync(mapInstance);
            }
        }
    }
}
