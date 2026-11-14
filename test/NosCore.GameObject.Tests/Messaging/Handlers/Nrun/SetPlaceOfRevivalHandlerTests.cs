//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class SetPlaceOfRevivalHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Mock<IRespawnService> _respawnService;
        private SetPlaceOfRevivalHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _respawnService = World.Freeze<Mock<IRespawnService>>();
            _respawnService.Setup(r => r.SetRespawnPoint(It.IsAny<PlayerComponentBundle>(),
                It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>())).Returns(true);
            _handler = World.Create<SetPlaceOfRevivalHandler>();
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesSetPlaceOfRevival()
        {
            await new Spec("SetPlaceOfRevivalHandler declares NrunRunnerType.SetPlaceOfRevival")
                .Then(RunnerShouldBe_, NrunRunnerType.SetPlaceOfRevival)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNpcsButtonAsksForConfirmation()
        {
            await new Spec("The dialog button asks a question whose Yes sends the same n_run back")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .WhenAsync(TheDialogButtonIsPressed)
                .Then(TheQuestionShouldBeAskedInTheCharactersLanguage)
                .And(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("A confirmation with no target is a no-op: no respawn update, no confirmation msg")
                .WhenAsync(NrunIsHandledWithType_AndNullTarget, (short)1)
                .Then(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonNpcTargetIsIgnored()
        {
            await new Spec("A confirmation on a target that is not an NPC is a no-op")
                .WhenAsync(NrunIsHandledWithType_AndGenericAliveTarget, (short)1)
                .Then(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingSavesTheRespawnPoint()
        {
            await new Spec("Confirming the question saves the NPC's position as the respawn point and confirms with the client's own message")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .AndAsync(TheDialogButtonIsPressed)
                .WhenAsync(TheQuestionIsConfirmed)
                .Then(RespawnPointShouldBeSetTo_And_, (short)5, (short)6)
                .And(RevivalPlaceChangedShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingOnAMapWithoutAPlaceOfRevivalSaysNothing()
        {
            await new Spec("A map without a place of revival saves nothing, so no confirmation is sent")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .And(TheMapHasNoPlaceOfRevival)
                .AndAsync(TheDialogButtonIsPressed)
                .WhenAsync(TheQuestionIsConfirmed)
                .Then(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingTwiceAsksAgain()
        {
            await new Spec("One question allows one confirmation; the same n_run afterwards asks again")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .AndAsync(TheDialogButtonIsPressed)
                .WhenAsync(TheQuestionIsConfirmed)
                .AndAsync(TheQuestionIsConfirmed)
                .Then(RespawnPointShouldBeSetTo_And_, (short)5, (short)6)
                .And(TheQuestionShouldBeAskedInTheCharactersLanguage)
                .ExecuteAsync();
        }

        private void TheCharacterStandsNextToAnNpcAt_And_(short x, short y)
        {
            _session.Character.PositionX = x;
            _session.Character.PositionY = (short)(y - 1);
            _session.Character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new()
                    {
                        MapNpcId = 100,
                        MapId = _session.Character.MapInstance.Map.MapId,
                        MapX = x,
                        MapY = y,
                        VNum = 1
                    }
                },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
        }

        private void TheMapHasNoPlaceOfRevival() =>
            _respawnService.Setup(r => r.SetRespawnPoint(It.IsAny<PlayerComponentBundle>(),
                It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>())).Returns(false);

        private Task TheDialogButtonIsPressed() => NrunIsHandledOnThatNpc();

        private Task TheQuestionIsConfirmed() => NrunIsHandledOnThatNpc();

        private Task NrunIsHandledOnThatNpc() =>
            _handler.HandleAsync(_session, _session.Character.MapInstance.GetNpcById(100),
                new NrunPacket
                {
                    Runner = NrunRunnerType.SetPlaceOfRevival,
                    Type = 0,
                    VisualType = VisualType.Npc,
                    VisualId = 100,
                });

        private void RespawnPointShouldBeSetTo_And_(short x, short y)
        {
            var mapId = _session.Character.MapInstance.Map.MapId;
            _respawnService.Verify(r => r.SetRespawnPoint(It.IsAny<PlayerComponentBundle>(), mapId, x, y), Times.Once);
        }

        private void RevivalPlaceChangedShouldBeSent()
        {
            var msgi = _session.LastPackets.OfType<MsgiPacket>().Single();
            Assert.AreEqual(MessageType.Default, msgi.Type);
            Assert.AreEqual(Game18NConstString.RevivalPlaceChanged, msgi.Message);
        }

        private void RunnerShouldBe_(NrunRunnerType expected) =>
            Assert.AreEqual(expected, _handler.Runner);

        private Task NrunIsHandledWithType_AndNullTarget(short type) =>
            _handler.HandleAsync(_session, null,
                new NrunPacket
                {
                    Runner = NrunRunnerType.SetPlaceOfRevival,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 1,
                });

        private Task NrunIsHandledWithType_AndGenericAliveTarget(short type) =>
            _handler.HandleAsync(_session, new Mock<IAliveEntity>().Object,
                new NrunPacket
                {
                    Runner = NrunRunnerType.SetPlaceOfRevival,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 1,
                });

        private void TheQuestionShouldBeAskedInTheCharactersLanguage()
        {
            var qna = _session.LastPackets.OfType<QnaPacket>().LastOrDefault();
            Assert.IsNotNull(qna);
            var yes = qna.YesPacket as NrunPacket;
            Assert.IsNotNull(yes);
            Assert.AreEqual(NrunRunnerType.SetPlaceOfRevival, yes.Runner);
            Assert.AreEqual(VisualType.Npc, yes.VisualType);
            Assert.AreEqual(100, yes.VisualId);
            Assert.IsFalse(qna.Question?.Contains("n_run", System.StringComparison.Ordinal) ?? true);
        }

        private void RespawnPointShouldNotHaveBeenUpdated() =>
            _respawnService.Verify(x => x.SetRespawnPoint(It.IsAny<PlayerComponentBundle>(),
                It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()), Times.Never);

        private void NoConfirmationMsgShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
    }
}
