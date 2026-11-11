//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class PetManagementHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IMinilandService> _miniland;
        private PetManagementHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Level = 30;
            _miniland = World.Freeze<Mock<IMinilandService>>();
            _handler = World.UseReal<IPartnerSpecialistService, PartnerSpecialistService>().Create<PetManagementHandler>();
        }

        private GameObject.Services.MateService.Mate Add(GameObject.Services.MateService.Mate mate)
        {
            _session.Character.Mates[mate.MateTransportId] = mate;
            return mate;
        }

        private void InOwnMiniland()
        {
            var mapInstanceId = _session.Character.MapInstance.MapInstanceId;
            var ownerId = _session.Character.CharacterId;
            _miniland.Setup(s => s.GetMinilandFromMapInstanceId(mapInstanceId))
                .Returns(new Miniland { OwnerId = ownerId });
        }

        private Task RunAsync(short type, GameObject.Services.MateService.Mate mate) =>
            _handler.HandleAsync(_session, null, new NrunPacket
            {
                Runner = NrunRunnerType.PetManagement,
                Type = type,
                VisualType = VisualType.Monster,
                VisualId = mate.MateTransportId
            });

        private void BarRanOut() => World.Clock.Advance(Duration.FromSeconds(4));

        [TestMethod]
        public async Task JoiningTheTeamReplacesTheMateOfTheSameKind()
        {
            InOwnMiniland();
            var out1 = Add(TestMates.Pet(_session.Character.CharacterId));
            await out1.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
            var home = Add(TestMates.Pet(_session.Character.CharacterId, false));
            var partner = Add(TestMates.Partner(_session.Character.CharacterId));

            await RunAsync(2, home);

            Assert.IsTrue(home.IsTeamMember);
            Assert.IsFalse(out1.IsTeamMember);
            Assert.IsTrue(partner.IsTeamMember);
            Assert.AreEqual((short)5, out1.MapX);
            Assert.IsNotNull(out1.Entity, "The replaced pet stays in the Miniland.");
            Assert.IsTrue(_session.LastPackets.OfType<PinitPacket>().Any());
        }

        [TestMethod]
        public async Task ATransformedPartnerSentHomeTurnsBack()
        {
            InOwnMiniland();
            var partner = Add(TestMates.Partner(_session.Character.CharacterId));
            await partner.SpawnAsync(_session.Character, _session.Character.MapInstance, 6, 7);
            partner.SpecialistMorph = 2041;

            await RunAsync(4, partner);

            Assert.IsNull(partner.SpecialistMorph);
            Assert.AreEqual(30, _session.LastPackets.OfType<PsdPacket>().Single().SpCooldown);
        }

        [TestMethod]
        public async Task AMateAboveTheOwnersLevelCannotJoin()
        {
            var strong = Add(TestMates.Pet(_session.Character.CharacterId, false, level: 50));

            await RunAsync(2, strong);

            Assert.IsFalse(strong.IsTeamMember);
            Assert.AreEqual(Game18NConstString.CantSetNosmateWithHigherLevel,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task InTheMinilandAMateIsSentHomeAtOnce()
        {
            InOwnMiniland();
            var pet = Add(TestMates.Pet(_session.Character.CharacterId));
            await pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 6, 7);

            await RunAsync(4, pet);

            Assert.IsFalse(pet.IsTeamMember);
            Assert.AreEqual(((short)6, (short)7), (pet.MapX, pet.MapY));
            Assert.IsFalse(_session.LastPackets.OfType<QnaiPacket>().Any());
        }

        [TestMethod]
        public async Task ElsewhereSendingAMateHomeAsksFirst()
        {
            var pet = Add(TestMates.Pet(_session.Character.CharacterId));

            await RunAsync(4, pet);

            var question = _session.LastPackets.OfType<QnaiPacket>().Single();
            Assert.AreEqual(Game18NConstString.AskMateReturn, question.Question);
            Assert.AreEqual((short)5, ((NrunPacket)question.YesPacket.NotNull()).Type);
            Assert.IsTrue(pet.IsTeamMember);
        }

        [TestMethod]
        public async Task ConfirmingStartsTheBarAndItsEndSendsTheMateHome()
        {
            var pet = Add(TestMates.Pet(_session.Character.CharacterId));
            await pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 6, 7);

            await RunAsync(5, pet);
            var bar = _session.LastPackets.OfType<DelayPacket>().Single();
            Assert.AreEqual(DelayPacketType.BackToMiniland, bar.Type);
            Assert.AreEqual((short)6, ((NrunPacket)bar.Packet.NotNull()).Type);

            BarRanOut();
            await RunAsync(6, pet);

            Assert.IsFalse(pet.IsTeamMember);
            Assert.IsNull(pet.Entity);
            Assert.IsTrue(_session.Character.MapInstance.LastPackets.OfType<OutPacket>()
                .Any(s => s.VisualId == pet.MateTransportId));
            Assert.AreEqual(Game18NConstString.SentBackToMiniland,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task TheBarsFollowUpIsIgnoredWithoutABar()
        {
            var pet = Add(TestMates.Pet(_session.Character.CharacterId));

            await RunAsync(6, pet);

            Assert.IsTrue(pet.IsTeamMember);
        }

        [TestMethod]
        public async Task SummoningIsRefusedWhileAMateOfTheSameKindIsOut()
        {
            Add(TestMates.Pet(_session.Character.CharacterId));
            var home = Add(TestMates.Pet(_session.Character.CharacterId, false));

            await RunAsync(7, home);

            Assert.AreEqual(Game18NConstString.PetAlreadyAccompanying,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        [TestMethod]
        public async Task ASummonedMateAppearsNextToTheOwner()
        {
            var home = Add(TestMates.Partner(_session.Character.CharacterId, false));

            await RunAsync(7, home);
            Assert.AreEqual((short)9, ((NrunPacket)_session.LastPackets.OfType<DelayPacket>().Single().Packet.NotNull()).Type);
            BarRanOut();
            await RunAsync(9, home);

            Assert.IsTrue(home.IsTeamMember);
            Assert.IsNotNull(home.Entity);
            Assert.IsTrue(Math.Abs(home.PositionX - _session.Character.PositionX) <= 1);
            Assert.AreEqual(Game18NConstString.MateSummoned,
                _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task AnotherPlayersMateIsIgnored()
        {
            var foreign = TestMates.Pet(_session.Character.CharacterId + 1, false);

            await RunAsync(2, foreign);

            Assert.IsFalse(foreign.IsTeamMember);
            Assert.AreEqual(0, _session.LastPackets.Count);
        }
    }
}
