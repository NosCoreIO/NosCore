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
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.Shop;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Helper;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.GameObject.Providers.UpgradeProvider.Handlers
{
    public class SumHandler : IEventHandler<UpgradePacket, UpgradePacket>
    {
        public bool Condition(UpgradePacket packet)
        {
            return (packet.UpgradeType == UpgradePacketType.SumResistance);
        }

        public void Execute(RequestData<UpgradePacket> requestData)
        {
            InventoryItemInstance item1 = null;
            InventoryItemInstance item2 = null;
            if (requestData.Data.UpgradeType == UpgradePacketType.SumResistance)
            {
                var acceptedItemType = new List<EquipmentType> { EquipmentType.Gloves, EquipmentType.Boots };

                item1 = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(requestData.Data.Slot, (NoscorePocketType)requestData.Data.InventoryType);
                if (!(item1?.ItemInstance is WearableInstance))
                {
                    return;
                }

                if (requestData.Data.Slot2 == null || requestData.Data.InventoryType2 == null)
                {
                    return;
                }

                item2 = requestData.ClientSession.Character.Inventory.LoadBySlotAndType((byte)requestData.Data.Slot2, (NoscorePocketType)requestData.Data.InventoryType2);
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

            Sum(requestData.ClientSession, item1, item2);
        }

        public InventoryItemInstance Sum(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            if (clientSession.Character.Gold <
                UpgradeHelper.Instance.SumGoldPrice[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            if (clientSession.Character.Inventory.CountItem(1027) <
                UpgradeHelper.Instance.SumSandAmount[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            var random = (short)RandomFactory.Instance.RandomNumber();
            if (random <=
                UpgradeHelper.Instance.SumSuccessPercent[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade])
            {
                HandleSuccessSum(clientSession, (WearableInstance)item.ItemInstance, (WearableInstance)itemToSum.ItemInstance);
            }
            else
            {
                HandleFailedSum(clientSession, (WearableInstance)item.ItemInstance, (WearableInstance)itemToSum.ItemInstance);
            }

            UpdateInv(clientSession, item, itemToSum);
            clientSession.SendPacket(new ShopEndPacket
            {
                Type = ShopEndPacketType.CloseSubWindow
            });

            return item;
        }

        private void HandleSuccessSum(ClientSession clientSession, WearableInstance item,
            WearableInstance itemToSum)
        {
            item.Upgrade += (byte)(itemToSum.Upgrade + 1);
            item.DarkResistance += itemToSum.DarkResistance;
            item.LightResistance += itemToSum.LightResistance;
            item.FireResistance += itemToSum.FireResistance;
            item.WaterResistance += itemToSum.WaterResistance;

            clientSession.SendPacket(new PdtiPacket
            {
                Unknow = 10,
                ItemVnum = item.ItemVNum,
                RecipeAmount = 1,
                Unknow3 = 27,
                ItemUpgrade = item.Upgrade,
                Unknow4 = 0
            });
            SendSumResult(clientSession, itemToSum, true);
        }

        private void HandleFailedSum(ClientSession clientSession, WearableInstance item,
            WearableInstance itemToSum)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemToSum.Id);
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, item.Id);
            SendSumResult(clientSession, itemToSum, false);
        }

        private void SendSumResult(ClientSession clientSession, WearableInstance itemToSum, bool success)
        {
            clientSession.Character.Inventory.RemoveItemAmountFromInventory(1, itemToSum.Id);
            clientSession.SendPacket(new MsgPacket
            {
                Message = Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language)
            });
            clientSession.SendPacket(clientSession.Character.GenerateSay(
                Language.Instance.GetMessageFromKey(
                    success ? LanguageKey.SUM_SUCCESS : LanguageKey.SUM_FAILED,
                    clientSession.Account.Language),
                success ? SayColorType.Green : SayColorType.Purple));
            clientSession.SendPacket(new GuriPacket
            {
                Type = GuriPacketType.AfterSumming,
                Unknown = 1,
                EntityId = clientSession.Character.VisualId,
                Value = success ? (uint)1324 : 1332
            });
        }

        private void UpdateInv(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            clientSession.Character.Gold -=
                UpgradeHelper.Instance.SumGoldPrice[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade - 1];
            clientSession.SendPacket(clientSession.Character.GenerateGold());

            var invMainReload = new InvPacket
            {
                Type = PocketType.Main,
                IvnSubPackets = new List<IvnSubPacket>()
            };
            List<InventoryItemInstance> removedSand =
                clientSession.Character.Inventory.RemoveItemAmountFromInventoryByVNum(
                    (byte)UpgradeHelper.Instance.SumSandAmount[item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade - 1],
                    1027);
            foreach (InventoryItemInstance inventoryItemInstance in removedSand)
            {
                invMainReload.IvnSubPackets.Add(
                    inventoryItemInstance.ItemInstance.GenerateIvnSubPacket(PocketType.Main,
                        inventoryItemInstance.Slot));
            }

            itemToSum.ItemInstance = null;
            var invEquipReload = new InvPacket
            {
                Type = PocketType.Equipment,
                IvnSubPackets = new List<IvnSubPacket>
                {
                    item.ItemInstance.GenerateIvnSubPacket(PocketType.Equipment, item.Slot),
                    itemToSum.ItemInstance.GenerateIvnSubPacket(PocketType.Equipment, itemToSum.Slot)
                }
            };

            clientSession.SendPackets(new List<IPacket> { invEquipReload, invMainReload });
        }
    }
}
