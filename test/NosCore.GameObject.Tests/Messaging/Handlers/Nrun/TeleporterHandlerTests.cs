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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class TeleporterHandlerTests
    {
        private ClientSession _session = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private TeleporterHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _mapChangeService = new Mock<IMapChangeService>();
            _handler = new TeleporterHandler(_mapChangeService.Object);
        }

        [TestMethod]
        public async Task RunnerPropertyMatchesTeleport()
        {
            await new Spec("TeleporterHandler declares NrunRunnerType.Teleport")
                .Then(RunnerShouldBe_, NrunRunnerType.Teleport)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("A null target is a no-op: no gold removed, no map change, no message")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(NrunIsHandledWithType_AndNullTarget, (short)1)
                .Then(CharacterGoldShouldStillBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .And(NoNotEnoughGoldShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NonNpcTargetIsIgnored()
        {
            await new Spec("A target that is not an NpcComponentBundle short-circuits before any gold/map logic")
                .Given(CharacterGoldIs_, 10_000L)
                .WhenAsync(NrunIsHandledWithType_AndGenericAliveTarget, (short)1)
                .Then(CharacterGoldShouldStillBe_, 10_000L)
                .And(NoMapChangeShouldHaveBeenInvoked)
                .ExecuteAsync();
        }

        private void RunnerShouldBe_(NrunRunnerType expected) =>
            Assert.AreEqual(expected, _handler.Runner);

        private void CharacterGoldIs_(long gold) =>
            _session.Character.Gold = gold;

        private Task NrunIsHandledWithType_AndNullTarget(short type) =>
            _handler.HandleAsync(_session, null,
                new NrunPacket
                {
                    Runner = NrunRunnerType.Teleport,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 1,
                });

        private Task NrunIsHandledWithType_AndGenericAliveTarget(short type) =>
            _handler.HandleAsync(_session, new Mock<IAliveEntity>().Object,
                new NrunPacket
                {
                    Runner = NrunRunnerType.Teleport,
                    Type = type,
                    VisualType = VisualType.Npc,
                    VisualId = 1,
                });

        private void CharacterGoldShouldStillBe_(long expected) =>
            Assert.AreEqual(expected, _session.Character.Gold);

        private void NoMapChangeShouldHaveBeenInvoked() =>
            _mapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()),
                Times.Never);

        private void NoNotEnoughGoldShouldBeSent()
        {
            var say = _session.LastPackets.OfType<SayiPacket>()
                .FirstOrDefault(p => p.Message == Game18NConstString.NotEnoughGold);
            Assert.IsNull(say);
        }
    }
}
