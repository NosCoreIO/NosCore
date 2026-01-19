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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Handlers;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class WearPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IItemGenerationService Item = null!;
        private ClientSession Session = null!;
        private WearPacketHandler WearPacketHandler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Item = TestHelpers.Instance.GenerateItemProvider();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            WearPacketHandler = new WearPacketHandler();
        }

        [DataTestMethod]
        [DataRow((int)EquipmentType.MainWeapon)]
        [DataRow((int)EquipmentType.Armor)]
        [DataRow((int)EquipmentType.Hat)]
        [DataRow((int)EquipmentType.Gloves)]
        [DataRow((int)EquipmentType.Boots)]
        [DataRow((int)EquipmentType.SecondaryWeapon)]
        [DataRow((int)EquipmentType.Necklace)]
        [DataRow((int)EquipmentType.Ring)]
        [DataRow((int)EquipmentType.Bracelet)]
        [DataRow((int)EquipmentType.Mask)]
        [DataRow((int)EquipmentType.Fairy)]
        [DataRow((int)EquipmentType.Amulet)]
        [DataRow((int)EquipmentType.Sp)]
        [DataRow((int)EquipmentType.CostumeSuit)]
        [DataRow((int)EquipmentType.CostumeHat)]
        [DataRow((int)EquipmentType.WeaponSkin)]
        [DataRow((int)EquipmentType.WingSkin)]
        public async Task WearingItemShouldPutInCorrectSlot(int typeInt)
        {
            var type = (EquipmentType)typeInt;
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = type, Class = 31 }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
            Assert.IsTrue(Session.Character.InventoryService.All(s =>
                s.Value.Slot == (short)type && s.Value.Type == NoscorePocketType.Wear));
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Adventurer)]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Swordsman)]
        public async Task WearingClassRestrictedItemShouldFailForWrongClass(int characterClassInt)
        {
            var classToTest = (CharacterClassType)characterClassInt;
            Session.Character.Class = classToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Class = (byte)(31 - Math.Pow(2, (byte)classToTest))
                }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
            Assert.IsTrue(Session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == Session.Character.CharacterId && packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.CanNotWearThat);
        }

        [DataTestMethod]
        [DataRow((int)GenderType.Female)]
        [DataRow((int)GenderType.Male)]
        public async Task WearingGenderRestrictedItemShouldFailForWrongGender(int genderInt)
        {
            var genderToTest = (GenderType)genderInt;
            Session.Character.Gender = genderToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Sex = (byte)(3 - Math.Pow(2, (byte)genderToTest))
                }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
            Assert.IsTrue(Session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player && packet?.VisualId == Session.Character.CharacterId && packet?.Type == SayColorType.Yellow && packet?.Message == Game18NConstString.CanNotWearThat);
        }

        [TestMethod]
        public async Task WearingItemWithLowJobLevelShouldFail()
        {
            await new Spec("Wearing item with low job level should fail")
                .Given(CharacterHasLowJobLevel)
                .And(CharacterHasItemRequiringJobLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldRemainInEquipment)
                .And(ShouldReceiveDifferentClassMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingItemWithSufficientJobLevelShouldSucceed()
        {
            await new Spec("Wearing item with sufficient job level should succeed")
                .Given(CharacterHasSufficientJobLevel)
                .And(CharacterHasItemRequiringJobLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingItemWithLowLevelShouldFail()
        {
            await new Spec("Wearing item with low level should fail")
                .Given(CharacterHasLowLevel)
                .And(CharacterHasItemRequiringLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldRemainInEquipment)
                .And(ShouldReceiveCantWearMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingItemWithSufficientLevelShouldSucceed()
        {
            await new Spec("Wearing item with sufficient level should succeed")
                .Given(CharacterHasSufficientLevel)
                .And(CharacterHasItemRequiringLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingHeroicItemWithLowHeroLevelShouldFail()
        {
            await new Spec("Wearing heroic item with low hero level should fail")
                .Given(CharacterHasLowHeroLevel)
                .And(CharacterHasHeroicItemRequiringLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldRemainInEquipment)
                .And(ShouldReceiveCantWearMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingHeroicItemWithSufficientHeroLevelShouldSucceed()
        {
            await new Spec("Wearing heroic item with sufficient hero level should succeed")
                .Given(CharacterHasSufficientHeroLevel)
                .And(CharacterHasHeroicItemRequiringLevel_, 3)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingDestroyedSpShouldFail()
        {
            await new Spec("Wearing destroyed sp should fail")
                .Given(CharacterHasDestroyedSp)
                .WhenAsync(WearingItemAsync)
                .Then(ItemShouldRemainInEquipment)
                .And(ShouldReceiveSoulDestroyedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingSpWhileTransformedShouldFail()
        {
            await new Spec("Wearing sp while transformed should fail")
                .Given(CharacterHasTwoSpItems)
                .AndAsync(CharacterWearsFirstSpAndTransformsAsync)
                .WhenAsync(WearingSecondItemAsync)
                .Then(SecondSpShouldRemainInEquipment)
                .And(ShouldReceiveSpTransformedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingSpDuringCooldownShouldFail()
        {
            await new Spec("Wearing sp during cooldown should fail")
                .Given(CharacterHasTwoSpItems)
                .AndAsync(CharacterWearsFirstSpWithCooldownAsync)
                .WhenAsync(WearingSecondItemAsync)
                .Then(SecondSpShouldRemainInEquipment)
                .And(ShouldReceiveCooldownMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingFairyWithWrongElementWhileSpInUseShouldFail()
        {
            await new Spec("Wearing fairy with wrong element while sp in use should fail")
                .Given(CharacterHasSpWithFireElementAndLightFairy)
                .AndAsync(CharacterTransformsWithSpAsync)
                .WhenAsync(WearingItemAsync)
                .Then(FairyShouldRemainInEquipment)
                .And(ShouldReceiveDifferentElementMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingFairyWithMatchingPrimaryElementShouldSucceed()
        {
            await new Spec("Wearing fairy with matching primary element should succeed")
                .Given(CharacterHasSpWithFireElementAndFireFairy)
                .AndAsync(CharacterTransformsWithSpAsync)
                .WhenAsync(WearingItemAsync)
                .Then(FairyShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingFairyWithMatchingSecondaryElementShouldSucceed()
        {
            await new Spec("Wearing fairy with matching secondary element should succeed")
                .Given(CharacterHasSpWithFireAndWaterElementsAndWaterFairy)
                .AndAsync(CharacterTransformsWithSpAsync)
                .WhenAsync(WearingItemAsync)
                .Then(FairyShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WearingItemRequiringBindingShouldAskForConfirmation()
        {
            await new Spec("Wearing item requiring binding should ask for confirmation")
                .Given(CharacterHasItemRequiringBinding)
                .WhenAsync(WearingItemAsync)
                .Then(ShouldReceiveBindingQuestion)
                .And(ItemShouldRemainInEquipment)
                .ExecuteAsync();
        }

        private void CharacterHasLowJobLevel()
        {
            Session.Character.JobLevel = 1;
        }

        private void CharacterHasSufficientJobLevel()
        {
            Session.Character.JobLevel = 3;
        }

        private void CharacterHasItemRequiringJobLevel_(int value)
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon, LevelJobMinimum = (byte)value }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
        }

        private void CharacterHasLowLevel()
        {
            Session.Character.Level = 1;
        }

        private void CharacterHasSufficientLevel()
        {
            Session.Character.Level = 3;
        }

        private void CharacterHasItemRequiringLevel_(int value)
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon, LevelMinimum = (byte)value }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
        }

        private void CharacterHasLowHeroLevel()
        {
            Session.Character.HeroLevel = 1;
        }

        private void CharacterHasSufficientHeroLevel()
        {
            Session.Character.HeroLevel = 3;
        }

        private void CharacterHasHeroicItemRequiringLevel_(int value)
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon, IsHeroic = true, LevelMinimum = (byte)value }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
        }

        private void CharacterHasDestroyedSp()
        {
            Session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Sp }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1, -2), Session.Character.CharacterId));
        }

        private void CharacterHasTwoSpItems()
        {
            Session.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Sp },
                new Item { Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(2, 1), Session.Character.CharacterId));
        }

        private async Task CharacterWearsFirstSpAndTransformsAsync()
        {
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
            Session.Character.UseSp = true;
        }

        private async Task CharacterWearsFirstSpWithCooldownAsync()
        {
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
            Session.Character.SpCooldown = 30;
            Session.Character.LastSp = TestHelpers.Instance.Clock.GetCurrentInstant();
        }

        private void CharacterHasSpWithFireElementAndLightFairy()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Light },
                new Item { Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = ElementType.Fire, SecondaryElement = ElementType.Water }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(2, 1), Session.Character.CharacterId));
        }

        private void CharacterHasSpWithFireElementAndFireFairy()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Fire },
                new Item { Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = ElementType.Fire, SecondaryElement = ElementType.Water }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(2, 1), Session.Character.CharacterId));
        }

        private void CharacterHasSpWithFireAndWaterElementsAndWaterFairy()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Water },
                new Item { Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp, Element = ElementType.Fire, SecondaryElement = ElementType.Water }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(2, 1), Session.Character.CharacterId));
        }

        private async Task CharacterTransformsWithSpAsync()
        {
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, Session);
            Session.Character.UseSp = true;
        }

        private void CharacterHasItemRequiringBinding()
        {
            var items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, RequireBinding = true }
            };
            Item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            Session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(Item.Create(1, 1), Session.Character.CharacterId));
        }

        private async Task WearingItemAsync()
        {
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, Session);
        }

        private async Task WearingSecondItemAsync()
        {
            await WearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, Session);
        }

        private void ItemShouldRemainInEquipment()
        {
            Assert.IsTrue(Session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
        }

        private void ItemShouldBeWorn()
        {
            Assert.IsTrue(Session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Wear));
        }

        private void SecondSpShouldRemainInEquipment()
        {
            Assert.IsTrue(Session.Character.InventoryService.Any(s =>
                s.Value.ItemInstance.ItemVNum == 2 && s.Value.Type == NoscorePocketType.Equipment));
        }

        private void FairyShouldRemainInEquipment()
        {
            Assert.IsTrue(Session.Character.InventoryService.Any(s =>
                s.Value.ItemInstance.ItemVNum == 1 && s.Value.Type == NoscorePocketType.Equipment));
        }

        private void FairyShouldBeWorn()
        {
            Assert.IsTrue(Session.Character.InventoryService.Any(s =>
                s.Value.ItemInstance.ItemVNum == 1 && s.Value.Type == NoscorePocketType.Wear));
        }

        private void ShouldReceiveCantWearMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.CanNotWearThat);
        }

        private void ShouldReceiveDifferentClassMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.CanNotBeWornDifferentClass);
        }

        private void ShouldReceiveSoulDestroyedMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default && packet?.Message == Game18NConstString.CantUseBecauseSoulDestroyed);
        }

        private void ShouldReceiveSpTransformedMessage()
        {
            var packet = (SayiPacket?)Session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.IsTrue(packet?.VisualType == VisualType.Player &&
                packet?.VisualId == Session.Character.CharacterId &&
                packet?.Type == SayColorType.Yellow &&
                packet?.Message == Game18NConstString.SpecialistCardsCannotBeTradedWhileTransformed);
        }

        private void ShouldReceiveCooldownMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Type == MessageType.Default &&
                packet?.Message == Game18NConstString.CantTrasformWithSideEffect &&
                packet?.ArgumentType == 4 &&
                (int?)packet?.Game18NArguments[0] == 30);
        }

        private void ShouldReceiveDifferentElementMessage()
        {
            var packet = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.SpecialistAndFairyDifferentElement);
        }

        private void ShouldReceiveBindingQuestion()
        {
            var packet = (QnaPacket?)Session.LastPackets.FirstOrDefault(s => s is QnaPacket);
            Assert.IsTrue(packet?.YesPacket is UseItemPacket yespacket &&
                yespacket.Slot == 0 &&
                yespacket.Type == PocketType.Equipment &&
                packet.Question == Session.GetMessageFromKey(LanguageKey.ASK_BIND));
        }
    }
}
