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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ItemGenerationService
{
    [TestClass]
    public class ItemGenerationServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private GameObject.Services.ItemGenerationService.ItemGenerationService? ItemProvider;
        private List<ItemDto>? Items;

        [TestInitialize]
        public void Setup()
        {
            Items = new List<ItemDto>
            {
                new Item { Type = NoscorePocketType.Main, VNum = 1012 },
                new Item { Type = NoscorePocketType.Main, VNum = 1013 },
                new Item { Type = NoscorePocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon },
                new Item { Type = NoscorePocketType.Equipment, VNum = 2, ItemType = ItemType.Armor },
                new Item { Type = NoscorePocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist },
                new Item { Type = NoscorePocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion },
                new Item { Type = NoscorePocketType.Miniland, VNum = 3000, MinilandObjectPoint = 100 }
            };

            ItemProvider = new GameObject.Services.ItemGenerationService.ItemGenerationService(
                Items,
                new EventLoaderService<Item, Tuple<InventoryItemInstance, UseItemPacket>, IUseItemEventHandler>(
                    new List<IEventHandler<Item, Tuple<InventoryItemInstance, UseItemPacket>>>()),
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public void CreateItemShouldReturnItemInstance()
        {
            new Spec("Create item should return item instance")
                .When(CreatingBasicItem)
                .Then(ItemShouldBeBasicItemInstance)
                .Execute();
        }

        [TestMethod]
        public void CreateSpecialistItemShouldReturnSpecialistInstance()
        {
            new Spec("Create specialist item should return specialist instance")
                .When(CreatingSpecialistItem)
                .Then(ItemShouldBeSpecialistInstance)
                .Execute();
        }

        [TestMethod]
        public void CreateWearableItemShouldReturnWearableInstance()
        {
            new Spec("Create wearable item should return wearable instance")
                .When(CreatingWearableItem)
                .Then(ItemShouldBeWearableInstance)
                .Execute();
        }

        [TestMethod]
        public void GenerateWithInvalidVNumShouldThrow()
        {
            new Spec("Generate with invalid VNum should throw")
                .When(GeneratingWithInvalidVNum).Catch(out var exception)
                .Then(ShouldThrowNullReferenceException_, exception)
                .Execute();
        }

        private IItemInstance? CreatedItem;

        private void CreatingBasicItem()
        {
            CreatedItem = ItemProvider!.Create(1012);
        }

        private void ItemShouldBeBasicItemInstance()
        {
            Assert.IsNotNull(CreatedItem);
            Assert.IsInstanceOfType(CreatedItem, typeof(ItemInstance));
            Assert.AreEqual(1012, CreatedItem.ItemVNum);
            Assert.AreEqual(1, CreatedItem.Amount);
        }

        private void CreatingSpecialistItem()
        {
            CreatedItem = ItemProvider!.Create(912);
        }

        private void ItemShouldBeSpecialistInstance()
        {
            Assert.IsNotNull(CreatedItem);
            Assert.IsInstanceOfType(CreatedItem, typeof(SpecialistInstance));
            Assert.AreEqual(912, CreatedItem.ItemVNum);
            var specialist = (SpecialistInstance)CreatedItem;
            Assert.AreEqual((byte)1, specialist.SpLevel);
        }

        private void CreatingWearableItem()
        {
            CreatedItem = ItemProvider!.Create(1);
        }

        private void ItemShouldBeWearableInstance()
        {
            Assert.IsNotNull(CreatedItem);
            Assert.IsInstanceOfType(CreatedItem, typeof(WearableInstance));
            Assert.AreEqual(1, CreatedItem.ItemVNum);
        }

        private void GeneratingWithInvalidVNum()
        {
            ItemProvider!.Create(9999);
        }

        private void ShouldThrowNullReferenceException_(Lazy<Exception> exception)
        {
            Assert.IsInstanceOfType(exception.Value, typeof(NullReferenceException));
        }
    }
}
