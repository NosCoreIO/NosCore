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
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class SpRechargerEventHandlerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void Test_SpRecharger_When_Max()
        {
            //else
            //{
            //    requestData.ClientSession.Character.SendPacket(new MsgPacket
            //    {
            //        Message = Language.Instance.GetMessageFromKey(LanguageKey.SP_ADDPOINTS_FULL,
            //            requestData.ClientSession.Character.Account.Language),
            //        Type = MessageType.White
            //    });
            //}
            Assert.Fail();
        }

        [TestMethod]
        public void Test_SpRecharger()
        {
            //if (requestData.ClientSession.Character.SpAdditionPoint < _worldConfiguration.MaxAdditionalSpPoints)
            //{
            //    var itemInstance = requestData.Data.Item1;
            //    requestData.ClientSession.Character.Inventory.RemoveItemAmountFromInventory(1,
            //        itemInstance.ItemInstanceId);
            //    requestData.ClientSession.SendPacket(
            //        itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            //    requestData.ClientSession.Character.AddAdditionalSpPoints(itemInstance.ItemInstance.Item.EffectValue);
            //}
            Assert.Fail();
        }
    }
}