//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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

#pragma warning disable 618

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class SetPlaceOfRevivalHandlerTests
    {
        private ClientSession _session = null!;
        private Mock<IRespawnService> _respawnService = null!;
        private SetPlaceOfRevivalHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _respawnService = new Mock<IRespawnService>();
            _handler = new SetPlaceOfRevivalHandler(_respawnService.Object);
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
                .WhenAsync(NrunIsHandledWithType_, (short)2)
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

        private void RunnerShouldBe_(NrunRunnerType expected) =>
            Assert.AreEqual(expected, _handler.Runner);

        private Task NrunIsHandledWithType_(short type) =>
            _handler.HandleAsync(_session, new Mock<IAliveEntity>().Object,
                new NrunPacket
                {
                    Runner = NrunRunnerType.SetPlaceOfRevival,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 1,
                });

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
            Assert.IsFalse(_session.LastPackets.OfType<MsgPacket>().Any());
    }
}
