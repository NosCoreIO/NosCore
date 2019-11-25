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

using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
using NosCore.Data;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
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
            _upgradeService.UpgradeLaunch(clientSession, packet);

            var item = clientSession.Character.Inventory.LoadBySlotAndType(packet.Slot,
                (NoscorePocketType)packet.InventoryType);
            if (packet.Slot2 == null || packet.InventoryType2 == null)
            {
                return;
            }
            var itemToSum = clientSession.Character.Inventory.LoadBySlotAndType((byte)packet.Slot2,
                (NoscorePocketType)packet.InventoryType2);
            UpdateInv(clientSession, item, itemToSum);
        }

        private void UpdateInv(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            var newUpgrade = item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade - 1;
            clientSession.Character.Gold -= UpgradeHelper.Instance.SumHelpers[newUpgrade].GoldPrice;
            clientSession.Character.Inventory.RemoveItemAmountFromInventoryByVNum(
                UpgradeHelper.Instance.SumHelpers[newUpgrade].SandAmount,
                UpgradeHelper.Instance.SandVNum).GeneratePocketChange();
            clientSession.SendPacket(clientSession.Character.GenerateGold());
            clientSession.SendPacket(
                ((InventoryItemInstance)null).GeneratePocketChange(PocketType.Equipment, itemToSum.Slot));
            clientSession.SendPacket(item.GeneratePocketChange(PocketType.Equipment, item.Slot));
        }
    }
}
