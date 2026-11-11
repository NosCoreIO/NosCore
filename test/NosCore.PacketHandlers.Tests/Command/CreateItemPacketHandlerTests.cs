//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemGrantService;
using NosCore.PacketHandlers.Command;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class CreateItemPacketHandlerTests
    {
        private TestWorld World;
        private CreateItemPackettHandler Handler;
        private ClientSession Session;
        private Mock<IItemGrantService> ItemGrantService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            ItemGrantService = World.Freeze<Mock<IItemGrantService>>();
            Handler = World.Create<CreateItemPackettHandler>();
        }

        [TestMethod]
        public async Task ThePacketValuesAreForwarded()
        {
            await new Spec("The item and both optional values are forwarded to the grant")
                .Given(TheGrantReturns_, new ItemGrantResult(ItemGrantOutcome.Refused))
                .WhenAsync(CreatingItem)
                .Then(TheGrantShouldReceiveThePacketValues)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedItemSendsNothing()
        {
            await new Spec("A refused item sends nothing")
                .Given(TheGrantReturns_, new ItemGrantResult(ItemGrantOutcome.Refused))
                .WhenAsync(CreatingItem)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnknownItemShowsAnError()
        {
            await new Spec("An unknown item shows the item does not exist")
                .Given(TheGrantReturns_, new ItemGrantResult(ItemGrantOutcome.UnknownItem))
                .WhenAsync(CreatingItem)
                .Then(ShouldReceive_, Game18NConstString.ItemDoesNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullBagShowsNotEnoughSpace()
        {
            await new Spec("A full bag shows not enough space")
                .Given(TheGrantReturns_, new ItemGrantResult(ItemGrantOutcome.NotEnoughSpace))
                .WhenAsync(CreatingItem)
                .Then(ShouldReceive_, Game18NConstString.NotEnoughSpace)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AGrantedItemRedrawsTheBagAndConfirms()
        {
            await new Spec("A granted item redraws its slot and confirms it with the amount")
                .Given(TheItemIsGranted)
                .WhenAsync(CreatingItem)
                .Then(TheSlotShouldBeRedrawn)
                .And(ShouldReceiveItemReceivedMessageWithAmount_, (short)5)
                .ExecuteAsync();
        }

        private void TheGrantReturns_(ItemGrantResult result) =>
            ItemGrantService.Setup(x => x.Grant(It.IsAny<PlayerComponentBundle>(), It.IsAny<short>(), It.IsAny<short?>(),
                It.IsAny<byte?>())).Returns(result);

        private void TheItemIsGranted()
        {
            var item = InventoryItemInstance.Create(
                new ItemInstance(new Item { VNum = 1012, Type = NosCore.Data.Enumerations.NoscorePocketType.Main }) { Amount = 5 },
                Session.Character.CharacterId);
            TheGrantReturns_(new ItemGrantResult(ItemGrantOutcome.Granted, new List<InventoryItemInstance> { item }, 5));
        }

        private Task CreatingItem() =>
            Handler.ExecuteAsync(new CreateItemPacket { VNum = 1012, DesignOrAmount = 5, Upgrade = 3 }, Session);

        private void TheGrantShouldReceiveThePacketValues() =>
            ItemGrantService.Verify(x => x.Grant(It.IsAny<PlayerComponentBundle>(), 1012, (short?)5, (byte?)3), Times.Once);

        private void NoPacketShouldBeSent() => Assert.AreEqual(0, Session.LastPackets.Count);

        private void ShouldReceive_(Game18NConstString message) =>
            Assert.AreEqual(message, Session.LastPackets.OfType<MsgiPacket>().Single().Message);

        private void TheSlotShouldBeRedrawn() => Assert.AreEqual(1, Session.LastPackets.OfType<IvnPacket>().Count());

        private void ShouldReceiveItemReceivedMessageWithAmount_(short amount)
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.ReceivedThisItem, packet.Message);
            Assert.AreEqual(amount, packet.Game18NArguments[1]);
        }
    }
}
