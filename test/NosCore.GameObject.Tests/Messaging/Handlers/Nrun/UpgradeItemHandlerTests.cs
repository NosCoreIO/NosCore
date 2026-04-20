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
    public class UpgradeItemHandlerTests
    {
        private ClientSession _session = null!;
        private UpgradeItemHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _handler = new UpgradeItemHandler();
        }

        [TestMethod]
        public async Task UpgradeItemRunnerOpensWopenWindowOne()
        {
            await new Spec("n_run 2 (UpgradeItem) opens wopen 1 0")
                .WhenAsync(NrunUpgradeItemIsHandled)
                .Then(WopenWithType_ShouldHaveBeenSent, WindowType.UpgradeItem)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherRunnerTypesAreIgnored()
        {
            await new Spec("UpgradeItem handler ignores other runner types")
                .WhenAsync(NrunBazaarIsHandled)
                .Then(NoWopenShouldHaveBeenSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NullTargetIsIgnored()
        {
            await new Spec("UpgradeItem handler ignores events with null target")
                .WhenAsync(NrunUpgradeItemWithNullTargetIsHandled)
                .Then(NoWopenShouldHaveBeenSent)
                .ExecuteAsync();
        }

        private Task NrunUpgradeItemIsHandled() =>
            _handler.Handle(new NrunRequestedEvent(_session,
                new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = NrunRunnerType.UpgradeItem }));

        private Task NrunUpgradeItemWithNullTargetIsHandled() =>
            _handler.Handle(new NrunRequestedEvent(_session, null,
                new NrunPacket { Runner = NrunRunnerType.UpgradeItem }));

        private Task NrunBazaarIsHandled() =>
            _handler.Handle(new NrunRequestedEvent(_session,
                new Mock<IAliveEntity>().Object,
                new NrunPacket { Runner = NrunRunnerType.OpenNosBazaar }));

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
