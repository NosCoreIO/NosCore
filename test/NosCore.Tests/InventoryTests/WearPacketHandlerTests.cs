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
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Handlers;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.PacketHandlers.Inventory;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.InventoryTests
{
    [TestClass]
    public class WearPacketHandlerTests
    {
        private static readonly ILogger Logger = Core.I18N.Logger.GetLoggerConfiguration().CreateLogger();
        private IItemProvider? _item;

        private ClientSession? _session;
        private WearPacketHandler? _wearPacketHandler;

        [TestCleanup]
        public void Cleanup()
        {
            SystemTime.Freeze(SystemTime.Now());
        }

        [TestInitialize]
        public void Setup()
        {
            SystemTime.Freeze();
            TestHelpers.Reset();
            _item = TestHelpers.Instance.GenerateItemProvider();
            _session = TestHelpers.Instance.GenerateSession();
            _wearPacketHandler = new WearPacketHandler();
        }

        [DataTestMethod]
        [DataRow(EquipmentType.MainWeapon)]
        [DataRow(EquipmentType.Armor)]
        [DataRow(EquipmentType.Hat)]
        [DataRow(EquipmentType.Gloves)]
        [DataRow(EquipmentType.Boots)]
        [DataRow(EquipmentType.SecondaryWeapon)]
        [DataRow(EquipmentType.Necklace)]
        [DataRow(EquipmentType.Ring)]
        [DataRow(EquipmentType.Bracelet)]
        [DataRow(EquipmentType.Mask)]
        [DataRow(EquipmentType.Fairy)]
        [DataRow(EquipmentType.Amulet)]
        [DataRow(EquipmentType.Sp)]
        [DataRow(EquipmentType.CostumeSuit)]
        [DataRow(EquipmentType.CostumeHat)]
        [DataRow(EquipmentType.WeaponSkin)]
        [DataRow(EquipmentType.WingSkin)]
        public void Test_Wear_Put_Item_CorrectSlot(EquipmentType type)
        {
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = type,
                    Class = 31 //sum of all 2^class
                }
            };
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s =>
                (s.Value.Slot == (short)type) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Adventurer)]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.MartialArtist)]
        [DataRow(CharacterClassType.Swordman)]
        public void Test_Wear_Put_Item_BadClass(CharacterClassType classToTest)
        {
            _session!.Character.Class = classToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Class = (byte) (31 - Math.Pow(2, (byte) classToTest))
                }
            };
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));

            foreach (var validClass in Enum.GetValues(typeof(CharacterClassType)).OfType<CharacterClassType>()
                .Where(s => s != classToTest).ToList())
            {
                _session.Character.Class = validClass;
                var item = _session.Character.InventoryService.First();
                _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
                Assert.IsTrue(item.Value.Type == NoscorePocketType.Wear);
                item.Value.Type = NoscorePocketType.Equipment;
                item.Value.Slot = 0;
            }
        }


        [DataTestMethod]
        [DataRow(GenderType.Female)]
        [DataRow(GenderType.Male)]
        public void Test_Wear_Put_Item_BadGender(GenderType genderToTest)
        {
            _session!.Character.Gender = genderToTest;
            var items = new List<ItemDto>
            {
                new Item
                {
                    Type = NoscorePocketType.Equipment, VNum = 1, EquipmentSlot = EquipmentType.MainWeapon,
                    Sex = (byte) (3 - Math.Pow(2, (byte) genderToTest))
                }
            };
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));

            foreach (var validClass in Enum.GetValues(typeof(GenderType)).OfType<GenderType>()
                .Where(s => s != genderToTest).ToList())
            {
                _session.Character.Gender = validClass;
                var item = _session.Character.InventoryService.First();
                _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
                Assert.IsTrue(item.Value.Type == NoscorePocketType.Wear);
                item.Value.Type = NoscorePocketType.Equipment;
                item.Value.Slot = 0;
            }
        }

        [TestMethod]
        public void Test_Wear_BadJobLevel()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.LOW_JOB_LVL,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));
        }

        [TestMethod]
        public void Test_Wear_GoodJobLevel()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_BadLevel()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));
        }

        [TestMethod]
        public void Test_Wear_GoodLevel()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Wear));
        }

        [TestMethod]
        public void Test_Wear_BadHeroLevel()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_EQUIPMENT,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.All(s => s.Value.Type == NoscorePocketType.Wear));
        }

        [TestMethod]
        public async Task Test_Wear_DestroyedSp()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1, -2),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);

            Assert.IsTrue(_session.Character.InventoryService.Any(s => s.Value.Type == NoscorePocketType.Equipment));
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_EQUIP_DESTROYED_SP,
                _session.Account.Language));
        }

        [TestMethod]
        public void Test_Wear_SpInUse()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 2) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (SayPacket?)_session.LastPackets.FirstOrDefault(s => s is SayPacket);
            Assert.IsTrue((packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_BLOCKED,
                _session.Account.Language)) && (packet?.Type == SayColorType.Yellow));
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});
            _session.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            _session.Character.SpCooldown = 30;
            await _wearPacketHandler.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 2) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message ==
                string.Format(GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_INLOADING, _session.Account.Language),
                    30));
        }


        [TestMethod]
        public void Test_Wear_WearFairy_SpUseBadElement()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(_session.Character.InventoryService.Any(s =>
                (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Equipment)));
            var packet = (MsgPacket?)_session.LastPackets.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(packet?.Message == GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_FAIRY,
                _session.Account.Language));
        }

        [TestMethod]
        public async Task Test_Wear_WearFairy_SpUseGoodElement()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            await _wearPacketHandler.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(
                _session.Character.InventoryService.Any(s =>
                    (s.Value.ItemInstance!.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [TestMethod]
        public async Task Test_Wear_WearFairy_SpUseGoodSecondElement()
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
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            _session.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(_item.Create(2, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 1, Type = PocketType.Equipment }, _session);
            _session.Character.UseSp = true;
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);
            Assert.IsTrue(
                _session.Character.InventoryService.Any(s =>
                    (s.Value.ItemInstance?.ItemVNum == 1) && (s.Value.Type == NoscorePocketType.Wear)));
        }

        [TestMethod]
        public async Task Test_Binding_Required()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, RequireBinding = true}
            };
            _item = new ItemProvider(items,
                new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>
                    {new WearEventHandler(Logger)});

            _session!.Character.InventoryService!.AddItemToPocket(InventoryItemInstance.Create(_item.Create(1, 1),
                _session.Character.CharacterId));
            await _wearPacketHandler!.Execute(new WearPacket { InventorySlot = 0, Type = PocketType.Equipment }, _session);

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