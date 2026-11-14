//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class MinilandDisconnectHandler(IMinilandService minilandService, IMapInstanceGeneratorService mapInstanceGeneratorService,
    ISessionRegistry sessionRegistry) : ISessionDisconnectHandler
{
    // Two players leaving each other's Miniland at once would otherwise wait on each other for good.
    private static readonly TimeSpan VisitorWait = TimeSpan.FromSeconds(5);

    // Last: removing the Miniland destroys the entity of whoever still stands in it, its owner included.
    public int Order => 200;

    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        var minilandId = await minilandService.DeleteMinilandAsync(session.SelectedCharacterId);
        if (minilandId is not { } mapInstanceId)
        {
            return;
        }

        // A visitor already on the way out is not moved by the removal, so its own steps, the save
        // among them, have to finish before the map and the entity on it go.
        var leaving = sessionRegistry.GetClientSessionsByMapInstance(mapInstanceId)
            .Where(visitor => visitor != session && visitor.TryGetCharacter(out var character) && character.IsDisconnecting)
            .Select(visitor => visitor.Disconnected)
            .ToList();
        try
        {
            await Task.WhenAll(leaving).WaitAsync(VisitorWait);
        }
        catch (TimeoutException)
        {
        }

        await mapInstanceGeneratorService.RemoveMapAsync(mapInstanceId);
    }
}
