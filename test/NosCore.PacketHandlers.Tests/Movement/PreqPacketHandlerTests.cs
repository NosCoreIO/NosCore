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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Tests.Shared;
using NosCore.GameObject.ComponentEntities.Entities;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private Mock<IMinilandService>? _minilandProvider;
        private PreqPacketHandler? _preqPacketHandler;
        private ClientSession? _session;
        private Mock<IMapChangeService>? _mapChangeService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _minilandProvider = new Mock<IMinilandService>();
            _mapChangeService = new Mock<IMapChangeService>();
            _minilandProvider.Setup(s => s.GetMinilandPortals(It.IsAny<long>())).Returns(new List<Portal>());
            _preqPacketHandler =
                new PreqPacketHandler(TestHelpers.Instance.MapInstanceAccessorService, _minilandProvider.Object, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock, _mapChangeService.Object);
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;

            _session.Character.MapInstance.Portals = new List<Portal>
            {
                new()
                {
                    DestinationMapId = 1,
                    DestinationMapInstanceId = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId,
                    DestinationX = 5, DestinationY = 5, SourceMapId = 0,
                    SourceMapInstanceId = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!.MapInstanceId,
                    SourceX = 0, SourceY = 0
                }
            };
        }

        [TestMethod]
        public async Task UserCanUsePortalAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);

            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Once);
        }

        [TestMethod]
        public async Task UserLastPortalSetOnUsageAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;

            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);
            Assert.IsTrue(_session.Character.LastPortal == time);
        }

        [TestMethod]
        public async Task UserCanTUsePortalIfRecentlyMovedAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            _session.Character.LastPortal = time.Plus(Duration.FromMinutes(1));
            _session.Character.LastMove = time;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }

        [TestMethod]
        public async Task UserCanTUsePortalIfRecentlyUsedAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            var time = TestHelpers.Instance.Clock.GetCurrentInstant();
            _session.Character.LastPortal = time;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }


        [TestMethod]
        public async Task UserCanTUsePortalIfTooFarAsync()
        {
            _session!.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);
            _mapChangeService!.Verify(x => x.ChangeMapInstanceAsync(_session, TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!.MapInstanceId, 5, 5), Times.Never);
        }

        [TestMethod]
        public async Task UserFromInstanceGoesBackToOriginePlaceAsync()
        {
            _session!.Character.MapX = 5;
            _session.Character.MapY = 5;
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session);

            _mapChangeService!.Verify(x => x.ChangeMapAsync(_session, 1, 5, 5), Times.Once);
        }
    }
}