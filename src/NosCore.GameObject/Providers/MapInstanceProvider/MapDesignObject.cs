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
using ChickenAPI.Packets.ServerPackets.Map;
using NosCore.Data;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.MapInstanceProvider
{
    public class MapDesignObject : MinilandObjectDto
    {
        public MapDesignObject(InventoryItemInstance inventoryItemInstance)
        {
            Effect = (short)(inventoryItemInstance.ItemInstance.Item?.EffectValue ?? inventoryItemInstance.ItemInstance.Design);
            Width = inventoryItemInstance.ItemInstance.Item.Width;
            Height = inventoryItemInstance.ItemInstance.Item.Height;
            DurabilityPoint = (short)inventoryItemInstance.ItemInstance.DurabilityPoint;
            IsWarehouse = inventoryItemInstance.ItemInstance.Item.IsWarehouse;
            ItemInstanceId = inventoryItemInstance.Id;
            InventoryItemInstance = inventoryItemInstance;
        }

        public short Effect { get; set; }

        public short Width { get; set; }

        public short Height { get; set; }

        public short Slot { get; set; }

        public short DurabilityPoint { get; set; }

        public bool IsWarehouse { get; set; }

        public InventoryItemInstance InventoryItemInstance { get; set; }

        public GroundEffectPacket GenerateEffect() => GenerateEffect(false);
        public GroundEffectPacket GenerateEffect(bool isRemoval)
        {
            return new GroundEffectPacket
            {
                Effect = (ushort)Effect,
                XYCoordinates = $"{MapX}{MapY.ToString("00")}",
                MapX = (ushort)MapX,
                MapY = (ushort)MapY,
                IsRemoval = isRemoval
            };
        }
        public MlobjPacket GenerateMapDesignObject() => GenerateMapDesignObject(false);
        public MlobjPacket GenerateMapDesignObject(bool deleted)
        {
            return new MlobjPacket
            {
                Deleted = deleted,
                Slot = Slot,
                MapX = MapX,
                MapY = MapY,
                Width = Width,
                Height = Height,
                Unknown = 0,
                DurabilityPoint = DurabilityPoint,
                Unknown2 = 0,
                IsWarehouse = IsWarehouse
            };
        }
    }
}