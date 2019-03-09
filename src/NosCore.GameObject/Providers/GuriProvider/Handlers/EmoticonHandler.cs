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
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets;

namespace NosCore.GameObject.Providers.GuriProvider.Handlers
{
    public class EmoticonHandler : IHandler<GuriPacket, GuriPacket>
    {
        public bool Condition(GuriPacket packet) => packet.Type == 10 && packet.Data >= 973 && packet.Data <= 999;

        public void Execute(RequestData<GuriPacket> requestData)
        {
            if (requestData.ClientSession.Character.EmoticonsBlocked)
            {
                return;
            }

            if (requestData.Data.VisualEntityId.GetValueOrDefault() == requestData.ClientSession.Character.CharacterId)
            {
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
                    requestData.ClientSession.Character.GenerateEff(requestData.Data.Data +
                        4099)); //TODO , ReceiverType.AllNoEmoBlocked
            }
        }
    }
}