//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
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
            _session = await World.GenerateSessionAsync();
            _respawnService = new Mock<IRespawnService>();
            _handler = new SetPlaceOfRevivalHandler(_respawnService.Object, World.NpcDialogService);
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesSetPlaceOfRevival()
        {
            await new Spec("SetPlaceOfRevivalHandler declares NrunRunnerType.SetPlaceOfRevival")
                .Then(RunnerShouldBe_, NrunRunnerType.SetPlaceOfRevival)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TypeTwoSendsConfirmationDialog()
        {
            await new Spec("n_run Type=2 prompts a Qna dialog whose YesPacket is the Type=1 commit")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .WhenAsync(NrunIsHandledWithType_OnThatNpc, (short)2)
                .Then(QnaWithYesPacketTypeOneShouldHaveBeenSent)
                .And(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("Type=1 with null target is a no-op: no respawn update, no confirmation msg")
                .WhenAsync(NrunIsHandledWithType_AndNullTarget, (short)1)
                .Then(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonNpcTargetIsIgnored()
        {
            await new Spec("Type=1 with a non-NpcComponentBundle target is a no-op")
                .WhenAsync(NrunIsHandledWithType_AndGenericAliveTarget, (short)1)
                .Then(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TypeOneOnAnNpcSavesTheRespawnPoint()
        {
            await new Spec("Type=1 on an NPC saves its position as the respawn point and confirms with the client's own message")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .AndAsync(NrunIsHandledWithType_OnThatNpc, (short)2)
                .WhenAsync(NrunIsHandledWithType_OnThatNpc, (short)1)
                .Then(RespawnPointShouldBeSetTo_And_, (short)5, (short)6)
                .And(RevivalPlaceChangedShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingWithoutTheQuestionIsIgnored()
        {
            await new Spec("Type=1 without the NPC having asked the question saves nothing")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .WhenAsync(NrunIsHandledWithType_OnThatNpc, (short)1)
                .Then(RespawnPointShouldNotHaveBeenUpdated)
                .And(NoConfirmationMsgShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmingTwiceSavesOnce()
        {
            await new Spec("One question allows one confirmation")
                .Given(TheCharacterStandsNextToAnNpcAt_And_, (short)5, (short)6)
                .AndAsync(NrunIsHandledWithType_OnThatNpc, (short)2)
                .WhenAsync(NrunIsHandledWithType_OnThatNpc, (short)1)
                .AndAsync(NrunIsHandledWithType_OnThatNpc, (short)1)
                .Then(RespawnPointShouldBeSetTo_And_, (short)5, (short)6)
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

        private Task NrunIsHandledWithType_OnThatNpc(short type) =>
            _handler.HandleAsync(_session, _session.Character.MapInstance.GetNpcById(100),
                new NrunPacket
                {
                    Runner = NrunRunnerType.SetPlaceOfRevival,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 100,
                });

        private void RespawnPointShouldBeSetTo_And_(short x, short y)
        {
            var mapId = _session.Character.MapInstance.Map.MapId;
            _respawnService.Verify(r => r.SetRespawnPoint(It.IsAny<ICharacterEntity>(), mapId, x, y), Times.Once);
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

        private void QnaWithYesPacketTypeOneShouldHaveBeenSent()
        {
            var qna = _session.LastPackets.OfType<QnaPacket>().LastOrDefault();
            Assert.IsNotNull(qna);
            var yes = qna.YesPacket as NrunPacket;
            Assert.IsNotNull(yes);
            Assert.AreEqual(NrunRunnerType.SetPlaceOfRevival, yes.Runner);
            Assert.AreEqual<short?>(1, yes.Type);
        }

        private void RespawnPointShouldNotHaveBeenUpdated() =>
            _respawnService.Verify(x => x.SetRespawnPoint(It.IsAny<ICharacterEntity>(),
                It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()), Times.Never);

        private void NoConfirmationMsgShouldBeSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
    }
}
