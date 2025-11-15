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
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Serilog;

namespace NosCore.GameObject.Tests.Services.InventoryService
{
    [TestClass]
    public class InventoryServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? _itemProvider;

        private IInventoryService? Inventory { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var items = new List<ItemDto>
            {
                new Item {Type = NoscorePocketType.Main, VNum = 1012},
                new Item {Type = NoscorePocketType.Main, VNum = 1013},
                new Item {Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Weapon},
                new Item {Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist},
                new Item {Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion}
            };
            _itemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()), Logger, TestHelpers.Instance.LogLanguageLocalizer);
            Inventory = new GameObject.Services.InventoryService.InventoryService(items, Options.Create(new WorldConfiguration { BackpackSize = 3, MaxItemAmount = 999 }),
                Logger);
        }

        [TestMethod]
        public void CreateItem()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012), 0))!.First();
            Assert.AreEqual(1, item.ItemInstance?.Amount);
            Assert.AreEqual(1012, item.ItemInstance.ItemVNum);
            Assert.AreEqual(NoscorePocketType.Main, item.Type);
        }

        [TestMethod]
        public void CreateItemAndStackIt()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012), 0))!.First();
            var item = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0))!.First();
            Assert.AreEqual(2, item.ItemInstance?.Amount);
            Assert.AreEqual(1012, item.ItemInstance.ItemVNum);
        }

        [TestMethod]
        public void CreateItemWhenSlotMax()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!.First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012), 0));
            Assert.AreEqual(1, items![0]!.ItemInstance?.Amount);
        }

        [TestMethod]
        public void CreateItemWhenSlotFilled()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0))!.First();
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 29), 0));
            Assert.AreEqual(999, items![0]!.ItemInstance?.Amount);
            Assert.AreEqual(20, items.Last()!.ItemInstance?.Amount);
        }

        [TestMethod]
        public void CreateItemAndFillMultiSlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0),
                NoscorePocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 27), 0))!;
            Assert.AreEqual(999, items.All(item => item.ItemInstance!.Amount);
            Assert.AreEqual(3, items?.Count);
        }

        [TestMethod]
        public void CreateMoreItemThanInventoryPlace()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0),
                NoscorePocketType.Main, 0);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 1);
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0),
                NoscorePocketType.Main, 2);
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 99), 0));
            Assert.AreEqual(990, Inventory.Values.All(item => item.ItemInstance?.Amount);
            Assert.AreEqual(0, items?.Count);
        }

        [TestMethod]
        public void CreateStackOnASpecificItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013), 0));
            Assert.AreEqual(1).ItemInstance!.Amount == 991, Inventory.Values.First(item => item.Slot);
        }

        [TestMethod]
        public void CreateDoesntStackOnWrongItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1013, 19), 0));
            Assert.AreEqual(0).ItemInstance?.Amount == 990, Inventory.Values.First(item => item.Slot);
        }

        [TestMethod]
        public void LoadItemOnAnNotEmptySlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(0, NoscorePocketType.Main);
            Assert.AreEqual(1012, item!.ItemInstance?.ItemVNum);
            Assert.AreEqual(990, item.ItemInstance?.Amount);
        }

        [TestMethod]
        public void LoadAnNonExistingItem()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var item = Inventory.LoadBySlotAndType(1, NoscorePocketType.Main);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void DeleteFromTypeAndSlot()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.AreEqual(2, Inventory.Count);
            var item = Inventory.DeleteFromTypeAndSlot(NoscorePocketType.Main, 0);
            Assert.IsNull(item);
            Assert.AreEqual(1, Inventory.Count);
        }

        [TestMethod]
        public void Delete()
        {
            Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 990), 0));
            var items = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 990), 0));
            Assert.AreEqual(2, Inventory.Count);
            var item = Inventory.DeleteById(items![0].ItemInstanceId);
            Assert.IsNull(item);
            Assert.AreEqual(1, Inventory.Count);
        }

        [TestMethod]
        public void MoveFullSlot()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 999, 1, out var originItem, out var destinationItem);
            Assert.AreEqual(null, originItem);
            Assert.AreEqual(999, destinationItem?.ItemInstance?.Amount);
            Assert.AreEqual(1, destinationItem.Slot);
        }

        [TestMethod]
        public void MoveHalfSlot()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out var originItem, out var destinationItem);
            Assert.AreEqual(500, originItem?.ItemInstance?.Amount);
            Assert.AreEqual(0, originItem.Slot);
            Assert.AreEqual(499, destinationItem?.ItemInstance?.Amount);
            Assert.AreEqual(1, destinationItem.Slot);
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThem()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.TryMoveItem(item.Type, item.Slot, 499, 1, out _, out _);
            Inventory.TryMoveItem(item.Type, 0, 500, 1, out var originItem, out var destinationItem);
            Assert.AreEqual(null, originItem);
            Assert.AreEqual(999, destinationItem?.ItemInstance?.Amount);
            Assert.AreEqual(1, destinationItem.Slot);
        }

        [TestMethod]
        public void MoveHalfSlotAndMergeThemWithOverflow()
        {
            var item = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1012, 999), 0))!
                .First();
            Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1012, 500), 0));
            Inventory.TryMoveItem(item.Type, item.Slot, 600, 1, out var originItem, out var destinationItem);
            Assert.AreEqual(500, originItem?.ItemInstance?.Amount);
            Assert.AreEqual(0, originItem.Slot);
            Assert.AreEqual(999, destinationItem?.ItemInstance?.Amount);
            Assert.AreEqual(1, destinationItem.Slot);
        }

        //TODO RemoveItemAmountFromInventory

        //TODO EnoughPlace

        [TestMethod]
        public void MoveFashionToFashionPocket()
        {
            var fashion = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(924), 0))!.First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, NoscorePocketType.Costume);
            Assert.AreEqual(NoscorePocketType.Costume, item?.Type);
        }

        [TestMethod]
        public void MoveFashionToSpecialistPocket()
        {
            var fashion = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(924), 0))!.First();
            var item = Inventory.MoveInPocket(fashion.Slot, fashion.Type, NoscorePocketType.Specialist);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToFashionPocket()
        {
            var specialist = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(912), 0))!
                .First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, NoscorePocketType.Costume);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void MoveSpecialistToSpecialistPocket()
        {
            var specialist = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(912), 0))!
                .First();
            var item = Inventory.MoveInPocket(specialist.Slot, specialist.Type, NoscorePocketType.Specialist);
            Assert.AreEqual(NoscorePocketType.Specialist, item?.Type);
        }

        [TestMethod]
        public void MoveWeaponToPocket()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear);
            Assert.AreEqual(NoscorePocketType.Wear, item?.Type);
        }

        [TestMethod]
        public void SwapWithEmpty()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
            Assert.AreEqual(NoscorePocketType.Wear, item?.Type);
            Assert.AreEqual(null, Inventory.LoadBySlotAndType(0, NoscorePocketType.Equipment));
        }

        [TestMethod]
        public void SwapWithNotEmpty()
        {
            var weapon = Inventory!.AddItemToPocket(InventoryItemInstance.Create(_itemProvider!.Create(2), 0))!.First();
            var weapon2 = Inventory.AddItemToPocket(InventoryItemInstance.Create(_itemProvider.Create(1), 0))!.First();
            var item = Inventory.MoveInPocket(weapon.Slot, weapon.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);
            var item2 = Inventory.MoveInPocket(weapon2.Slot, weapon2.Type, NoscorePocketType.Wear,
                (short)EquipmentType.MainWeapon, true);

            Assert.AreEqual(NoscorePocketType.Equipment, item?.Type);
            Assert.AreEqual(1, item.Slot);
            Assert.AreEqual(NoscorePocketType.Wear, item2?.Type);
            Assert.AreEqual((short)EquipmentType.MainWeapon, item2.Slot);
        }
    }
}