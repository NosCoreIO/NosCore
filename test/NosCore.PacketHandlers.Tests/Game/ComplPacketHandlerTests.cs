//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ComplimentService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class ComplPacketHandlerTests
    {
        private TestWorld World;
        private ComplPacketHandler Handler;
        private ClientSession Session;
        private ClientSession Target;
        private Mock<IComplimentService> ComplimentService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Target = await World.CreateSessionAsync();
            ComplimentService = World.Freeze<Mock<IComplimentService>>();
            Handler = World.Create<ComplPacketHandler>();
        }

        [TestMethod]
        public async Task ASuccessfulComplimentTellsBothPlayers()
        {
            await new Spec("A successful compliment tells the sender and the target")
                .Given(TheComplimentSucceeds)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TheServiceShouldBeAskedToComplimentTheTarget)
                .And(SenderShouldReceiveHaveCommended)
                .And(TargetShouldReceiveCommendedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RefusalsAreShownToTheSender()
        {
            await new Spec("Each refusal shows its own message to the sender")
                .WhenAsync(Refusal_ShouldShow_, ComplimentOutcome.InGlacernon, Game18NConstString.CannotLeadInGlacernon)
                .AndAsync(Refusal_ShouldShow_, ComplimentOutcome.LevelTooLow, Game18NConstString.OnlyPlayersAboveLevelThirtyLead)
                .AndAsync(Refusal_ShouldShow_, ComplimentOutcome.AlreadyComplimentedToday, Game18NConstString.CanOnlyLeadOncePerDayAndAccount)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentingTooSoonShowsTheMinutesLeft()
        {
            await new Spec("Complimenting too soon after connecting shows the minutes left")
                .Given(TheComplimentIsRefusedWith_, new ComplimentResult(ComplimentOutcome.TooSoonAfterConnection, MinutesToWait: 40))
                .WhenAsync(ComplimentingTargetAsync)
                .Then(SenderShouldBeToldToWait_Minutes, 40L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NoTargetSendsNothing()
        {
            await new Spec("A compliment without a target sends nothing")
                .Given(TheComplimentIsRefusedWith_, new ComplimentResult(ComplimentOutcome.NoTarget))
                .WhenAsync(ComplimentingTargetAsync)
                .Then(SenderShouldReceiveNothing)
                .ExecuteAsync();
        }

        private void TheComplimentSucceeds()
        {
            PlayerComponentBundle target = Target.Character;
            ComplimentService.Setup(x => x.Compliment(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>()))
                .Returns(new ComplimentResult(ComplimentOutcome.Complimented, target));
        }

        private void TheComplimentIsRefusedWith_(ComplimentResult result)
        {
            ComplimentService.Setup(x => x.Compliment(It.IsAny<PlayerComponentBundle>(), It.IsAny<long>()))
                .Returns(result);
        }

        private async Task Refusal_ShouldShow_(ComplimentOutcome outcome, Game18NConstString message)
        {
            TheComplimentIsRefusedWith_(new ComplimentResult(outcome));
            await ComplimentingTargetAsync();
            Assert.AreEqual(message, Session.LastPackets.OfType<SayiPacket>().Single().Message);
        }

        private async Task ComplimentingTargetAsync()
        {
            Session.LastPackets.Clear();
            Target.LastPackets.Clear();
            await Handler.ExecuteAsync(new ComplPacket { CharacterId = Target.Character.CharacterId }, Session);
        }

        private void TheServiceShouldBeAskedToComplimentTheTarget()
        {
            var targetId = Target.Character.CharacterId;
            ComplimentService.Verify(x => x.Compliment(It.IsAny<PlayerComponentBundle>(), targetId), Times.Once);
        }

        private void SenderShouldReceiveHaveCommended()
        {
            var packet = Session.LastPackets.OfType<Sayi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.HaveCommendedPercentZero, packet.Message);
            Assert.AreEqual(Target.Character.Name, packet.Game18NArguments[0]);
        }

        private void TargetShouldReceiveCommendedMessage()
        {
            var packet = Target.LastPackets.OfType<Sayi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.HaveBeenCommendedByPercentZero, packet.Message);
            Assert.AreEqual(Session.Character.Name, packet.Game18NArguments[0]);
        }

        private void SenderShouldBeToldToWait_Minutes(long minutes)
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.CantCommendNow, packet.Message);
            Assert.AreEqual(minutes, packet.Game18NArguments[0]);
        }

        private void SenderShouldReceiveNothing() => Assert.AreEqual(0, Session.LastPackets.Count);
    }
}
