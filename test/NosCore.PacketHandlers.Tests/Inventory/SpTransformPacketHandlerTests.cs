//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemInfoService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.PacketHandlers.Inventory;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Inventory
{
    [TestClass]
    public class SpTransformPacketHandlerTests
    {
        private ClientSession Session = null!;
        private SpTransformPacketHandler Handler = null!;
        private Mock<ISpecialistTransformationService> Service = null!;
        private readonly Mock<IItemInfoService> ItemInfo = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Service = new Mock<ISpecialistTransformationService>();
            ItemInfo.Setup(i => i.GenerateInfo(It.IsAny<IItemInstance>())).Returns(new SlInfoPacket());
            Handler = new SpTransformPacketHandler(Service.Object, ItemInfo.Object, new Mock<IBattleStatsProvider>().Object);
        }

        [TestMethod]
        public async Task WearingAsksForTheBar()
        {
            await new Spec("Asking to wear the specialist toggles it without a completed bar")
                .Given(TheToggleReports_, SpecialistTransformationOutcome.Handled)
                .WhenAsync(Sending_, SlPacketType.WearSp)
                .Then(TheToggleShouldBeCalledWithCompletedBar_, false)
                .And(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBarsFollowUpTransforms()
        {
            await new Spec("The bar's follow-up toggles the specialist with a completed bar")
                .Given(TheToggleReports_, SpecialistTransformationOutcome.Handled)
                .WhenAsync(Sending_, SlPacketType.WearSpAndTransform)
                .Then(TheToggleShouldBeCalledWithCompletedBar_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefusalsAreShown()
        {
            await new Spec("Each refused transformation shows its message")
                .WhenAsync(Refusal_ShouldShow_, SpecialistTransformationOutcome.NoSpecialistCardEquipped, Game18NConstString.NoSpecialistCardEquipped)
                .AndAsync(Refusal_ShouldShow_, SpecialistTransformationOutcome.InVehicle, Game18NConstString.CantUseInVehicle)
                .AndAsync(Refusal_ShouldShow_, SpecialistTransformationOutcome.NoPoints, Game18NConstString.ZeroPointsTransformationCancelled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACooldownShowsTheSecondsLeft()
        {
            await new Spec("A transformation on cooldown shows the seconds left")
                .Given(TheToggleReturns_, new SpecialistTransformationResult(SpecialistTransformationOutcome.OnCooldown, 30))
                .WhenAsync(Sending_, SlPacketType.WearSp)
                .Then(ShouldReceiveCooldownMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpendingPointsOnACardNotWornIsRefused()
        {
            await new Spec("Spending points on a card that is not worn shows the card must be equipped")
                .Given(SpendingReports_, new SpecialistPointsResult(SpecialistPointsOutcome.CardNotWorn))
                .WhenAsync(SpendingPoints)
                .Then(ShouldReceive_, Game18NConstString.SpecialistCardMustBeEquipped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SpentPointsRefreshTheCardInfo()
        {
            await new Spec("Spent points forward the packet values and refresh the card info")
                .Given(SpendingReports_, new SpecialistPointsResult(SpecialistPointsOutcome.Spent, new SpecialistInstance(new NosCore.GameObject.Services.ItemGenerationService.Item.Item())))
                .WhenAsync(SpendingPoints)
                .Then(ThePacketValuesShouldBeForwarded)
                .And(TheCardInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefusedPointsSendNothing()
        {
            await new Spec("Points the card cannot take send nothing")
                .Given(SpendingReports_, new SpecialistPointsResult(SpecialistPointsOutcome.Refused))
                .WhenAsync(SpendingPoints)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void TheToggleReports_(SpecialistTransformationOutcome outcome) =>
            TheToggleReturns_(new SpecialistTransformationResult(outcome));

        private void TheToggleReturns_(SpecialistTransformationResult result) =>
            Service.Setup(s => s.ToggleAsync(It.IsAny<ClientSession>(), It.IsAny<bool>())).ReturnsAsync(result);

        private void SpendingReports_(SpecialistPointsResult result) =>
            Service.Setup(s => s.SpendPointsAsync(It.IsAny<ClientSession>(), It.IsAny<long?>(), It.IsAny<short>(),
                It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>())).ReturnsAsync(result);

        private Task Sending_(SlPacketType type) =>
            Handler.ExecuteAsync(new SpTransformPacket { Type = type }, Session);

        private Task SpendingPoints() =>
            Handler.ExecuteAsync(new SpTransformPacket
            {
                Type = SlPacketType.ChangePoints, TransportId = 42,
                SpecialistDamage = 1, SpecialistDefense = 2, SpecialistElement = 3, SpecialistHP = 4
            }, Session);

        private async Task Refusal_ShouldShow_(SpecialistTransformationOutcome outcome, Game18NConstString message)
        {
            Session.LastPackets.Clear();
            TheToggleReports_(outcome);
            await Sending_(SlPacketType.WearSp);
            ShouldReceive_(message);
        }

        private void TheToggleShouldBeCalledWithCompletedBar_(bool completed) =>
            Service.Verify(s => s.ToggleAsync(Session, completed), Times.Once);

        private void ThePacketValuesShouldBeForwarded() =>
            Service.Verify(s => s.SpendPointsAsync(Session, 42, 1, 2, 3, 4), Times.Once);

        private void TheCardInfoShouldBeSent() => Assert.AreEqual(1, Session.LastPackets.OfType<SlInfoPacket>().Count());

        private void ShouldReceive_(Game18NConstString message) =>
            Assert.AreEqual(message, Session.LastPackets.OfType<MsgiPacket>().Single().Message);

        private void ShouldReceiveCooldownMessage()
        {
            var packet = Session.LastPackets.OfType<MsgiPacket>().Single();
            Assert.AreEqual(Game18NConstString.CantTrasformWithSideEffect, packet.Message);
            Assert.AreEqual(4, packet.ArgumentType);
            Assert.AreEqual((short)30, packet.Game18NArguments[0]);
        }

        private void NoPacketShouldBeSent() => Assert.AreEqual(0, Session.LastPackets.Count);
    }
}
