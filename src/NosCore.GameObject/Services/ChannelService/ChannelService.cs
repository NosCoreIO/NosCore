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
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.GameObject.Networking.LoginService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ChannelService
{
    public class ChannelService : IChannelService
    {
        private readonly IAuthHttpClient _authHttpClient;
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly ISaveService _saveService;

        public ChannelService(IAuthHttpClient authHttpClient,
            IChannelHttpClient channelHttpClient, ISaveService saveService)
        {
            _authHttpClient = authHttpClient;
            _channelHttpClient = channelHttpClient;
            _saveService = saveService;
        }

        public async Task MoveChannelAsync(Networking.ClientSession.ClientSession clientSession, int channelId)
        {
            var server = await _channelHttpClient.GetChannelAsync(channelId).ConfigureAwait(false);
            if (server == null || server.Type != ServerType.WorldServer)
            {
                return;
            }
            await clientSession.SendPacketAsync(new MzPacket
            {
                Port = server.DisplayPort ?? server.Port,
                Ip = server.DisplayHost ?? server.Host,
                CharacterSlot = clientSession.Character.Slot
            });

            await clientSession.SendPacketAsync(new ItPacket
            {
                Mode = 1
            });

            await _authHttpClient.SetAwaitingConnectionAsync(-1, clientSession.Account.Name);
            await _saveService.SaveAsync(clientSession.Character);
            await clientSession.DisconnectAsync();
        }

    }
}