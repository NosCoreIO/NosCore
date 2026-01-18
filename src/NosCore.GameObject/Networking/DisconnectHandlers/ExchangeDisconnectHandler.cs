//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Networking.DisconnectHandlers;

public class ExchangeDisconnectHandler(IExchangeService exchangeService, ISessionRegistry sessionRegistry) : ISessionDisconnectHandler
{
    public async Task HandleDisconnectAsync(ClientSession session)
    {
        if (!session.TryGetPlayer(out var player))
        {
            return;
        }

        var exchange = exchangeService.GetExchange(player.VisualId);
        if (exchange == null)
        {
            return;
        }

        var targetId = exchange.GetPartnerId(player.VisualId);
        var closeExchange = exchangeService.CloseExchange(player.VisualId, ExchangeResultType.Failure);
        var targetSender = sessionRegistry.GetSenderByCharacterId(targetId);
        if (targetSender != null)
        {
            await targetSender.SendPacketAsync(closeExchange).ConfigureAwait(false);
        }
    }
}
