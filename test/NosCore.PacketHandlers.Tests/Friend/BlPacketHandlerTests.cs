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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BlackListService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Configuration;
using NosCore.Tests.Shared;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class BlPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private BlPacketHandler? _blPacketHandler;
        private IDao<CharacterRelationDto, Guid>? _characterRelationDao;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _characterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            TestHelpers.Instance.PubSubHubClient
                .Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<ConnectedAccount> { 
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session.Character.CharacterId } },
                });
            _blPacketHandler = new BlPacketHandler();
        }

        [TestMethod]
        public async Task Test_Distant_BlacklistAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var blPacket = new BlPacket
            {
                CharacterName = targetSession.Character.Name
            };
            TestHelpers.Instance.PubSubHubClient
                .Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<ConnectedAccount> {
                    new ConnectedAccount
                        { ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId } },
                    new ConnectedAccount
                        { ChannelId = 1, ConnectedCharacter = new Character { Id = targetSession.Character.CharacterId } },
                });
          
             var blacklist = new BlacklistService(TestHelpers.Instance.PubSubHubClient.Object,
                _characterRelationDao!, TestHelpers.Instance.CharacterDao);
            TestHelpers.Instance.BlacklistHttpClient.Setup(s => s.AddToBlacklistAsync(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.BlacklistPlayerAsync( _session!.Character.CharacterId, targetSession.Character.VisualId));
            await _blPacketHandler!.ExecuteAsync(blPacket, _session).ConfigureAwait(false);
            Assert.IsTrue(await _characterRelationDao!.FirstOrDefaultAsync(s =>
                (s.CharacterId == _session.Character.CharacterId) &&
                (s.RelatedCharacterId == targetSession.Character.CharacterId)
                && (s.RelationType == CharacterRelationType.Blocked)).ConfigureAwait(false) != null);
        }
    }
}