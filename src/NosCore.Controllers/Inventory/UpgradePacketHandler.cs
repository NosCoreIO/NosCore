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
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.UpgradeProvider;

namespace NosCore.PacketHandlers.Inventory
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly IUpgradeProvider _upgradeService;

        public UpgradePacketHandler(IUpgradeProvider upgradeService)
        {
            _upgradeService = upgradeService;
        }

        public override void Execute(UpgradePacket packet, ClientSession clientSession)
        {
            //todo fix https://github.com/ChickenAPI/ChickenAPI.Packets/issues/165
            var items = new List<InventoryItemInstance>();
            var item1 = clientSession.Character.Inventory.LoadBySlotAndType(packet.Slot,
                (NoscorePocketType)packet.InventoryType);
            if (item1 != null)
            {
                items.Add(item1);
            }

            if (packet.InventoryType2 != null && packet.Slot2 != null)
            {
                var item2 = clientSession.Character.Inventory.LoadBySlotAndType((short)packet.Slot2,
                    (NoscorePocketType)packet.InventoryType2);
                if (item2 != null)
                {
                    items.Add(item2);
                }
            }

            if (packet.CellonInventoryType != null && packet.CellonSlot != null)
            {
                var item3 = clientSession.Character.Inventory.LoadBySlotAndType((short)packet.CellonSlot,
                    (NoscorePocketType)packet.CellonInventoryType);
                if (item3 != null)
                {
                    items.Add(item3);
                }
            }
            _upgradeService.Upgrade(clientSession, new UpgradeProperties
            {
                UpgradeType = packet.UpgradeType,
                Items = items
            });
        }
    }
}
