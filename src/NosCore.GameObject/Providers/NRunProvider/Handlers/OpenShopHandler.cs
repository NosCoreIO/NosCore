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

using ChickenAPI.Packets.ClientPackets.Npcs;
using ChickenAPI.Packets.ClientPackets.Shops;
using ChickenAPI.Packets.Enumerations;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using System;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class OpenShopEventHandler : IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item) => item.Item2.Runner == NrunRunnerType.OpenShop && item.Item1 != null;

        public void Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            requestData.ClientSession.HandlePackets(new[] {new ShoppingPacket
            {
                VisualType = (VisualType) requestData.Data.Item2.VisualType,
                VisualId = (long) requestData.Data.Item2.VisualId,
                ShopType = (short) requestData.Data.Item2.Type,
                Unknown = 0
            }});
        }
    }
}