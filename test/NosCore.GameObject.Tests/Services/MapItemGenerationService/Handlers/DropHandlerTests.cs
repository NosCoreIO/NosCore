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
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.MapItemGenerationService.Handlers
{
    [TestClass]
    public class DropHandlerTests
    {
        private DropEventHandler Handler = null!;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private IIdService<MapItem> IdService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new DropEventHandler();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            IdService = new IdService<MapItem>(1);
        }

        [TestMethod]
        public void ConditionShouldReturnTrueForNonMapItem()
        {
            new Spec("Condition should return true for non-map item")
                .Given(ItemIsNotMapType)
                .When(CheckingCondition)
                .Then(ConditionShouldBeTrue)
                .Execute();
        }

        [TestMethod]
        public async Task PickingUpItemShouldAddToInventory()
        {
            await new Spec("Picking up item should add to inventory")
                .Given(ItemDroppedOnMap)
                .And(CharacterHasEmptyInventory)
                .WhenAsync(PickingUpItem)
                .Then(ItemShouldBeInInventory)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemShouldRemoveFromMap()
        {
            await new Spec("Picking up item should remove from map")
                .Given(ItemDroppedOnMap)
                .And(CharacterHasEmptyInventory)
                .WhenAsync(PickingUpItem)
                .Then(ItemShouldBeRemovedFromMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PickingUpItemWithFullInventoryShouldFail()
        {
            await new Spec("Picking up item with full inventory should fail")
                .Given(ItemDroppedOnMap)
                .And(CharacterHasFullInventory)
                .WhenAsync(PickingUpItem)
                .Then(ShouldReceiveNotEnoughSpaceMessage)
                .ExecuteAsync();
        }

        private MapItem? DroppedItem;
        private IItemInstance? ItemInstance;
        private bool ConditionResult;

        private void ItemIsNotMapType()
        {
            ItemInstance = ItemProvider.Create(1012, 10);
            DroppedItem = CreateMapItem(ItemInstance);
        }

        private void ItemDroppedOnMap()
        {
            ItemInstance = ItemProvider.Create(1012, 5);
            DroppedItem = CreateMapItem(ItemInstance);
            Session.Character.MapInstance.MapItems.TryAdd(DroppedItem.VisualId, DroppedItem);
        }

        private void CharacterHasEmptyInventory()
        {
            Session.Character.InventoryService.Clear();
        }

        private void CharacterHasFullInventory()
        {
            for (var i = 0; i < 48; i++)
            {
                var item = ItemProvider.Create(1012, 999);
                Session.Character.InventoryService.AddItemToPocket(
                    GameObject.Services.InventoryService.InventoryItemInstance.Create(item, Session.Character.CharacterId));
            }
        }

        private void CheckingCondition()
        {
            ConditionResult = Handler.Condition(DroppedItem!);
        }

        private async Task PickingUpItem()
        {
            var requestData = new RequestData<Tuple<MapItem, GetPacket>>(
                Session,
                new Tuple<MapItem, GetPacket>(DroppedItem!, new GetPacket
                {
                    PickerId = Session.Character.CharacterId,
                    VisualId = DroppedItem!.VisualId,
                    PickerType = VisualType.Player
                }));
            await Handler.ExecuteAsync(requestData);
        }

        private void ConditionShouldBeTrue()
        {
            Assert.IsTrue(ConditionResult);
        }

        private void ConditionShouldBeFalse()
        {
            Assert.IsFalse(ConditionResult);
        }

        private void ItemShouldBeInInventory()
        {
            var invItem = Session.Character.InventoryService.Values.FirstOrDefault(i => i.ItemInstance.ItemVNum == 1012);
            Assert.IsNotNull(invItem);
        }

        private void ItemShouldBeRemovedFromMap()
        {
            Assert.IsFalse(Session.Character.MapInstance.MapItems.ContainsKey(DroppedItem!.VisualId));
        }

        private void ShouldReceiveNotEnoughSpaceMessage()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is MsgiPacket msg && msg.Message == Game18NConstString.NotEnoughSpace));
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
