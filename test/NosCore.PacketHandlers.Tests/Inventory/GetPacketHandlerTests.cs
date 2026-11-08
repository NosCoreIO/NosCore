//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.MapItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class GetPacketHandlerTests
    {
        private const short ItemVNum = 1012;
        private ClientSession Session = null!;
        private IItemGenerationService ItemProvider = null!;
        private GetPacketHandler Handler = null!;
        private InlineOrDeferredMessageBus<MapItemPickedUpEvent> Bus = null!;
        private long MapItemVisualId;
        private Guid DroppedInstanceId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ItemProvider = TestHelpers.Instance.GenerateItemProvider();
            var dropHandler = new DropHandler(new Mock<Wolverine.IMessageBus>().Object);
            var goldHandler = new GoldDropHandler(TestHelpers.Instance.WorldConfiguration, TestHelpers.Instance.ItemList);
            var spChargerHandler = new SpChargerHandler(TestHelpers.Instance.WorldConfiguration);
            Bus = new InlineOrDeferredMessageBus<MapItemPickedUpEvent>(async evt =>
            {
                await dropHandler.Handle(evt);
                await goldHandler.Handle(evt);
                await spChargerHandler.Handle(evt);
            });
            Handler = new GetPacketHandler(NullLogger<GetPacketHandler>.Instance, TestHelpers.Instance.DistanceCalculator,
                TestHelpers.Instance.Clock, TestHelpers.Instance.LogLanguageLocalizer, Bus.Object);
        }

        [TestMethod]
        public async Task TheSamePickUpSentTwiceGrantsTheItemOnce()
        {
            await new Spec("The same pick-up sent twice grants the item once")
                .Given(AnItemLiesNextToTheCharacter)
                .WhenAsync(TheSamePickUpIsSentTwiceConcurrently)
                .Then(TheCharacterHolds_Items, 1)
                .And(TheItemIsNoLongerOnTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APickedUpItemGetsANewInstanceId()
        {
            await new Spec("A picked-up item gets a new instance id")
                .Given(AnItemLiesNextToTheCharacter)
                .WhenAsync(TheSamePickUpIsSentTwiceConcurrently)
                .Then(TheHeldItemHasANewInstanceId)
                .ExecuteAsync();
        }

        private void AnItemLiesNextToTheCharacter()
        {
            var inv = Session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(ItemProvider.Create(ItemVNum, 1), Session.Character.CharacterId))!.First();
            var mapItem = Session.Character.MapInstance.PutItem(1, inv.ItemInstance, Session)!.Value;
            MapItemVisualId = mapItem.VisualId;
            DroppedInstanceId = mapItem.ItemInstance!.Id;
            Assert.AreEqual(0, Session.Character.InventoryService.CountItem(ItemVNum));
        }

        private async Task TheSamePickUpIsSentTwiceConcurrently()
        {
            var packet = new GetPacket { PickerType = VisualType.Player, VisualId = MapItemVisualId };
            await Task.WhenAll(Handler.ExecuteAsync(packet, Session), Handler.ExecuteAsync(packet, Session));
            await Bus.DeliverPublishedAsync();
        }

        private void TheCharacterHolds_Items(int amount) =>
            Assert.AreEqual(amount, Session.Character.InventoryService.CountItem(ItemVNum));

        private void TheItemIsNoLongerOnTheMap() =>
            Assert.IsNull(Session.Character.MapInstance.TryGetMapItem(MapItemVisualId));

        private void TheHeldItemHasANewInstanceId()
        {
            var held = Session.Character.InventoryService.Values.Single(i => i.ItemInstance.ItemVNum == ItemVNum);
            Assert.AreNotEqual(DroppedInstanceId, held.ItemInstance.Id);
        }
    }
}
