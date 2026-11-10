//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class EscapePacketHandlerTests
    {
        private ClientSession _session = null!;
        private Mock<IScriptedInstanceService> _scriptedInstances = null!;
        private Mock<IMapChangeService> _mapChange = null!;
        private EscapePacketHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _scriptedInstances = new Mock<IScriptedInstanceService>();
            _mapChange = new Mock<IMapChangeService>();
            _handler = new EscapePacketHandler(_scriptedInstances.Object, _mapChange.Object);
        }

        [TestMethod]
        public async Task EscapingATimeSpaceReturnsToItsEntrance()
        {
            var entrance = new ScriptedInstance { MapId = 1, PositionX = 32, PositionY = 74 };
            var mapInstanceId = _session.Character.MapInstanceId;
            _scriptedInstances.Setup(s => s.GetRun(mapInstanceId))
                .Returns(new ScriptedInstanceRun(entrance, new Dictionary<int, Guid>(), Instant.MinValue));

            await _handler.ExecuteAsync(new EscapePacket(), _session);

            _mapChange.Verify(m => m.ChangeMapAsync(_session, (short?)1, (short?)32, (short?)74), Times.Once);
        }

        [TestMethod]
        public async Task EscapingOutsideAnInstanceDoesNothing()
        {
            await _handler.ExecuteAsync(new EscapePacket(), _session);

            _mapChange.VerifyNoOtherCalls();
        }
    }
}
