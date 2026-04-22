//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.PacketHandlers.Upgrades;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Upgrades
{
    [TestClass]
    public class UpgradePacketHandlerTests
    {
        private ClientSession _session = null!;
        private Mock<IUpgradeOperation> _matchingOperation = null!;
        private Mock<IUpgradeOperation> _otherOperation = null!;
        private Mock<ILogger> _logger = null!;
        private UpgradePacketHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _matchingOperation = new Mock<IUpgradeOperation>();
            _matchingOperation.Setup(o => o.Kind).Returns(UpgradePacketType.SumResistance);
            _matchingOperation.Setup(o => o.ExecuteAsync(It.IsAny<ClientSession>(), It.IsAny<UpgradePacket>()))
                .ReturnsAsync((IReadOnlyList<IPacket>)new IPacket[0]);
            _otherOperation = new Mock<IUpgradeOperation>();
            _otherOperation.Setup(o => o.Kind).Returns(UpgradePacketType.RarifyItem);
            _logger = new Mock<ILogger>();
            _handler = new UpgradePacketHandler(
                new[] { _otherOperation.Object, _matchingOperation.Object },
                _logger.Object,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task DispatchesToOperationWithMatchingKind()
        {
            await new Spec("Dispatches to the operation whose Kind matches the packet's UpgradeType")
                .WhenAsync(SumResistancePacketIsHandled)
                .Then(MatchingOperationShouldHaveExecutedExactlyOnce)
                .And(OtherOperationShouldNotHaveExecuted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnknownUpgradeTypeIsLoggedAndIgnored()
        {
            await new Spec("Unknown UpgradeType logs a warning and dispatches to nothing")
                .WhenAsync(UnknownUpgradeTypePacketIsHandled)
                .Then(NoOperationShouldHaveExecuted)
                .And(WarningShouldHaveBeenLogged)
                .ExecuteAsync();
        }

        // --- Whens ---

        private Task SumResistancePacketIsHandled() =>
            _handler.ExecuteAsync(new UpgradePacket
            {
                UpgradeType = UpgradePacketType.SumResistance,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            }, _session);

        private Task UnknownUpgradeTypePacketIsHandled() =>
            _handler.ExecuteAsync(new UpgradePacket
            {
                UpgradeType = UpgradePacketType.FusionItem,
                InventoryType = PocketType.Equipment,
                Slot = 0,
            }, _session);

        // --- Thens ---

        private void MatchingOperationShouldHaveExecutedExactlyOnce() =>
            _matchingOperation.Verify(o => o.ExecuteAsync(_session, It.IsAny<UpgradePacket>()), Times.Once);

        private void OtherOperationShouldNotHaveExecuted() =>
            _otherOperation.Verify(o => o.ExecuteAsync(It.IsAny<ClientSession>(), It.IsAny<UpgradePacket>()), Times.Never);

        private void NoOperationShouldHaveExecuted()
        {
            _matchingOperation.Verify(o => o.ExecuteAsync(It.IsAny<ClientSession>(), It.IsAny<UpgradePacket>()), Times.Never);
            _otherOperation.Verify(o => o.ExecuteAsync(It.IsAny<ClientSession>(), It.IsAny<UpgradePacket>()), Times.Never);
        }

        private void WarningShouldHaveBeenLogged() =>
            _logger.Verify(l => l.Warning(It.IsAny<string>(), It.IsAny<UpgradePacketType>()), Times.AtLeastOnce);
    }
}
