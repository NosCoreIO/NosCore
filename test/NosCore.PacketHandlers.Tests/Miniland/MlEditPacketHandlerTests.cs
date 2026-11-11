//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Map;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MlEditPacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<MlEditPacketHandlerTests> Logger = new Mock<ILogger<MlEditPacketHandlerTests>>().Object;
        private MlEditPacketHandler MlEditPacketHandler;
        private ClientSession Session;
        private IMinilandService MinilandProvider;
        private ClientSession Session2;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Session2 = await World.GenerateSessionAsync();
            var session3 = await World.GenerateSessionAsync();
            World.FriendHub
                .Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>
                {
                    new()
                    {
                        CharacterId = Session2.Character.CharacterId,
                        CharacterName = Session2.Character.Name,
                        IsConnected = true,
                        RelationType = CharacterRelationType.Friend,
                        CharacterRelationId = Guid.NewGuid()
                    }
                });
            await World.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = Session.Character.CharacterId,
            });
            MinilandProvider = new MinilandService(World.MapInstanceAccessorService,
                World.FriendHub.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                World.MinilandDao,
                World.MinilandObjectDao, new MinilandRegistry());
            await MinilandProvider.InitializeAsync(Session.Character, World.MapInstanceGeneratorService);
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            await World.MapChangeService.ChangeMapInstanceAsync(Session, miniland.MapInstanceId);
            await World.MapChangeService.ChangeMapInstanceAsync(Session2, miniland.MapInstanceId);
            await World.MapChangeService.ChangeMapInstanceAsync(session3, miniland.MapInstanceId);
            MlEditPacketHandler = new MlEditPacketHandler(MinilandProvider);
        }

        [TestMethod]
        public async Task CanChangeMinilandMessage()
        {
            await new Spec("Can change miniland message")
                .WhenAsync(ChangingMinilandMessageAsync)
                .Then(MinilandMessageShouldBeChanged)
                .And(InfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CanChangeMinilandMessageWithSpace()
        {
            await new Spec("Can change miniland message with space")
                .WhenAsync(ChangingMinilandMessageWithSpaceAsync)
                .Then(MinilandMessageWithSpaceShouldBeChanged)
                .And(InfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CanLockMiniland()
        {
            await new Spec("Can lock miniland")
                .WhenAsync(LockingMinilandAsync)
                .Then(MinilandShouldBeLocked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CanOpenMiniland()
        {
            await new Spec("Can open miniland")
                .WhenAsync(OpeningMinilandAsync)
                .Then(MinilandShouldBeOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CanSetMinilandPrivate()
        {
            await new Spec("Can set miniland private")
                .WhenAsync(SettingMinilandPrivateAsync)
                .Then(MinilandShouldBePrivate)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PrivateKicksEveryoneButFriend()
        {
            await new Spec("Private kicks everyone but friend")
                .WhenAsync(SettingMinilandPrivateAsync)
                .Then(OnlyOwnerAndFriendShouldRemain)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LockKicksEveryone()
        {
            await new Spec("Lock kicks everyone")
                .WhenAsync(LockingMinilandAsync)
                .Then(OnlyOwnerShouldRemain)
                .ExecuteAsync();
        }

        private async Task ChangingMinilandMessageAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "test",
                Type = MinilandEditType.Intro
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task ChangingMinilandMessageWithSpaceAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "Test Test",
                Type = MinilandEditType.Intro
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task LockingMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Lock,
                Type = MinilandEditType.State
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task OpeningMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Open,
                Type = MinilandEditType.State
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task SettingMinilandPrivateAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Private,
                Type = MinilandEditType.State
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private void MinilandMessageShouldBeChanged()
        {
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual("test", miniland.MinilandMessage);
            var lastpacket2 = (MlintroPacket?)Session.LastPackets.FirstOrDefault(s => s is MlintroPacket);
            Assert.AreEqual("test", lastpacket2?.Intro);
        }

        private void MinilandMessageWithSpaceShouldBeChanged()
        {
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual("Test Test", miniland.MinilandMessage);
            var lastpacket2 = ((MlintroPacket?)Session.LastPackets.FirstOrDefault(s => s is MlintroPacket)).NotNull();
            Assert.AreEqual("Test Test", lastpacket2?.Intro);
            Assert.AreEqual("mlintro Test^Test", new Serializer(new[] { typeof(MlintroPacket) }).Serialize(lastpacket2.NotNull()));
        }

        private void InfoPacketShouldBeSent()
        {
            var lastpacket = ((InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket)).NotNull();
            Assert.AreEqual(Game18NConstString.MinilandChanged, lastpacket.Message);
        }

        private void MinilandShouldBeLocked()
        {
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandLocked);
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Lock, miniland.State);
        }

        private void MinilandShouldBeOpen()
        {
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandPublic);
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Open, miniland.State);
        }

        private void MinilandShouldBePrivate()
        {
            var lastpacket = (MsgiPacket?)Session.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandPrivate);
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Private, miniland.State);
        }

        private void OnlyOwnerAndFriendShouldRemain()
        {
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.IsFalse(World.SessionRegistry.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != Session.Character.CharacterId && s.VisualId != Session2.Character.VisualId));
            Assert.AreEqual(2, World.SessionRegistry
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }

        private void OnlyOwnerShouldRemain()
        {
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.IsFalse(World.SessionRegistry.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != Session.Character.CharacterId));
            Assert.AreEqual(1, World.SessionRegistry
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }
    }
}
