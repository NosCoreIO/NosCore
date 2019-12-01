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
using System.Linq;
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
    public class SumHandler : IEventHandler<UpgradeProperties, UpgradeProperties>
    {
        public readonly EquipmentType[] acceptedItemType = { EquipmentType.Gloves, EquipmentType.Boots };

        public bool Condition(UpgradeProperties prop)
        {
            return prop.UpgradeType == UpgradePacketType.SumResistance
                && prop.Items.Count > 1
                && prop.Items.TrueForAll(s=> acceptedItemType.Contains(s.ItemInstance.Item.EquipmentSlot) && s.ItemInstance is WearableInstance)
                && prop.Items[0].ItemInstance.Upgrade + prop.Items[1].ItemInstance.Upgrade + 1 <= UpgradeHelper.Instance.SumHelpers.Count;
        } 

        public void Execute(RequestData<UpgradeProperties> requestData)
        {
            var newUpgrade = requestData.Data.Items[0].ItemInstance.Upgrade + requestData.Data.Items[1].ItemInstance.Upgrade;
            if (requestData.ClientSession.Character.Gold < UpgradeHelper.Instance.SumHelpers[newUpgrade].Cost)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, requestData.ClientSession.Account.Language),
                    SayColorType.Yellow));
                return;
            }

            if (requestData.ClientSession.Character.Inventory.CountItem(UpgradeHelper.SandVNum) <
                UpgradeHelper.Instance.SumHelpers[newUpgrade].CellaCost)
            {
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, requestData.ClientSession.Account.Language),
                    SayColorType.Yellow));
                return;
            }

            requestData.ClientSession.Character.Gold -= UpgradeHelper.Instance.SumHelpers[newUpgrade].Cost;
            requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventoryByVNum(
                UpgradeHelper.Instance.SumHelpers[newUpgrade].CellaCost,
                UpgradeHelper.SandVNum).GeneratePocketChange();
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateGold());

            var random = (short)RandomFactory.Instance.RandomNumber();
            var success = random <= UpgradeHelper.Instance.SumHelpers[newUpgrade].SuccessRate;
            if (success)
            {
                HandleSuccessSum(requestData.ClientSession, requestData.Data.Items[0].ItemInstance as WearableInstance, requestData.Data.Items[1].ItemInstance as WearableInstance);
            }
            else
            {
                requestData.ClientSession.SendPacket(((InventoryItemInstance)null).GeneratePocketChange((PocketType)requestData.Data.Items[0].Type, requestData.Data.Items[0].Slot));
                requestData.ClientSession.Character.Inventory.DeleteById(requestData.Data.Items[0].Id);
            }
            SendSumResult(requestData.ClientSession, success);
            requestData.ClientSession.SendPacket(
                ((InventoryItemInstance)null).GeneratePocketChange((PocketType)requestData.Data.Items[1].Type, requestData.Data.Items[1].Slot));

            requestData.ClientSession.SendPacket(new ShopEndPacket
            {
                Type = ShopEndPacketType.CloseSubWindow
            });
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

            clientSession.Character.Inventory.DeleteById(itemToSum.Id);
            SendSumResult(clientSession, true);
        }

        private void SendSumResult(ClientSession clientSession, bool success)
        {
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
