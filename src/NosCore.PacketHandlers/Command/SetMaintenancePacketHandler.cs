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

using Json.More;
using Json.Patch;
using Json.Pointer;
using NosCore.Core;
using NosCore.Data.CommandPackets;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.HubInterfaces;
using NosCore.GameObject.HubClients.ChannelHubClient;
using NosCore.GameObject.HubClients.ChannelHubClient.Events;

namespace NosCore.PacketHandlers.Command
{
    public class SetMaintenancePacketHandler : PacketHandler<SetMaintenancePacket>, IWorldPacketHandler
    {
        private readonly IChannelHubClient _channelHubClient;
        private readonly Channel _channel;

        public SetMaintenancePacketHandler(IChannelHubClient channelHubClient, Channel channel)
        {
            _channelHubClient = channelHubClient;
            _channel = channel;
        }

        public override async Task ExecuteAsync(SetMaintenancePacket setMaintenancePacket, ClientSession session)
        {
            await _channelHubClient.BroadcastEvent(new Event<IEvent>(new MaintenanceEvent())
            {
                ChannelIds = setMaintenancePacket.IsGlobal == false ? new List<long>
                {
                    _channel.ChannelId
                } : new List<long>()
            });
        }
    }
}