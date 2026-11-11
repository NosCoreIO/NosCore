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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Nrun
{
    [TestClass]
    public class ProbabilityWindowHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ProbabilityWindowHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var spUpgrade = new Mock<IUpgradeOperation>();
            spUpgrade.SetupGet(o => o.Kind).Returns(UpgradePacketType.UpgradeSpNoProtection);
            spUpgrade.SetupGet(o => o.Window).Returns(NpcDialogWindow.Upgrade);
            _handler = new ProbabilityWindowHandler(new[] { spUpgrade.Object }, World.NpcDialogService);
        }

        [TestMethod]
        public async Task TheWindowOfAHandledUpgradeOpens()
        {
            await RunAsync(9);

            Assert.AreEqual(WindowType.UpgradeSpNoProtection, _session.LastPackets.OfType<WopenPacket>().Single().Type);
        }

        [TestMethod]
        public async Task AWindowWithoutAHandledUpgradeStaysClosed()
        {
            await RunAsync(41);

            Assert.IsFalse(_session.LastPackets.OfType<WopenPacket>().Any());
        }

        [TestMethod]
        public async Task WithoutAnNpcNothingOpens()
        {
            await _handler.HandleAsync(_session, null, new NrunPacket { Runner = NrunRunnerType.ProbabilityUIs, Type = 9 });

            Assert.IsFalse(_session.LastPackets.OfType<WopenPacket>().Any());
        }

        [TestMethod]
        public async Task APlayerTakenForTheNpcOpensNothing()
        {
            await _handler.HandleAsync(_session, _session.Character,
                new NrunPacket { Runner = NrunRunnerType.ProbabilityUIs, Type = 9 });

            Assert.IsFalse(_session.LastPackets.OfType<WopenPacket>().Any());
        }

        private Task RunAsync(short type) => _handler.HandleAsync(_session, _session.WithNpcAt(1, 1, 1),
            new NrunPacket { Runner = NrunRunnerType.ProbabilityUIs, Type = type });
    }
}
