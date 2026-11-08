//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class ShopDisconnectHandler : ISessionDisconnectHandler
{
    // Before the save, so no sale can land after the inventory was written.
    public int Order => -100;

    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter || session.Character.Shop == null)
        {
            return;
        }

        await session.Character.CloseShopAsync();
    }
}
