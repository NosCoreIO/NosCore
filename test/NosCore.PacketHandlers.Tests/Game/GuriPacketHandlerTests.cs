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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GuriRunnerService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class GuriPacketHandlerTests
    {
        private GuriPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IGuriRunnerService> GuriRunnerService = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            GuriRunnerService = new Mock<IGuriRunnerService>();
            Handler = new GuriPacketHandler(GuriRunnerService.Object);
        }

        [TestMethod]
        public void HandlerCanBeConstructed()
        {
            new Spec("Handler can be constructed")
                .Then(HandlerShouldNotBeNull)
                .Execute();
        }

        [TestMethod]
        public async Task GuriPacketShouldCallGuriRunnerService()
        {
            await new Spec("Guri packet should call guri runner service")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacket)
                .Then(GuriRunnerServiceShouldBeCalled)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GuriPacketWithTypeShouldPassCorrectData()
        {
            await new Spec("Guri packet with type should pass correct data")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingGuriPacketWithType)
                .Then(GuriRunnerServiceShouldBeCalledWithCorrectType)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
        }

        private async Task ExecutingGuriPacket()
        {
            await Handler.ExecuteAsync(new GuriPacket(), Session);
        }

        private async Task ExecutingGuriPacketWithType()
        {
            await Handler.ExecuteAsync(new GuriPacket { Type = GuriPacketType.Title }, Session);
        }

        private void HandlerShouldNotBeNull()
        {
            Assert.IsNotNull(Handler);
        }

        private void GuriRunnerServiceShouldBeCalled()
        {
            GuriRunnerService.Verify(x => x.GuriLaunch(
                It.IsAny<ClientSession>(),
                It.IsAny<GuriPacket>()), Times.Once);
        }

        private void GuriRunnerServiceShouldBeCalledWithCorrectType()
        {
            GuriRunnerService.Verify(x => x.GuriLaunch(
                It.IsAny<ClientSession>(),
                It.Is<GuriPacket>(p => p.Type == GuriPacketType.Title)), Times.Once);
        }
    }
}
