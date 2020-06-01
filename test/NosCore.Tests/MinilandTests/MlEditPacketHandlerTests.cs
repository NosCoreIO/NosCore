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
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.MinilandTests
{
    [TestClass]
    public class MlEditPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private MlEditPacketHandler? _mlEditPacketHandler;

        private ClientSession? _session;
        private MinilandProvider _minilandProvider = null!;
        private ClientSession _session2;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, null, null, null, Logger, new List<NpcTalkDto>(), TestHelpers.Instance.DistanceCalculator));
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _session2 = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var session3 = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            TestHelpers.Instance.FriendHttpClient
                .Setup(s => s.GetListFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>
                {
                    new CharacterRelationStatus
                    {
                        CharacterId = _session2.Character.CharacterId,
                        CharacterName = _session2.Character.Name,
                        IsConnected = true,
                        RelationType = CharacterRelationType.Friend,
                        CharacterRelationId = Guid.NewGuid()
                    }
                });
            await TestHelpers.Instance.MinilandDao.TryInsertOrUpdateAsync(new MinilandDto()
            {
                OwnerId = _session.Character.CharacterId,
            });
            _minilandProvider = new MinilandProvider(TestHelpers.Instance.MapInstanceProvider,
                TestHelpers.Instance.FriendHttpClient.Object,
                new List<MapDto> {new Map
                {
                    MapId = 20001,
                    NameI18NKey = "miniland",
                    Data = new byte[] {}
                }},
                TestHelpers.Instance.MinilandDao,
                TestHelpers.Instance.MinilandObjectDao);
            await _minilandProvider.InitializeAsync(_session.Character);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            await _session.ChangeMapInstanceAsync(miniland.MapInstanceId);
            await _session2.ChangeMapInstanceAsync(miniland.MapInstanceId);
            await session3.ChangeMapInstanceAsync(miniland.MapInstanceId);
            _mlEditPacketHandler = new MlEditPacketHandler(_minilandProvider);
        }

        [TestMethod]
        public async Task CanChangeMinilandMessageAsync()
        {

            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "test",
                Type = 1
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);
            var lastpacket = (InfoiPacket?)_session!.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.AreEqual(Game18NConstString.MinilandChanged, lastpacket!.Message);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            Assert.AreEqual("test", miniland.MinilandMessage);
            var lastpacket2 = (MlintroPacket?)_session!.LastPackets.FirstOrDefault(s => s is MlintroPacket);
            Assert.AreEqual("test", lastpacket2?.Intro);
        }

        [TestMethod]
        public async Task CanChangeMinilandMessageWithSpaceAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "Test Test",
                Type = 1
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);
            var lastpacket = (InfoiPacket?)_session!.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.AreEqual(Game18NConstString.MinilandChanged, lastpacket!.Message);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            Assert.AreEqual("Test Test", miniland.MinilandMessage);
            var lastpacket2 = (MlintroPacket?)_session!.LastPackets.FirstOrDefault(s => s is MlintroPacket);
            Assert.AreEqual("Test^Test", lastpacket2?.Intro);
        }

        [TestMethod]
        public async Task CanLockMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Lock,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)_session!.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandLocked);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Lock, miniland.State);
        }


        [TestMethod]
        public async Task CanOpenMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Open,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)_session!.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandPublic);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Open, miniland.State);
        }


        [TestMethod]
        public async Task CanPrivateMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Private,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);
            var lastpacket = (MsgiPacket?)_session!.LastPackets.FirstOrDefault(s => s is MsgiPacket);
            Assert.AreEqual(lastpacket?.Message, Game18NConstString.MinilandPrivate);
            var miniland = _minilandProvider.GetMiniland(_session.Character.CharacterId);
            Assert.AreEqual(MinilandState.Private, miniland.State);
        }

        [TestMethod]
        public async Task PrivateKickEveryoneButFriendAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Private,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            var miniland = _minilandProvider.GetMiniland(_session!.Character.CharacterId);
            Assert.AreEqual(MinilandState.Private, miniland.State);

            Assert.IsFalse(Broadcaster.Instance.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != _session.Character.CharacterId && s.VisualId != _session2.Character.VisualId));
            Assert.AreEqual(2, Broadcaster.Instance
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }

        [TestMethod]
        public async Task LockKickEveryoneAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Lock,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            var miniland = _minilandProvider.GetMiniland(_session!.Character.CharacterId);
            Assert.AreEqual(MinilandState.Lock, miniland.State);

            Assert.IsFalse(Broadcaster.Instance.GetCharacters()
                .Where(s => s.MapInstanceId == miniland.MapInstanceId)
                .Any(s => s.VisualId != _session.Character.CharacterId));

            Assert.AreEqual(1, Broadcaster.Instance
                .GetCharacters().Count(s => s.MapInstanceId == miniland.MapInstanceId));
        }
    }
}