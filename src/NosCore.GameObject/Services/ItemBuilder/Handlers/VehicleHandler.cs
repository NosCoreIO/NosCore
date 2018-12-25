//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Services.ItemBuilder.Handlers
{
    public class VehicleHandler : IHandler<Item.Item, Tuple<IItemInstance, UseItemPacket>>
    {
        public bool Condition(Item.Item item) => item.ItemType == ItemType.Special && item.Effect == 1000;

        public void Execute(RequestData<Tuple<IItemInstance, UseItemPacket>> requestData)
        {
            var itemInstance = requestData.Data.Item1;
            var packet = requestData.Data.Item2;
            if (requestData.ClientSession.Character.InExchangeOrShop)
            {
                return;
            }

            if (packet.Mode == 1 && !requestData.ClientSession.Character.IsVehicled)
            {
                requestData.ClientSession.SendPacket(new DelayPacket
                {
                    Type = 3,
                    Delay = 3000,
                    Packet = requestData.ClientSession.Character.GenerateUseItem(itemInstance.Type, itemInstance.Slot,
                        2, 0)
                });
                return;
            }

            if (packet.Mode == 2 && !requestData.ClientSession.Character.IsVehicled)
            {
                requestData.ClientSession.Character.IsVehicled = true;
                requestData.ClientSession.Character.VehicleSpeed = itemInstance.Item.Speed;
                requestData.ClientSession.Character.MorphUpgrade = 0;
                requestData.ClientSession.Character.MorphDesign = 0;
                requestData.ClientSession.Character.Morph =
                    (short) ((short) requestData.ClientSession.Character.Gender + itemInstance.Item.Morph);
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
                    requestData.ClientSession.Character.GenerateEff(196));
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character
                    .GenerateCMode());
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateCond());
                return;
            }

            requestData.ClientSession.Character.RemoveVehicle();
        }
    }
}