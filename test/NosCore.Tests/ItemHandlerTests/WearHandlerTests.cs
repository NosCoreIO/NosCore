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
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.ItemHandlerTests
{
    [TestClass]
    public class WearEventHandlerTests : UseItemEventHandlerTests
    {
        private ItemProvider _itemProvider;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
            _session = TestHelpers.Instance.GenerateSession();
            _handler = new WearEventHandler(_logger.Object);
            var items = new List<ItemDto>
            {
                new Item
                {
                    VNum = 1,
                    Type = NoscorePocketType.Equipment, 
                    ItemType = ItemType.Weapon,
                    RequireBinding = true
                },
                new Item
                { 
                    VNum = 2,
                    Type = NoscorePocketType.Equipment, 
                    EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Water
                },
                new Item
                {  
                    VNum = 3,
                    Type = NoscorePocketType.Equipment,
                    EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Fire
                },
                new Item
                { 
                    VNum = 4,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Specialist,
                    ReputationMinimum = 2,
                    Element = ElementType.Fire
                },
            };
            _itemProvider = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>());
        }

        [TestMethod]
        public void Test_Can_Not_Use_WearEvent_In_Shop()
        {
            _session.Character.InShop = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            _logger.Verify(s => s.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_USE_ITEM_IN_SHOP)), Times.Exactly(1));
        }

        [TestMethod]
        public void Test_BoundCharacter_Question()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            var lastpacket = (QnaPacket)_session.LastPackets.FirstOrDefault(s => s is QnaPacket);
            Assert.AreEqual(_session.GetMessageFromKey(LanguageKey.ASK_BIND), lastpacket.Question);
        }

        [TestMethod]
        public void Test_BoundCharacter()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider.Create(1), _session.Character.CharacterId);
            ExecuteInventoryItemInstanceEventHandler(itemInstance);
            _useItem.Mode = 1;
            Assert.AreEqual(_session.Character.CharacterId, itemInstance.ItemInstance.BoundCharacterId);
        }

        [TestMethod]
        public void Test_BadEquipment()
        {
            Assert.Fail();
            //if ((itemInstance.ItemInstance.Item.LevelMinimum > (itemInstance.ItemInstance.Item.IsHeroic
            //        ? requestData.ClientSession.Character.HeroLevel : requestData.ClientSession.Character.Level))
            //    || ((itemInstance.ItemInstance.Item.Sex != 0) &&
            //        (((itemInstance.ItemInstance.Item.Sex >> (byte)requestData.ClientSession.Character.Gender) & 1) !=
            //            1))
            //    || ((itemInstance.ItemInstance.Item.Class != 0) &&
            //        (((itemInstance.ItemInstance.Item.Class >> (byte)requestData.ClientSession.Character.Class) & 1) !=
            //            1)))
            //{
            //    requestData.ClientSession.SendPacket(
            //        requestData.ClientSession.Character.GenerateSay(
            //            requestData.ClientSession.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT),
            //            SayColorType.Yellow));
            //    return;
            //}
        }

        [TestMethod]
        public void Test_BadFairy()
        {
            Assert.Fail();
            //if (requestData.ClientSession.Character.UseSp &&
            //    (itemInstance.ItemInstance.Item.EquipmentSlot == EquipmentType.Fairy))
            //{
            //    var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
            //        (byte)EquipmentType.Sp, NoscorePocketType.Wear);

            //    if ((sp != null) && (sp.ItemInstance.Item.Element != 0) &&
            //        (itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.Element) &&
            //        (itemInstance.ItemInstance.Item.Element != sp.ItemInstance.Item.SecondaryElement))
            //    {
            //        requestData.ClientSession.SendPacket(new MsgPacket
            //        {
            //            Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
            //                requestData.ClientSession.Account.Language)
            //        });
            //        return;
            //    }
            //}
        }

        [TestMethod]
        public void Test_SpLoading()
        {
            Assert.Fail();
            //var timeSpanSinceLastSpUsage =
            //    (SystemTime.Now() - requestData.ClientSession.Character.LastSp).TotalSeconds;
            //var sp = requestData.ClientSession.Character.Inventory.LoadBySlotAndType(
            //    (byte)EquipmentType.Sp, NoscorePocketType.Wear);
            //if ((timeSpanSinceLastSpUsage < requestData.ClientSession.Character.SpCooldown) && (sp != null))
            //{
            //    requestData.ClientSession.SendPacket(new MsgPacket
            //    {
            //        Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING,
            //                requestData.ClientSession.Account.Language),
            //            requestData.ClientSession.Character.SpCooldown - (int)Math.Round(timeSpanSinceLastSpUsage))
            //    });
            //    return;
            //}
        }

        [TestMethod]
        public void Test_UseSp()
        {
            Assert.Fail();
            //if (requestData.ClientSession.Character.UseSp)
            //{
            //    requestData.ClientSession.SendPacket(
            //        requestData.ClientSession.Character.GenerateSay(
            //            requestData.ClientSession.GetMessageFromKey(LanguageKey.SP_BLOCKED),
            //            SayColorType.Yellow));
            //    return;
            //}
        }

        [TestMethod]
        public void Test_UseDestroyedSp()
        {
            Assert.Fail();
            //requestData.ClientSession.SendPacket(new MsgPacket
            //{
            //    Message = Language.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
            //        requestData.ClientSession.Account.Language)
            //});
            //return;
        }

        public void Test_Use_BadJobLevel()
        {
            Assert.Fail();
            //if (requestData.ClientSession.Character.JobLevel < itemInstance.ItemInstance.Item.LevelJobMinimum)
            //{
            //    requestData.ClientSession.SendPacket(
            //        requestData.ClientSession.Character.GenerateSay(
            //            requestData.ClientSession.GetMessageFromKey(LanguageKey.LOW_JOB_LVL),
            //            SayColorType.Yellow));
            //    return;
            //}
        }

        public void Test_Use_SP()
        {
            Assert.Fail();
        }

        public void Test_Use_Fairy()
        {
            Assert.Fail();
        }

        public void Test_Use_Amulet()
        {
            Assert.Fail();
        }

        public void Test_Binding()
        {
            Assert.Fail();
        }
        public void Test_ItemValidTime()
        {
            Assert.Fail();
        }
    }
}