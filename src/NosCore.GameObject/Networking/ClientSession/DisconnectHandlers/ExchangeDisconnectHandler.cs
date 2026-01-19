//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class ExchangeDisconnectHandler(IExchangeService exchangeService, ISessionRegistry sessionRegistry) : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        var targetId = exchangeService.GetTargetId(session.Character.VisualId);
        if (!targetId.HasValue)
        {
            return;
        }

        var closeExchange = exchangeService.CloseExchange(session.Character.VisualId, ExchangeResultType.Failure);
        if (sessionRegistry.GetCharacter(s => s.VisualId == targetId) is Character target)
        {
            await target.SendPacketAsync(closeExchange);
        }
    }
}
