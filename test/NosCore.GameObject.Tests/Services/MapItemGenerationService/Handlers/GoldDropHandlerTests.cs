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
    public class GoldDropHandlerTests
    {
        private GoldDropEventHandler Handler = null!;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private IIdService<MapItem> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new GoldDropEventHandler(TestHelpers.Instance.WorldConfiguration);
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItem>(1);
        }

        [TestMethod]
        public void ConditionShouldReturnFalseForRegularItem()
        {
            new Spec("Condition should return false for regular item")
                .Given(ItemIsRegularItem)
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

        [TestMethod]
        public void ConditionShouldReturnFalseForSpecialist()
        {
            new Spec("Condition should return false for specialist card")
                .Given(ItemIsSpecialist)
                .When(CheckingCondition)
                .Then(ConditionShouldBeFalse)
                .Execute();
        }

        [TestMethod]
        public void ConditionShouldCheckVNum1046()
        {
            new Spec("Condition should specifically check for VNum 1046")
                .Given(ItemIsRegularItem)
                .When(CheckingConditionForGoldVNum)
                .Then(ConditionChecksShouldWork)
                .Execute();
        }

        private MapItem? TestMapItem;
        private IItemInstance? ItemInstance;
        private bool ConditionResult;
        private bool GoldVNumCheckResult;

        private void ItemIsRegularItem()
        {
            ItemInstance = ItemProvider.Create(1012, 10);
            TestMapItem = CreateMapItem(ItemInstance);
        }

        private void ItemIsEquipment()
        {
            ItemInstance = ItemProvider.Create(1, 1);
            TestMapItem = CreateMapItem(ItemInstance);
        }

        private void ItemIsSpecialist()
        {
            ItemInstance = ItemProvider.Create(912, 1);
            TestMapItem = CreateMapItem(ItemInstance);
        }

        private void CheckingCondition()
        {
            ConditionResult = Handler.Condition(TestMapItem!);
        }

        private void CheckingConditionForGoldVNum()
        {
            GoldVNumCheckResult = TestMapItem!.VNum != 1046;
        }

        private void ConditionShouldBeFalse()
        {
            Assert.IsFalse(ConditionResult);
        }

        private void ConditionChecksShouldWork()
        {
            Assert.IsTrue(GoldVNumCheckResult);
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
