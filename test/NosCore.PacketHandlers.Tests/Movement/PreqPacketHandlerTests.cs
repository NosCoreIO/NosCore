//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Arch.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private PreqPacketHandler? _preqPacketHandler;
        private ClientSession? _session;
        private Mock<IMapChangeService>? _mapChangeService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _mapChangeService = new Mock<IMapChangeService>();
            _preqPacketHandler =
                new PreqPacketHandler(TestHelpers.Instance.MapInstanceAccessorService, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock, _mapChangeService.Object);
            var sourceMapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _session.ChangeMapInstance(sourceMapInstance);
            var destMapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            var portalEntity = sourceMapInstance.EcsWorld.CreatePortal(
                0, 0, 0, 0, 5, 5, 1, Packets.Enumerations.PortalType.MapPortal, false,
                sourceMapInstance.MapInstanceId, destMapInstance.MapInstanceId);
            sourceMapInstance.Portals = new List<Entity> { portalEntity };
        }

        [TestMethod]
        public async Task UserCanUsePortalAsync()
        {
            _session!.Player.SetPosition(0, 0);
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);

            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Once);
        }

        [TestMethod]
        public async Task UserLastPortalSetOnUsageAsync()
        {
            var player = _session!.Player;
            player.SetPosition(0, 0);

            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            Assert.IsTrue(_session.Player.LastPortal == time);
        }

        [TestMethod]
        public async Task UserCanTUsePortalIfRecentlyMovedAsync()
        {
            var player = _session!.Player;
            player.SetPosition(0, 0);
            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            player.LastPortal = time.Plus(Duration.FromMinutes(1));
            player.LastMove = time;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }

        [TestMethod]
        public async Task UserCanTUsePortalIfRecentlyUsedAsync()
        {
            var player = _session!.Player;
            player.SetPosition(0, 0);
            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            player.LastPortal = time;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }


        [TestMethod]
        public async Task UserCanTUsePortalIfTooFarAsync()
        {
            _session!.Player.SetPosition(8, 8);
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }

        [TestMethod]
        public async Task UserFromInstanceGoesBackToOriginePlaceAsync()
        {
            var player = _session!.Player;
            player.MapX = 5;
            player.MapY = 5;
            player.SetPosition(0, 0);
            player.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);

            _mapChangeService!.Verify(x => x.ChangeMapAsync(_session, 1, 5, 5), Times.Once);
        }
    }
}