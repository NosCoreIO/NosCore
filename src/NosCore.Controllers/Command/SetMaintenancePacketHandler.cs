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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.StatHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class SetMaintenancePacketHandler : PacketHandler<SetMaintenancePacket>, IWorldPacketHandler
    {
        private readonly IChannelHttpClient _channelHttpClient;

        public SetMaintenancePacketHandler(IChannelHttpClient channelHttpClient)
        {
            _channelHttpClient = channelHttpClient;
        }

        public override async Task ExecuteAsync(SetMaintenancePacket setMaintenancePacket, ClientSession session)
        {
            var servers = (await _channelHttpClient.GetChannelsAsync().ConfigureAwait(false))
                ?.Where(c => c.Type == ServerType.WorldServer).ToList();

            if (setMaintenancePacket.IsGlobal == false)
            {
                await _channelHttpClient.SetMaintenanceAsync(MasterClientListSingleton.Instance.ChannelId, setMaintenancePacket.MaintenanceMode);
            }
            else
            {
                foreach (var server in servers ?? new List<ChannelInfo>())
                {
                    await _channelHttpClient.SetMaintenanceAsync(server.Id, setMaintenancePacket.MaintenanceMode);
                }
            }
        }
    }
}