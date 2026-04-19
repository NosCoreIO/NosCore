//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService.Handlers;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService
{
    [TestClass]
    public class MapItemGenerationServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IMapItemGenerationService Service = null!;
        private IItemGenerationService ItemProvider = null!;
        private MapInstance MapInstance = null!;
        private IIdService<MapItemComponentBundle> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();

            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItemComponentBundle>(1);

            var handlers = new List<IGetMapItemEventHandler>
            {
                new DropEventHandler(),
                new SpChargerEventHandler(TestHelpers.Instance.WorldConfiguration),
                new GoldDropEventHandler(TestHelpers.Instance.WorldConfiguration)
            };

            Service = new GameObject.Services.MapItemGenerationService.MapItemGenerationService(handlers, IdService);
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

        private MapItemComponentBundle? CreatedMapItem;
        private MapItemComponentBundle? SecondMapItem;
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
            Assert.IsTrue(CreatedMapItem.Value.VisualId > 0);
        }

        private void PositionShouldBeCorrect()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.AreEqual(PositionX, CreatedMapItem.Value.PositionX);
            Assert.AreEqual(PositionY, CreatedMapItem.Value.PositionY);
        }

        private void ItemInstanceShouldBeCorrect()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(CreatedMapItem.Value.ItemInstance);
            Assert.AreEqual(1012, CreatedMapItem.Value.ItemInstance!.ItemVNum);
            Assert.AreEqual((short)25, CreatedMapItem.Value.ItemInstance!.Amount);
        }

        private void IdsShouldBeUnique()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(SecondMapItem);
            Assert.AreNotEqual(CreatedMapItem.Value.VisualId, SecondMapItem.Value.VisualId);
        }

        private void MapInstanceShouldBeSet()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.AreEqual(MapInstance.MapInstanceId, CreatedMapItem.Value.MapInstanceId);
        }

        private void GoldMapItemShouldBeCreated()
        {
            Assert.IsNotNull(CreatedMapItem);
            Assert.IsNotNull(CreatedMapItem.Value.ItemInstance);
            Assert.AreEqual(1012, CreatedMapItem.Value.ItemInstance!.ItemVNum);
            Assert.AreEqual((short)1000, CreatedMapItem.Value.ItemInstance!.Amount);
        }
    }
}
