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
using NosCore.GameObject;
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

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class WearPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IItemGenerationService? _item;

        private ClientSession? _session;
        private WearPacketHandler? _wearPacketHandler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _wearPacketHandler = new WearPacketHandler();
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
        public async Task Test_Wear_Put_Item_CorrectSlotAsync(int typeInt)
        {
            var type = (EquipmentType)typeInt;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = type,
                    Class = 31 //sum of all 2^class
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.All(s =>
                (s.Value.Slot == (short)type) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [DataTestMethod]
        [DataRow((int)CharacterClassType.Adventurer)]
        [DataRow((int)CharacterClassType.Archer)]
        [DataRow((int)CharacterClassType.Mage)]
        [DataRow((int)CharacterClassType.MartialArtist)]
        [DataRow((int)CharacterClassType.Swordsman)]
        public async Task Test_Wear_Put_Item_BadClassAsync(int characterClassInt)
        {
            var classToTest = (CharacterClassType)characterClassInt;
            _session!.Character.Class = classToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Class = (byte) (31 - Math.Pow(2, (byte) classToTest))
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.All(s => s.Value.Type);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(VisualType.Player, packet?.VisualType);
            Assert.AreEqual(_session.Character.CharacterId, packet?.VisualId);
            Assert.AreEqual(SayColorType.Yellow, packet?.Type);
            Assert.AreEqual(Game18NConstString.CanNotWearThat, packet?.Message);

            foreach (var validClass in Enum.GetValues(typeof(CharacterClassType)).OfType<CharacterClassType>()
                .Where(s => s != classToTest).ToList())
            {
                _session.Character.Class = validClass;
                var item = _session.Character.InventoryService.First();
                await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
                Assert.AreEqual(NoscorePocketType.Wear, item.Value.Type);
                item.Value.Type = NoscorePocketType.Equipment;
                item.Value.Slot = 0;
            }
        }


        [DataTestMethod]
        [DataRow((int)GenderType.Female)]
        [DataRow((int)GenderType.Male)]
        public async Task Test_Wear_Put_Item_BadGenderAsync(int genderToTestInt)
        {
            var genderToTest = (GenderType)genderToTestInt;
            _session!.Character.Gender = genderToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Sex = (byte) (3 - Math.Pow(2, (byte) genderToTest))
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.All(s => s.Value.Type);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(VisualType.Player, packet?.VisualType);
            Assert.AreEqual(_session.Character.CharacterId, packet?.VisualId);
            Assert.AreEqual(SayColorType.Yellow, packet?.Type);
            Assert.AreEqual(Game18NConstString.CanNotWearThat, packet?.Message);

            foreach (var validClass in Enum.GetValues(typeof(GenderType)).OfType<GenderType>()
                .Where(s => s != genderToTest).ToList())
            {
                _session.Character.Gender = validClass;
                var item = _session.Character.InventoryService.First();
                await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
                Assert.AreEqual(NoscorePocketType.Wear, item.Value.Type);
                item.Value.Type = NoscorePocketType.Equipment;
                item.Value.Slot = 0;
            }
        }

        [TestMethod]
        public async Task Test_Wear_BadJobLevelAsync()
        {
            _session!.Character.JobLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelJobMinimum = 3
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.All(s => s.Value.Type);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.CanNotBeWornDifferentClass, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_GoodJobLevelAsync()
        {
            _session!.Character.JobLevel = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelJobMinimum = 3
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Wear), _session.Character.InventoryService.All(s => s.Value.Type);
        }

        [TestMethod]
        public async Task Test_Wear_BadLevelAsync()
        {
            _session!.Character.Level = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelMinimum = 3
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.All(s => s.Value.Type);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(VisualType.Player, packet?.VisualType);
            Assert.AreEqual(_session.Character.CharacterId, packet?.VisualId);
            Assert.AreEqual(SayColorType.Yellow, packet?.Type);
            Assert.AreEqual(Game18NConstString.CanNotWearThat, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_GoodLevelAsync()
        {
            _session!.Character.Level = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    LevelMinimum = 3
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Wear), _session.Character.InventoryService.All(s => s.Value.Type);
        }

        [TestMethod]
        public async Task Test_Wear_BadHeroLevelAsync()
        {
            _session!.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    IsHeroic = true,
                    LevelMinimum = 3
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.All(s => s.Value.Type);
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(VisualType.Player, packet?.VisualType);
            Assert.AreEqual(_session.Character.CharacterId, packet?.VisualId);
            Assert.AreEqual(SayColorType.Yellow, packet?.Type);
            Assert.AreEqual(Game18NConstString.CanNotWearThat, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_GoodHeroLevelAsync()
        {
            _session!.Character.HeroLevel = 3;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.MainWeapon,
                    IsHeroic = true,
                    LevelMinimum = 3
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.AreEqual(NoscorePocketType.Wear), _session.Character.InventoryService.All(s => s.Value.Type);
        }

        [TestMethod]
        public async Task Test_Wear_DestroyedSpAsync()
        {
            _session!.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1, -2),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);

            Assert.AreEqual(NoscorePocketType.Equipment), _session.Character.InventoryService.Any(s => s.Value.Type);
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(MessageType.Default, packet?.Type);
            Assert.AreEqual(Game18NConstString.CantUseBecauseSoulDestroyed, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_SpInUseAsync()
        {
            _session!.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 2,
                    EquipmentSlot = EquipmentType.Sp
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            _session.Character.UseSp = true;
            await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 2) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (SayiPacket?)_session.LastPackets.FirstOrDefault(s => s is SayiPacket);
            Assert.AreEqual(VisualType.Player, packet?.VisualType);
            Assert.AreEqual(_session.Character.CharacterId, packet?.VisualId);
            Assert.AreEqual(SayColorType.Yellow, packet?.Type);
            Assert.AreEqual(Game18NConstString.SpecialistCardsCannotBeTradedWhileTransformed, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_SpInLoadingAsync()
        {
            _session!.Character.HeroLevel = 1;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1,
                    EquipmentSlot = EquipmentType.Sp
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 2,
                    EquipmentSlot = EquipmentType.Sp
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            _session.Character.SpCooldown = 30;
            _session.Character.LastSp = TestHelpers.Instance.Clock.GetCurrentInstant();
            await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 2) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(MessageType.Default, packet?.Type);
            Assert.AreEqual(Game18NConstString.CantTrasformWithSideEffect, packet?.Message);
            Assert.AreEqual(4, packet?.ArgumentType);
            Assert.AreEqual(30, int?)packet?.Game18NArguments[0]);
        }


        [TestMethod]
        public async Task Test_Wear_WearFairy_SpUseBadElementAsync()
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Light
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp,
                    Element = ElementType.Fire,
                    SecondaryElement = ElementType.Water
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            _session.Character.UseSp = true;
            await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (MsgiPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(Game18NConstString.SpecialistAndFairyDifferentElement, packet?.Message);
        }

        [TestMethod]
        public async Task Test_Wear_WearFairy_SpUseGoodElementAsync()
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Fire
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp,
                    Element = ElementType.Fire,
                    SecondaryElement = ElementType.Water
                }
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            _session.Character.UseSp = true;
            await _wearPacketHandler.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(
                _session.Character.InventoryService.Any(s =>
                    (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [TestMethod]
        public async Task Test_Wear_WearFairy_SpUseGoodSecondElementAsync()
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.Fairy,
                    Element = ElementType.Water
                },
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Sp,
                    Element = ElementType.Fire,
                    SecondaryElement = ElementType.Water
                }
            };
            _item = new ItemGenerationService(items, new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            _session.Character.UseSp = true;
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);
            Assert.IsTrue(
                _session.Character.InventoryService.Any(s =>
                    (s.Value.ItemInstance?.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [TestMethod]
        public async Task Test_Binding_RequiredAsync()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, RequireBinding = true}
            };
            _item = new ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger, TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer)}), Logger, TestHelpers.Instance.LogLanguageLocalizer);

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.ExecuteAsync(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session).ConfigureAwait(false);

            var packet = (QnaPacket?)_session.LastPackets.FirstOrDefault(s => s is QnaPacket);
            Assert.IsTrue(packet?.YesPacket is UseItemPacket yespacket
                && (yespacket.Slot == 0)
                && (yespacket.Type == PocketType.Equipment)
                && (packet.Question == _session.GetMessageFromKey(LanguageKey.ASK_BIND)));
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Equipment)));
        }
    }
}