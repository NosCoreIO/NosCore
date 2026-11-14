//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ChannelService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Login;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ChannelService
{
    [TestClass]
    public class ChannelServiceTests
    {
        private TestWorld World;
        private IChannelService Service;
        private Mock<IAuthHub> AuthHub;
        private Mock<IChannelHub> ChannelHub;
        private Mock<ISaveService> SaveService;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);

            AuthHub = World.Freeze<Mock<IAuthHub>>();
            ChannelHub = World.Freeze<Mock<IChannelHub>>();
            SaveService = World.Freeze<Mock<ISaveService>>();

            SaveService.Setup(s => s.SaveAsync(It.IsAny<ClientSession>()))
                .Returns(Task.CompletedTask);

            Service = World.Create<GameObject.Services.ChannelService.ChannelService>();
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
        public async Task MovingToExistingChannelShouldAwaitTheAccountOnThatChannelOnly()
        {
            await new Spec("Moving to existing channel should await the account on that channel only")
                .Given(TargetChannelExists)
                .And(MfaIsValidated)
                .WhenAsync(MovingToChannel)
                .Then(ChannelChangeShouldBeAwaitedOnTargetChannel)
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

        [TestMethod]
        public async Task AnOrdinaryChannelSendsGlacernonToTheAct4Channel()
        {
            await new Spec("An ordinary channel sends Glacernon to the Act 4 channel")
                .Given(BothKindsOfChannelRun)
                .WhenAsync(AskingWhoHostsGlacernon)
                .Then(TheHostShouldBe_, 51L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOrdinaryChannelKeepsTheOtherMaps()
        {
            await new Spec("An ordinary channel keeps the other maps")
                .Given(BothKindsOfChannelRun)
                .WhenAsync(AskingWhoHostsAnOrdinaryMap)
                .Then(ThisChannelShouldKeepThePlayer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutAnAct4ChannelGlacernonStaysHere()
        {
            await new Spec("Without an Act 4 channel Glacernon stays here")
                .Given(TargetChannelExists)
                .WhenAsync(AskingWhoHostsGlacernon)
                .Then(ThisChannelShouldKeepThePlayer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAct4ChannelOfAnotherServerIsNotUsed()
        {
            await new Spec("An Act 4 channel of another server is not used")
                .Given(OnlyAnotherServerHasAnAct4Channel)
                .WhenAsync(AskingWhoHostsGlacernon)
                .Then(ThisChannelShouldKeepThePlayer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAct4ChannelSendsOtherMapsToTheFirstOrdinaryChannel()
        {
            await new Spec("The Act 4 channel sends other maps to the first ordinary channel")
                .Given(ThisIsTheAct4Channel)
                .And(BothKindsOfChannelRun)
                .WhenAsync(AskingWhoHostsAnOrdinaryMap)
                .Then(TheHostShouldBe_, 2L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheAct4ChannelKeepsGlacernon()
        {
            await new Spec("The Act 4 channel keeps Glacernon")
                .Given(ThisIsTheAct4Channel)
                .And(BothKindsOfChannelRun)
                .WhenAsync(AskingWhoHostsGlacernon)
                .Then(ThisChannelShouldKeepThePlayer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingToAMapOnAnotherChannelSavesThePlayerThere()
        {
            await new Spec("Moving to a map on another channel saves the player there")
                .Given(BothKindsOfChannelRun)
                .WhenAsync(SailingToTheAngelCitadel)
                .Then(TheCharacterShouldStandInTheAngelCitadel)
                .And(TheClientShouldShowTheCrossing)
                .And(CharacterShouldBeSaved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GlacernonOnAnOrdinaryChannelSendsThePlayerToTheAct4Channel()
        {
            await new Spec("Glacernon on an ordinary channel sends the player to the Act 4 channel")
                .Given(BothKindsOfChannelRun)
                .WhenAsync(EnteringGlacernon)
                .Then(ThePlayerShouldHaveMoved)
                .And(TheClientShouldShow_, ItModeType.ToAct4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnInstanceNeverLeavesTheChannel()
        {
            await new Spec("An instance never leaves the channel")
                .Given(BothKindsOfChannelRun)
                .WhenAsync(EnteringAnAct4Instance)
                .Then(ThePlayerShouldHaveStayed)
                .ExecuteAsync();
        }

        private bool Moved;

        private async Task EnteringGlacernon()
        {
            var map = World.GetBaseMap(1);
            map.IsAct4 = true;
            Moved = await Service.TryMoveToHostAsync(Session, map, 12, 38);
        }

        private async Task EnteringAnAct4Instance()
        {
            var map = World.GetBaseMap(1);
            map.IsAct4 = true;
            map.MapInstanceType = MapInstanceType.NormalInstance;
            Moved = await Service.TryMoveToHostAsync(Session, map, 12, 38);
        }

        private void ThePlayerShouldHaveMoved()
        {
            Assert.IsTrue(Moved);
        }

        private void ThePlayerShouldHaveStayed()
        {
            Assert.IsFalse(Moved);
            Assert.IsFalse(Session.LastPackets.OfType<MzPacket>().Any());
        }

        private void TheClientShouldShow_(ItModeType mode)
        {
            Assert.AreEqual(mode, Session.LastPackets.OfType<ItPacket>().Single().Mode);
        }

        private ChannelInfo? Host;

        private void ThisIsTheAct4Channel()
        {
            World.WorldConfiguration.Value.Act4Channel = true;
        }

        private void BothKindsOfChannelRun()
        {
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new() { Id = 3, Type = ServerType.WorldServer, Host = "127.0.0.1", Port = 1339 },
                    new() { Id = 51, Type = ServerType.WorldServer, Host = "127.0.0.1", Port = 1338, IsAct4 = true },
                    new() { Id = 2, Type = ServerType.WorldServer, Host = "127.0.0.1", Port = 1337 },
                });
        }

        private void OnlyAnotherServerHasAnAct4Channel()
        {
            World.WorldConfiguration.Value.ServerId = 1;
            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new() { Id = 2, ServerId = 1, Type = ServerType.WorldServer, Host = "127.0.0.1", Port = 1337 },
                    new() { Id = 51, ServerId = 2, Type = ServerType.WorldServer, Host = "127.0.0.1", Port = 1338, IsAct4 = true },
                });
        }

        private async Task AskingWhoHostsGlacernon()
        {
            Host = await Service.GetChannelHostingAsync(true);
        }

        private async Task AskingWhoHostsAnOrdinaryMap()
        {
            Host = await Service.GetChannelHostingAsync(false);
        }

        private void TheHostShouldBe_(long id)
        {
            Assert.AreEqual(id, Host?.Id);
        }

        private void ThisChannelShouldKeepThePlayer()
        {
            Assert.IsNull(Host);
        }

        private async Task SailingToTheAngelCitadel()
        {
            var act4 = (await ChannelHub.Object.GetCommunicationChannels()).Single(c => c.IsAct4);
            await Service.MoveToMapAsync(Session, act4, ItModeType.ShipToAct4, 130, 12, 38);
        }

        private void TheCharacterShouldStandInTheAngelCitadel()
        {
            Assert.AreEqual((short)130, Session.Character.MapId);
            Assert.AreEqual((short)12, Session.Character.MapX);
            Assert.AreEqual((short)38, Session.Character.MapY);
        }

        private void TheClientShouldShowTheCrossing()
        {
            Assert.AreEqual(1338, Session.LastPackets.OfType<MzPacket>().Single().Port);
            Assert.AreEqual(ItModeType.ShipToAct4, Session.LastPackets.OfType<ItPacket>().Single().Mode);
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

        private void MfaIsValidated()
        {
            Session.MfaValidated = true;
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
            SaveService.Verify(s => s.SaveAsync(Session), Times.Once);
        }

        private void ChannelChangeShouldBeAwaitedOnTargetChannel()
        {
            AuthHub.Verify(s => s.SetAwaitingChannelChangeAsync(Session.Account.Name, 2, true), Times.Once);
        }

        private void NoPacketsShouldBeSent()
        {
            var mzPacket = Session.LastPackets.OfType<MzPacket>().FirstOrDefault();
            Assert.IsNull(mzPacket);
        }

        private void CharacterShouldNotBeSaved()
        {
            SaveService.Verify(s => s.SaveAsync(It.IsAny<ClientSession>()), Times.Never);
        }
    }
}
