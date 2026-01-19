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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class TeleportPacketHandlerTests
    {
        private TeleportPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IMapChangeService> MapChangeService = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            MapChangeService = new Mock<IMapChangeService>();

            Handler = new TeleportPacketHandler(
                Logger,
                MapChangeService.Object,
                TestHelpers.Instance.GameLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task TeleportingToPlayerShouldChangeMapInstance()
        {
            await new Spec("Teleporting to player should change map instance")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnline)
                .WhenAsync(TeleportingToPlayer)
                .Then(MapChangeServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToMapByIdShouldChangeMap()
        {
            await new Spec("Teleporting to map by id should change map")
                .Given(CharacterIsOnMap)
                .WhenAsync(TeleportingToMapById)
                .Then(MapChangeByIdShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TeleportingToUnknownPlayerShouldLogError()
        {
            await new Spec("Teleporting to unknown player should log error")
                .Given(CharacterIsOnMap)
                .WhenAsync(TeleportingToUnknownPlayer)
                .Then(NoMapChangeShouldOccur)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetIsOnline()
        {
            TargetSession.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task TeleportingToPlayer()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = TargetSession.Character.Name
            }, Session);
        }

        private async Task TeleportingToMapById()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = "1",
                MapX = 50,
                MapY = 50
            }, Session);
        }

        private async Task TeleportingToUnknownPlayer()
        {
            await Handler.ExecuteAsync(new TeleportPacket
            {
                TeleportArgument = "UnknownPlayerName123"
            }, Session);
        }

        private void MapChangeServiceShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<System.Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()), Times.Once);
        }

        private void MapChangeByIdShouldBeCalled()
        {
            MapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<short?>(),
                It.IsAny<short?>(),
                It.IsAny<short?>()), Times.Once);
        }

        private void NoMapChangeShouldOccur()
        {
            MapChangeService.Verify(x => x.ChangeMapInstanceAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<System.Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()), Times.Never);
            MapChangeService.Verify(x => x.ChangeMapAsync(
                It.IsAny<ClientSession>(),
                It.IsAny<short?>(),
                It.IsAny<short?>(),
                It.IsAny<short?>()), Times.Never);
        }
    }
}
