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

using System;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;

namespace NosCore.PacketHandlers.Inventory
{
    public class UseItemPacketHandler : PacketHandler<UseItemPacket>, IWorldPacketHandler
    {
        public override Task Execute(UseItemPacket useItemPacket, ClientSession clientSession)
        {
            var inv =
                clientSession.Character.InventoryService.LoadBySlotAndType(useItemPacket.Slot,
                    (NoscorePocketType) useItemPacket.Type);
            if (inv?.ItemInstance.Requests == null)
            {
                return Task.CompletedTask;
            }

            inv.ItemInstance.Requests.OnNext(new RequestData<Tuple<InventoryItemInstance, UseItemPacket>>(clientSession,
                new Tuple<InventoryItemInstance, UseItemPacket>(inv, useItemPacket))); 
            return Task.CompletedTask;
        }
    }
}