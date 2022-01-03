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

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using System;
using System.Threading.Tasks;
using NosCore.Networking;


namespace NosCore.GameObject.Services.GuriRunnerService.Handlers
{
    public class EmoticonEventHandler : IGuriEventHandler
    {
        public bool Condition(GuriPacket packet)
        {
            return (packet.Type == GuriPacketType.TextInput) && (packet.Data >= 973) && (packet.Data <= 999);
        }

        public Task ExecuteAsync(RequestData<GuriPacket> requestData)
        {
            if (requestData.ClientSession.Character.EmoticonsBlocked)
            {
                return Task.CompletedTask;
            }

            if (requestData.Data.VisualId.GetValueOrDefault() == requestData.ClientSession.Character.CharacterId)
            {
                return requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateEff(Convert.ToInt32(requestData.Data.Data) +
                        4099)); //TODO , ReceiverType.AllNoEmoBlocked
            }

            return Task.CompletedTask;
        }
    }
}