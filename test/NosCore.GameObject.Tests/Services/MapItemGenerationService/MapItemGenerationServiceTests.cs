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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.EventLoaderService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService.Handlers;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService
{
    [TestClass]
    public class MapItemGenerationServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IMapItemGenerationService Service = null!;
        private IItemGenerationService ItemProvider = null!;
        private MapInstance MapInstance = null!;
        private IIdService<MapItem> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();

            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItem>(1);

            var handlers = new List<IEventHandler<MapItem, Tuple<MapItem, GetPacket>>>
            {
                new DropEventHandler(),
                new SpChargerEventHandler(),
                new GoldDropEventHandler(TestHelpers.Instance.WorldConfiguration)
            };

            var eventLoader = new EventLoaderService<MapItem, Tuple<MapItem, GetPacket>, IGetMapItemEventHandler>(handlers);
            Service = new GameObject.Services.MapItemGenerationService.MapItemGenerationService(eventLoader, IdService);
            MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
        }

        [TestMethod]
        public void CreatingMapItemShouldReturnMapItem()
        {
            new Spec("Creating map item should return map item")
                .When(CreatingMapItem)
                .Then(MapItemShouldBeCreated)
                .Execute();
        }

        [TestMethod]
        public void CreatedMapItemShouldHaveCorrectPosition()
        {
            new Spec("Created map item should have correct position")
                .When(CreatingMapItemAtPosition)
                .Then(PositionShouldBeCorrect)
                .Execute();
        }

        [TestMethod]
        public void CreatedMapItemShouldHaveCorrectItemInstance()
        {
            new Spec("Created map item should have correct item instance")
                .When(CreatingMapItemWithItem)
                .Then(ItemInstanceShouldBeCorrect)
                .Execute();
        }

        [TestMethod]
        public void CreatedMapItemShouldHaveUniqueId()
        {
            new Spec("Created map item should have unique id")
                .When(CreatingMultipleMapItems)
                .Then(IdsShouldBeUnique)
                .Execute();
        }

        [TestMethod]
        public void CreatedMapItemShouldHaveMapInstanceSet()
        {
            new Spec("Created map item should have map instance set")
                .When(CreatingMapItem)
                .Then(MapInstanceShouldBeSet)
                .Execute();
        }

        [TestMethod]
        public void CreatingMapItemWithGoldShouldSucceed()
        {
            new Spec("Creating map item with gold should succeed")
                .When(CreatingGoldMapItem)
                .Then(GoldMapItemShouldBeCreated)
                .Execute();
        }

        private MapItem? CreatedMapItem;
        private MapItem? SecondMapItem;
        private IItemInstance? ItemInstance;
        private short PositionX = 3;
        private short PositionY = 4;

        private void CreatingMapItem()
        {
            ItemInstance = ItemProvider.Create(1012, 10);
            CreatedMapItem = Service.Create(MapInstance, ItemInstance, 1, 1);
        }

        private void CreatingMapItemAtPosition()
        {
            ItemInstance = ItemProvider.Create(1012, 5);
            CreatedMapItem = Service.Create(MapInstance, ItemInstance, PositionX, PositionY);
        }

        private void CreatingMapItemWithItem()
        {
            ItemInstance = ItemProvider.Create(1012, 25);
            CreatedMapItem = Service.Create(MapInstance, ItemInstance, 1, 1);
        }

        private void CreatingMultipleMapItems()
        {
            var item1 = ItemProvider.Create(1012, 1);
            var item2 = ItemProvider.Create(1013, 1);
            CreatedMapItem = Service.Create(MapInstance, item1, 1, 1);
            SecondMapItem = Service.Create(MapInstance, item2, 2, 2);
        }

        private void CreatingGoldMapItem()
        {
            ItemInstance = ItemProvider.Create(1012, 1000);
            CreatedMapItem = Service.Create(MapInstance, ItemInstance, 1, 1);
        }

        private void MapItemShouldBeCreated()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsTrue(CreatedMapItem.VisualId > 0);
        }

        private void PositionShouldBeCorrect()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.AreEqual(PositionX, CreatedMapItem.PositionX);
            Assert.AreEqual(PositionY, CreatedMapItem.PositionY);
        }

        private void ItemInstanceShouldBeCorrect()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(CreatedMapItem.ItemInstance);
            Assert.AreEqual(1012, CreatedMapItem.ItemInstance.ItemVNum);
            Assert.AreEqual((short)25, CreatedMapItem.ItemInstance.Amount);
        }

        private void IdsShouldBeUnique()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(SecondMapItem);
            Assert.AreNotEqual(CreatedMapItem.VisualId, SecondMapItem.VisualId);
        }

        private void MapInstanceShouldBeSet()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(CreatedMapItem.MapInstance);
            Assert.AreEqual(MapInstance, CreatedMapItem.MapInstance);
        }

        private void GoldMapItemShouldBeCreated()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(CreatedMapItem.ItemInstance);
            Assert.AreEqual(1012, CreatedMapItem.ItemInstance.ItemVNum);
            Assert.AreEqual((short)1000, CreatedMapItem.ItemInstance.Amount);
        }
    }
}
