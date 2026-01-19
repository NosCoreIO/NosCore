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
using NodaTime;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.PathFinder.Interfaces;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class WalkPacketHandlerTests
    {
        private WalkPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IHeuristic> DistanceCalculator = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            DistanceCalculator = new Mock<IHeuristic>();
            DistanceCalculator.Setup(x => x.GetDistance(It.IsAny<(short, short)>(), It.IsAny<(short, short)>()))
                .Returns(1);

            Handler = new WalkPacketHandler(
                DistanceCalculator.Object,
                Logger,
                TestHelpers.Instance.Clock,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task WalkWithHighSpeedShouldBeIgnored()
        {
            await new Spec("Walk with high speed should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingWithHighSpeed)
                .Then(PositionShouldNotBeUpdated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WalkShouldExecuteWithoutError()
        {
            await new Spec("Walk should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(WalkingToValidPosition)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private short InitialX;
        private short InitialY;

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.PositionX = 10;
            Session.Character.PositionY = 10;
            InitialX = Session.Character.PositionX;
            InitialY = Session.Character.PositionY;
        }

        private async Task WalkingToValidPosition()
        {
            var checksum = ((11 + 12) % 3) % 2;
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 11,
                YCoordinate = 12,
                Speed = 20,
                CheckSum = (byte)checksum
            }, Session);
        }

        private async Task WalkingWithHighSpeed()
        {
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 11,
                YCoordinate = 12,
                Speed = 100,
                CheckSum = 0
            }, Session);
        }

        private async Task WalkingWithInvalidChecksum()
        {
            await Handler.ExecuteAsync(new WalkPacket
            {
                XCoordinate = 11,
                YCoordinate = 12,
                Speed = 20,
                CheckSum = 99
            }, Session);
        }

        private void PositionShouldBeUpdated()
        {
            Assert.AreEqual(11, Session.Character.PositionX);
            Assert.AreEqual(12, Session.Character.PositionY);
        }

        private void PositionShouldNotBeUpdated()
        {
            Assert.AreEqual(InitialX, Session.Character.PositionX);
            Assert.AreEqual(InitialY, Session.Character.PositionY);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
