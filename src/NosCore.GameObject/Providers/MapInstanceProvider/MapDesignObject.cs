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

using ChickenAPI.Packets.ClientPackets.Miniland;
using ChickenAPI.Packets.Interfaces;
using NosCore.Data;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapDesignObject : MinilandObjectDto
    {
        public InventoryItemInstance InventoryItemInstance { get; set; }
        public short Effect { get; set; }

        public IPacket GenerateMapDesignObject(bool deleted)
        {
            return new MlobjPacket
            {
                Deleted = deleted,
                Slot = InventoryItemInstance.Slot,
                MapX = MapX,
                MapY = MapY,
                Width = InventoryItemInstance.ItemInstance.Item.Width,
                Height = InventoryItemInstance.ItemInstance.Item.Height,
                Unknown = 0,
                DurabilityPoint = (short)InventoryItemInstance.ItemInstance.DurabilityPoint,
                Unknown2 = 0,
                IsWarehouse = InventoryItemInstance.ItemInstance.Item.IsWarehouse
            };
        }
    }
}