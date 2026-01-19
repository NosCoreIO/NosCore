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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MlEditPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private MlEditPacketHandler MlEditPacketHandler = null!;
        private ClientSession Session = null!;
        private IMinilandService MinilandProvider = null!;
        private ClientSession Session2 = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock));
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session2 = await TestHelpers.Instance.GenerateSessionAsync();
            var session3 = await TestHelpers.Instance.GenerateSessionAsync();
            TestHelpers.Instance.FriendHttpClient
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
            await TestHelpers.Instance.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = Session.Character.CharacterId,
            });
            MinilandProvider = new MinilandService(TestHelpers.Instance.MapInstanceAccessorService,
                TestHelpers.Instance.FriendHttpClient.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                TestHelpers.Instance.MinilandDao,
                TestHelpers.Instance.MinilandObjectDao, new MinilandRegistry());
            await MinilandProvider.InitializeAsync(Session.Character, TestHelpers.Instance.MapInstanceGeneratorService);
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            var mapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetMapInstance(miniland.MapInstanceId)!;
            Session.Character.MapInstance = mapInstance;
            Session2.Character.MapInstance = mapInstance;
            session3.Character.MapInstance = mapInstance;
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
                Type = 1
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task ChangingMinilandMessageWithSpaceAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "Test Test",
                Type = 1
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task LockingMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Lock,
                Type = 2
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task OpeningMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Open,
                Type = 2
            };
            await MlEditPacketHandler.ExecuteAsync(mleditPacket, Session);
        }

        private async Task SettingMinilandPrivateAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Private,
                Type = 2
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
            var lastpacket2 = (MlintroPacket?)Session.LastPackets.FirstOrDefault(s => s is MlintroPacket);
            Assert.AreEqual("Test^Test", lastpacket2?.Intro);
        }

        private void InfoPacketShouldBeSent()
        {
            var lastpacket = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.AreEqual(Game18NConstString.MinilandChanged, lastpacket!.Message);
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
            Assert.IsFalse(TestHelpers.Instance.SessionRegistry.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != Session.Character.CharacterId && s.VisualId != Session2.Character.VisualId));
            Assert.AreEqual(2, TestHelpers.Instance.SessionRegistry
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }

        private void OnlyOwnerShouldRemain()
        {
            var miniland = MinilandProvider.GetMiniland(Session.Character.CharacterId);
            Assert.IsFalse(TestHelpers.Instance.SessionRegistry.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != Session.Character.CharacterId));
            Assert.AreEqual(1, TestHelpers.Instance.SessionRegistry
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }
    }
}
