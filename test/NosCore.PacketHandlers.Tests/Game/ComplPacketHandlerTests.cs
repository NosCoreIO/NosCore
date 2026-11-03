//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class ComplPacketHandlerTests
    {
        private ComplPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession Target = null!;
        private short InitialCompliment;
        private readonly List<MapTypeMapDto> MapTypeMaps = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Target = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.Level = 31;
            InitialCompliment = Target.Character.Compliment;
            Handler = new ComplPacketHandler(TestHelpers.Instance.Clock, TestHelpers.Instance.SessionRegistry, MapTypeMaps);
        }

        [TestMethod]
        public async Task ComplimentShouldIncreaseTargetCompliment()
        {
            await new Spec("Compliment should increase target compliment")
                .Given(OneHourHasPassedSinceConnection)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 1)
                .And(SenderShouldReceive_, Game18NConstString.HaveCommendedPercentZero)
                .And(TargetShouldReceiveCommendedMessage)
                .And(LastComplimentShouldBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentAtLevelThirtyShouldBeRefused()
        {
            await new Spec("Compliment at level thirty should be refused")
                .Given(SenderIsLevel_, (byte)30)
                .And(OneHourHasPassedSinceConnection)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(SenderShouldReceiveRefusal_, Game18NConstString.OnlyPlayersAboveLevelThirtyLead)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentWithinAnHourOfConnectionShouldBeRefused()
        {
            await new Spec("Compliment within an hour of connection should be refused")
                .Given(_MinutesHavePassedSinceConnection, 20)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(SenderShouldBeToldToWait_Minutes, 40L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SecondComplimentOnSameDayShouldBeRefused()
        {
            await new Spec("Second compliment on same day should be refused")
                .Given(OneHourHasPassedSinceConnection)
                .AndAsync(ComplimentingTargetAsync)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 1)
                .And(SenderShouldReceiveRefusal_, Game18NConstString.CanOnlyLeadOncePerDayAndAccount)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentOnNextDayShouldSucceed()
        {
            await new Spec("Compliment on next day should succeed")
                .Given(OneHourHasPassedSinceConnection)
                .AndAsync(ComplimentingTargetAsync)
                .And(ADayHasPassed)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentingSelfShouldDoNothing()
        {
            await new Spec("Complimenting self should do nothing")
                .Given(OneHourHasPassedSinceConnection)
                .WhenAsync(ComplimentingSelfAsync)
                .Then(SenderComplimentShouldBeUnchanged)
                .And(LastComplimentShouldNotBeRecorded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentInGlacernonShouldBeRefused()
        {
            await new Spec("Compliment in Glacernon should be refused")
                .Given(SenderIsOnAMapOfType_, MapTypeType.Act4)
                .And(OneHourHasPassedSinceConnection)
                .WhenAsync(ComplimentingTargetAsync)
                .Then(TargetComplimentShouldBeIncreasedBy_, 0)
                .And(SenderShouldReceiveRefusal_, Game18NConstString.CannotLeadInGlacernon)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ComplimentingAbsentCharacterShouldSendNothing()
        {
            await new Spec("Complimenting absent character should send nothing")
                .Given(SenderIsLevel_, (byte)29)
                .WhenAsync(ComplimentingAbsentCharacterAsync)
                .Then(SenderShouldReceiveNothing)
                .ExecuteAsync();
        }

        private void SenderIsOnAMapOfType_(MapTypeType type) =>
            MapTypeMaps.Add(new MapTypeMapDto { MapId = Session.Character.MapInstance.Map.MapId, MapTypeId = (short)type });

        private void SenderIsLevel_(byte level) => Session.Character.Level = level;

        private void OneHourHasPassedSinceConnection() => TestHelpers.Instance.Clock.AdvanceMinutes(60);

        private void _MinutesHavePassedSinceConnection(int minutes) => TestHelpers.Instance.Clock.AdvanceMinutes(minutes);

        private void ADayHasPassed() => TestHelpers.Instance.Clock.AdvanceHours(24);

        private async Task ComplimentingTargetAsync()
        {
            Session.LastPackets.Clear();
            Target.LastPackets.Clear();
            await Handler.ExecuteAsync(new ComplPacket { CharacterId = Target.Character.CharacterId }, Session);
        }

        private async Task ComplimentingSelfAsync()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ComplPacket { CharacterId = Session.Character.CharacterId }, Session);
        }

        private async Task ComplimentingAbsentCharacterAsync()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new ComplPacket { CharacterId = long.MaxValue }, Session);
        }

        private void SenderShouldReceiveNothing() => Assert.AreEqual(0, Session.LastPackets.Count);

        private void TargetComplimentShouldBeIncreasedBy_(int amount) =>
            Assert.AreEqual(InitialCompliment + amount, Target.Character.Compliment);

        private void SenderComplimentShouldBeUnchanged() =>
            Assert.AreEqual(InitialCompliment, Session.Character.Compliment);

        private void SenderShouldReceive_(Game18NConstString message)
        {
            var packet = Session.LastPackets.OfType<Sayi2Packet>().Single();
            Assert.AreEqual(message, packet.Message);
            Assert.AreEqual(Target.Character.Name, packet.Game18NArguments[0]);
        }

        private void TargetShouldReceiveCommendedMessage()
        {
            var packet = Target.LastPackets.OfType<Sayi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.HaveBeenCommendedByPercentZero, packet.Message);
            Assert.AreEqual(Session.Character.Name, packet.Game18NArguments[0]);
        }

        private void SenderShouldReceiveRefusal_(Game18NConstString message) =>
            Assert.AreEqual(message, Session.LastPackets.OfType<SayiPacket>().Single().Message);

        private void SenderShouldBeToldToWait_Minutes(long minutes)
        {
            var packet = Session.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.CantCommendNow, packet.Message);
            Assert.AreEqual(minutes, packet.Game18NArguments[0]);
        }

        private void LastComplimentShouldBeRecorded() =>
            Assert.AreEqual(TestHelpers.Instance.Clock.GetCurrentInstant(), Session.Character.Account.LastCompliment);

        private void LastComplimentShouldNotBeRecorded() =>
            Assert.IsNull(Session.Character.Account.LastCompliment);
    }
}
