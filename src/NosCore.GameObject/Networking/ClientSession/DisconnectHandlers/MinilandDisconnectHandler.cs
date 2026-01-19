//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class MinilandDisconnectHandler(IMinilandService minilandService, IMapInstanceGeneratorService mapInstanceGeneratorService) : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        var minilandId = await minilandService.DeleteMinilandAsync(session.Character.CharacterId);
        if (minilandId != null)
        {
            mapInstanceGeneratorService.RemoveMap((Guid)minilandId);
        }
    }
}
