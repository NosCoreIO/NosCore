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
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Specialists;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Tests.Services.ItemGenerationService.Handlers
{
    [TestClass]
    public class WearEventHandlerTests : UseItemEventHandlerTestsBase
    {
        private GameObject.Services.ItemGenerationService.ItemGenerationService? _itemProvider;
        private Mock<ILogger>? _logger;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _logger = new Mock<ILogger>();
            TestHelpers.Instance.WorldConfiguration.Value.BackpackSize = 40;
            Session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            Handler = new WearEventHandler(_logger.Object, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer);
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
                    EquipmentSlot = EquipmentType.Sp,
                    Element = ElementType.Fire
                },
                new Item
                {
                    VNum = 5,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Weapon,
                    RequireBinding = true,
                    Sex = 2
                },
                new Item
                {
                    VNum = 6,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Specialist,
                    EquipmentSlot = EquipmentType.Sp,
                    LevelJobMinimum = 2,
                    Element = ElementType.Fire
                },
                new Item
                {
                    VNum = 7,
                    Type = NoscorePocketType.Equipment,
                    ItemType = ItemType.Jewelery,
                    ItemValidTime = 50,
                    EquipmentSlot = EquipmentType.Amulet
                }
            };
            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), _logger.Object);
        }

        [TestMethod]
        public async Task Test_Can_Not_Use_WearEvent_In_ShopAsync()
        {
            Session!.Character.InShop = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session.Character.CharacterId);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            _logger!.Verify(s => s.Error(TestHelpers.Instance.LogLanguageLocalizer[LogLanguageKey.CANT_USE_ITEM_IN_SHOP]), Times.Exactly(1));
        }

        [TestMethod]
        public async Task Test_BoundCharacter_QuestionAsync()
        {
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (QnaPacket?)Session.LastPackets.FirstOrDefault(s => s is QnaPacket);
            Assert.AreEqual(Session.GetMessageFromKey(LanguageKey.ASK_BIND), lastpacket?.Question);
        }

        [TestMethod]
        public async Task Test_BoundCharacterAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(1), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            Assert.AreEqual(Session.Character.CharacterId, itemInstance.ItemInstance?.BoundCharacterId);
        }

        [TestMethod]
        public async Task Test_BadEquipmentAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(5), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(Session.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT), lastpacket?.Message);
        }

        [TestMethod]
        public async Task Test_BadFairyAsync()
        {
            UseItem.Mode = 1;
            Session!.Character.UseSp = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(2), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.SpecialistAndFairyDifferentElement, lastpacket?.Message);
        }

        [TestMethod]
        public async Task Test_SpLoadingAsync()
        {
            UseItem.Mode = 1;
            Session!.Character.LastSp = TestHelpers.Instance.Clock.GetCurrentInstant();
            Session.Character.SpCooldown = 300;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(4), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Type == MessageType.Default, lastpacket?.Message == Game18NConstString.CantTrasformWithSideEffect);
            Assert.AreEqual(lastpacket?.FirstArgument == 4, lastpacket?.SecondArgument == Session.Character.SpCooldown);
        }

        [TestMethod]
        public async Task Test_UseSpAsync()
        {
            UseItem.Mode = 1;
            Session!.Character.UseSp = true;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(4), Session.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            var sp = InventoryItemInstance.Create(_itemProvider.Create(4), Session.Character.CharacterId);
            Session.Character.InventoryService.AddItemToPocket(sp, NoscorePocketType.Wear);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(lastpacket?.VisualType == VisualType.Player, lastpacket?.VisualId == Session.Character.CharacterId);
            Assert.AreEqual(lastpacket?.Type == SayColorType.Yellow, lastpacket?.Message == Game18NConstString.SpecialistCardsCannotBeTradedWhileTransformed);
        }

        [TestMethod]
        public async Task Test_UseDestroyedSpAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(4), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            itemInstance.ItemInstance!.Rare = -2;
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Type == MessageType.Default, lastpacket?.Message == Game18NConstString.CantUseBecauseSoulDestroyed);
        }

        [TestMethod]
        public async Task Test_Use_BadJobLevelAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(6), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SayPacket?)Session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.LOW_JOB_LVL,
                    Session.Account.Language), lastpacket?.Message);
        }

        [TestMethod]
        public async Task Test_Use_SPAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(4), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (SpPacket?)Session.LastPackets.FirstOrDefault(s => s is SpPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public async Task Test_Use_FairyAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(2), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (PairyPacket?)Session.Character.MapInstance.LastPackets.FirstOrDefault(s => s is PairyPacket);
            Assert.IsNotNull(lastpacket);
        }

        [TestMethod]
        public async Task Test_Use_AmuletAsync()
        {
            UseItem.Mode = 1;
            var itemInstance = InventoryItemInstance.Create(_itemProvider!.Create(7), Session!.Character.CharacterId);
            Session.Character.InventoryService!.AddItemToPocket(itemInstance);
            await ExecuteInventoryItemInstanceEventHandlerAsync(itemInstance).ConfigureAwait(false);
            var lastpacket = (EffectPacket?)Session.LastPackets.FirstOrDefault(s => s is EffectPacket);
            Assert.IsNotNull(lastpacket);
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant().Plus(Duration.FromSeconds(itemInstance.ItemInstance!.Item!.ItemValidTime)), itemInstance.ItemInstance.ItemDeleteTime);
        }
    }
}