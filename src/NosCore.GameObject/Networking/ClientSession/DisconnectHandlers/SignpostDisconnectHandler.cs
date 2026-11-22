//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Services.MinilandService;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

// The Miniland a signpost leads to goes with its owner.
public class SignpostDisconnectHandler(IMinilandSignpostService signpostService) : ISessionDisconnectHandler
{
    public int Order => 200;

    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        await signpostService.TakeDownAsync(session.SelectedCharacterId);
    }
}
