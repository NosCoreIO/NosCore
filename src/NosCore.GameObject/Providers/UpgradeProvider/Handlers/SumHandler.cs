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
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
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
        public EquipmentType[] acceptedItemType = { EquipmentType.Gloves, EquipmentType.Boots };
        public bool Condition(UpgradePacket packet) => packet.UpgradeType == UpgradePacketType.SumResistance;

        public void Execute(RequestData<UpgradePacket> requestData)
        {
            if (requestData.Data.Slot2 == null || requestData.Data.InventoryType2 == null)
            {
                return;
            }

            var item1 = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
                requestData.Data.Slot, (NoscorePocketType) requestData.Data.InventoryType);

            var item2 = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
                (byte) requestData.Data.Slot2, (NoscorePocketType) requestData.Data.InventoryType2);

            if (item1.ItemInstance.Upgrade + item2.ItemInstance.Upgrade + 1 > UpgradeHelper.Instance.SumHelpers.Count)
            {
                return;
            }

            if (!CheckAcceptedItemType(item1, item2))
            {
                return;
            }

            Sum(requestData.ClientSession, item1, item2);
        }

        private bool CheckAcceptedItemType(params InventoryItemInstance[] items)
        {
            foreach (var item in items)
            {
                if (Array.IndexOf(acceptedItemType, item.ItemInstance.Item.EquipmentSlot) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public InventoryItemInstance Sum(ClientSession clientSession, InventoryItemInstance item, InventoryItemInstance itemToSum)
        {
            var newUpgrade = item.ItemInstance.Upgrade + itemToSum.ItemInstance.Upgrade;
            if (clientSession.Character.Gold < UpgradeHelper.Instance.SumHelpers[newUpgrade].GoldPrice)
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            if (clientSession.Character.Inventory.CountItem(UpgradeHelper.SandVNum) <
                UpgradeHelper.Instance.SumHelpers[newUpgrade].SandAmount)
            {
                clientSession.SendPacket(clientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, clientSession.Account.Language),
                    SayColorType.Yellow));
                return null;
            }

            var random = (short)RandomFactory.Instance.RandomNumber();
            if (random <= UpgradeHelper.Instance.SumHelpers[newUpgrade].SuccessPercent)
            {
                HandleSuccessSum(clientSession, (WearableInstance)item.ItemInstance, (WearableInstance)itemToSum.ItemInstance);
            }
            else
            {
                HandleFailedSum(clientSession, (WearableInstance)item.ItemInstance, (WearableInstance)itemToSum.ItemInstance);
            }

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
    }
}
