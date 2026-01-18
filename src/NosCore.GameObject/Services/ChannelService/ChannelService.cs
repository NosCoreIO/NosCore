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

using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ChannelService
{
    public class ChannelService(IAuthHub authHttpClient,
            IChannelHub channelHttpClient, ISaveService saveService)
        : IChannelService
    {
        public async Task MoveChannelAsync(Networking.ClientSession clientSession, int channelId)
        {
            var servers = await channelHttpClient.GetCommunicationChannels().ConfigureAwait(false);
            var server = servers.FirstOrDefault(x => x.Id == channelId);
            if (server == null || server.Type != ServerType.WorldServer)
            {
                return;
            }
            await clientSession.SendPacketAsync(new MzPacket(server.DisplayHost ?? server.Host)
            {
                Port = server.DisplayPort ?? server.Port,
                CharacterSlot = clientSession.Player.CharacterData.Slot
            });

            await clientSession.SendPacketAsync(new ItPacket
            {
                Mode = 1
            });

            await authHttpClient.SetAwaitingConnectionAsync(-1, clientSession.Account.Name);
            await saveService.SaveAsync(clientSession.Player);
            await clientSession.DisconnectAsync();
        }

    }
}