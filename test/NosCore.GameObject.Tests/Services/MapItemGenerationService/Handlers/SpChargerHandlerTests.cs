//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapItemGenerationService.Handlers;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService.Handlers
{
    [TestClass]
    public class SpChargerHandlerTests
    {
        private SpChargerEventHandler Handler = null!;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private IIdService<MapItem> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new SpChargerEventHandler(TestHelpers.Instance.WorldConfiguration);
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItem>(1);
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForNonSpCharger()
        {
            new Spec("Condition should return false for non-SP charger item")
                .Given(ItemIsNotSpCharger)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForRegularItem()
        {
            new Spec("Condition should return false for regular item")
                .Given(ItemIsRegularMainItem)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForEquipment()
        {
            new Spec("Condition should return false for equipment")
                .Given(ItemIsEquipment)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        private MapItem? DroppedSpCharger;
        private IItemInstance? ItemInstance;
        private bool ConditionResult;

        private void ItemIsNotSpCharger()
        {
            ItemInstance = ItemProvider.Create(1012, 10);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void ItemIsRegularMainItem()
        {
            ItemInstance = ItemProvider.Create(1013, 5);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void ItemIsEquipment()
        {
            ItemInstance = ItemProvider.Create(1, 1);
            DroppedSpCharger = CreateMapItem(ItemInstance);
        }

        private void CheckingCondition()
        {
            ConditionResult = Handler.Condition(DroppedSpCharger!);
        }

        private void ConditionShouldBeFalse()
        {
            Assert.IsFalse(ConditionResult);
        }

        private MapItem CreateMapItem(IItemInstance item)
        {
            var mapItem = new MapItem(IdService.GetNextId())
            {
                MapInstance = Session.Character.MapInstance,
                PositionX = 1,
                PositionY = 1,
                ItemInstance = item
            };
            return mapItem;
        }
    }
}
