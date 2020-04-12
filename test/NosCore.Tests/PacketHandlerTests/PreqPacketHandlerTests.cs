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
using NosCore.Packets.ClientPackets.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.Movement;
using NosCore.Tests.Helpers;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class PreqPacketHandlerTests
    {
        private Mock<IMinilandProvider>? _minilandProvider;
        private PreqPacketHandler? _preqPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _minilandProvider = new Mock<IMinilandProvider>();
            _minilandProvider.Setup(s => s.GetMinilandPortals(It.IsAny<long>())).Returns(new List<Portal>());
            _preqPacketHandler =
                new PreqPacketHandler(TestHelpers.Instance.MapInstanceProvider, _minilandProvider.Object, TestHelpers.Instance.DistanceHelper);
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceProvider.GetBaseMapById(0);

            _session.Character.MapInstance.Portals = new List<Portal>
            {
                new Portal
                {
                    DestinationMapId = 1,
                    DestinationMapInstanceId = TestHelpers.Instance.MapInstanceProvider.GetBaseMapInstanceIdByMapId(1),
                    DestinationX = 5, DestinationY = 5, SourceMapId = 0,
                    SourceMapInstanceId = TestHelpers.Instance.MapInstanceProvider.GetBaseMapInstanceIdByMapId(0),
                    SourceX = 0, SourceY = 0
                }
            };
        }

        [TestMethod]
        public async Task UserCanUsePortalAsync()
        {
            _session!.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            Assert.IsTrue((_session.Character.PositionY == 5) && (_session.Character.PositionX == 5) &&
                (_session.Character.MapInstance.Map.MapId == 1));
        }

        [TestMethod]
        public async Task UserCanTUsePortalIfTooFarAsync()
        {
            _session!.Character.PositionX = 8;
            _session.Character.PositionY = 8;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            Assert.IsTrue((_session.Character.PositionY == 8) && (_session.Character.PositionX == 8) &&
                (_session.Character.MapInstance.Map.MapId == 0));
        }

        [TestMethod]
        public async Task UserFromInstanceGoesBackToOriginePlaceAsync()
        {
            _session!.Character.MapX = 5;
            _session.Character.MapY = 5;
            _session.Character.PositionX = 0;
            _session.Character.PositionY = 0;
            _session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
            await _preqPacketHandler!.ExecuteAsync(new PreqPacket(), _session).ConfigureAwait(false);
            Assert.IsTrue((_session.Character.PositionY == 5) && (_session.Character.PositionX == 5) &&
                (_session.Character.MapInstance.Map.MapId == 1));
        }
    }
}