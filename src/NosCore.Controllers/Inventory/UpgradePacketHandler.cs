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
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.UpgradeService;

namespace NosCore.PacketHandlers.Inventory
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly IUpgradeService _upgradeService;

        public UpgradePacketHandler(IUpgradeService upgradeService)
        {
            _upgradeService = upgradeService;
        }

        public override void Execute(UpgradePacket packet, ClientSession clientSession)
        {
            InventoryItemInstance item1 = null;
            InventoryItemInstance item2 = null;
            if (packet.UpgradeType == UpgradePacketType.SumResistance)
            {
                var acceptedItemType = new List<EquipmentType> { EquipmentType.Gloves, EquipmentType.Boots };

                item1 = clientSession.Character.Inventory.LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
                if (!(item1?.ItemInstance is WearableInstance))
                {
                    return;
                }

                if (packet.Slot2 == null || packet.InventoryType2 == null)
                {
                    return;
                }

                item2 = clientSession.Character.Inventory.LoadBySlotAndType((byte)packet.Slot2, (NoscorePocketType)packet.InventoryType2);
                if (!(item2?.ItemInstance is WearableInstance))
                {
                    return;
                }

                if (item1.ItemInstance.Upgrade + item2.ItemInstance.Upgrade > UpgradeHelper.Instance.MaxSumLevel)
                {
                    return;
                }

                if (!acceptedItemType.Contains(item1.ItemInstance.Item.EquipmentSlot) ||
                    !acceptedItemType.Contains(item2.ItemInstance.Item.EquipmentSlot))
                {
                    return;
                }

            }
            _upgradeService.HandlePacket(packet.UpgradeType, clientSession, item1, item2);
        }
    }
}
