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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ChannelService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ChannelService
{
    [TestClass]
    public class ChannelServiceTests
    {
        private IChannelService Service = null!;
        private Mock<IAuthHub> AuthHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private Mock<ISaveService> SaveService = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;

            AuthHub = new Mock<IAuthHub>();
            ChannelHub = new Mock<IChannelHub>();
            SaveService = new Mock<ISaveService>();

            SaveService.Setup(s => s.SaveAsync(It.IsAny<ComponentEntities.Interfaces.ICharacterEntity>()))
                .Returns(Task.CompletedTask);

            Service = new GameObject.Services.ChannelService.ChannelService(
                AuthHub.Object,
                ChannelHub.Object,
                SaveService.Object);
        }

        [TestMethod]
        public async Task MovingToExistingChannelShouldSendPackets()
        {
            await new Spec("Moving to existing channel should send packets")
                .Given(TargetChannelExists)
                .WhenAsync(MovingToChannel)
                .Then(MzPacketShouldBeSent)
                .And(CharacterShouldBeSaved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingToNonExistentChannelShouldDoNothing()
        {
            await new Spec("Moving to non-existent channel should do nothing")
                .Given(NoChannelsExist)
                .WhenAsync(MovingToChannel)
                .Then(NoPacketsShouldBeSent)
                .And(CharacterShouldNotBeSaved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingToNonWorldServerShouldDoNothing()
        {
            await new Spec("Moving to non-world server should do nothing")
                .Given(OnlyLoginServerExists)
                .WhenAsync(MovingToChannel)
                .Then(NoPacketsShouldBeSent)
                .ExecuteAsync();
        }

        private void TargetChannelExists()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo
                    {
                        Id = 2,
                        Type = ServerType.WorldServer,
                        Host = "127.0.0.1",
                        Port = 1234
                    }
                });
        }

        private void NoChannelsExist()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>());
        }

        private void OnlyLoginServerExists()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo
                    {
                        Id = 2,
                        Type = ServerType.LoginServer,
                        Host = "127.0.0.1",
                        Port = 1234
                    }
                });
        }

        private async Task MovingToChannel()
        {
            await Service.MoveChannelAsync(Session, 2);
        }

        private void MzPacketShouldBeSent()
        {
            var mzPacket = Session.LastPackets.OfType<MzPacket>().FirstOrDefault();
            Assert.IsNotNull(mzPacket);
            Assert.AreEqual("127.0.0.1", mzPacket.Ip);
            Assert.AreEqual(1234, mzPacket.Port);
        }

        private void CharacterShouldBeSaved()
        {
            SaveService.Verify(s => s.SaveAsync(Session.Character), Times.Once);
        }

        private void NoPacketsShouldBeSent()
        {
            var mzPacket = Session.LastPackets.OfType<MzPacket>().FirstOrDefault();
            Assert.IsNull(mzPacket);
        }

        private void CharacterShouldNotBeSaved()
        {
            SaveService.Verify(s => s.SaveAsync(It.IsAny<ComponentEntities.Interfaces.ICharacterEntity>()), Times.Never);
        }
    }
}
