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
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Nrun;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class ProbabilityUIsHandlerTests
    {
        private ClientSession _session = null!;
        private ProbabilityUIsHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new ProbabilityUIsHandler();
        }

        [TestMethod]
        public async Task ProbabilityUIsRunnerOpensTheWindowCarriedInPacketType()
        {
            await new Spec("n_run 12 with Type=8 opens wopen 8 0 (sum window)")
                .WhenAsync(NrunProbabilityUIsIsHandledWithType_, (short)WindowType.SumResistance)
                .Then(WopenWithType_ShouldHaveBeenSent, WindowType.SumResistance)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProbabilityUIsRunnerOpensRarifyWindowWhenTypeIsSeven()
        {
            await new Spec("n_run 12 with Type=7 opens wopen 7 0 (rarify window)")
                .WhenAsync(NrunProbabilityUIsIsHandledWithType_, (short)WindowType.RarifyItem)
                .Then(WopenWithType_ShouldHaveBeenSent, WindowType.RarifyItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProbabilityUIsRunnerWithNullTypeIsIgnored()
        {
            await new Spec("ProbabilityUIs handler ignores events with null Type")
                .WhenAsync(NrunProbabilityUIsWithNullTypeIsHandled)
                .Then(NoWopenShouldHaveBeenSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherRunnerTypesAreIgnored()
        {
            await new Spec("ProbabilityUIs handler ignores other runner types")
                .WhenAsync(NrunUpgradeItemIsHandled)
                .Then(NoWopenShouldHaveBeenSent)
                .ExecuteAsync();
        }

        private Task NrunProbabilityUIsIsHandledWithType_(short type) =>
            _handler.Handle(new NrunRequestedEvent(_session,
                new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = NrunRunnerType.ProbabilityUIs, Type = type }));

        private Task NrunProbabilityUIsWithNullTypeIsHandled() =>
            _handler.Handle(new NrunRequestedEvent(_session,
                new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = NrunRunnerType.ProbabilityUIs, Type = null }));

        private Task NrunUpgradeItemIsHandled() =>
            _handler.Handle(new NrunRequestedEvent(_session,
                new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = NrunRunnerType.UpgradeItem }));

        private void WopenWithType_ShouldHaveBeenSent(WindowType expected)
        {
            var wopen = _session.LastPackets.OfType<WopenPacket>().LastOrDefault();
            Assert.IsNotNull(wopen);
            Assert.AreEqual(expected, wopen.Type);
        }

        private void NoWopenShouldHaveBeenSent() =>
            Assert.IsFalse(_session.LastPackets.OfType<WopenPacket>().Any());
    }
}
