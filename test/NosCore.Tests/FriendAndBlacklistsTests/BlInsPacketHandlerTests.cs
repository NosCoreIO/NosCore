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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.BlackListService;
using NosCore.MasterServer.Controllers;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.Data.WebApi.Character;
using NosCore.Shared.Configuration;

namespace NosCore.Tests.FriendAndBlacklistsTests
{
    [TestClass]
    public class BlInsPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;

        private BlInsPackettHandler? _blInsPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _blInsPacketHandler = new BlInsPackettHandler(TestHelpers.Instance.BlacklistHttpClient.Object, Logger);
            TestHelpers.Instance.ConnectedAccountHttpClient
                .Setup(s => s.GetCharacterAsync(_session.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                        {ChannelId = 1, ConnectedCharacter = new Character {Id = _session.Character.CharacterId}}));
            TestHelpers.Instance.ConnectedAccountHttpClient.Setup(s => s.GetCharacterAsync(null, _session.Character.Name))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                        {ChannelId = 1, ConnectedCharacter = new Character {Id = _session.Character.CharacterId}}));
        }

        [TestMethod]
        public async Task Test_Blacklist_When_DisconnectedAsync()
        {
            var blinsPacket = new BlInsPacket
            {
                CharacterId = 2
            };

            await _blInsPacketHandler!.ExecuteAsync(blinsPacket, _session!).ConfigureAwait(false);
            Assert.IsNull(await
                TestHelpers.Instance.CharacterRelationDao.FirstOrDefaultAsync(s =>
                    (_session!.Character.CharacterId == s.CharacterId) &&
                    (s.RelationType == CharacterRelationType.Blocked)).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Test_Blacklist_CharacterAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            TestHelpers.Instance.ConnectedAccountHttpClient
                .Setup(s => s.GetCharacterAsync(targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character {Id = targetSession.Character.CharacterId}
                    }));
            using var blacklist = new BlacklistController(new BlacklistService(TestHelpers.Instance.ConnectedAccountHttpClient.Object,
                TestHelpers.Instance.CharacterRelationDao, TestHelpers.Instance.CharacterDao));
            TestHelpers.Instance.BlacklistHttpClient.Setup(s => s.AddToBlacklistAsync(It.IsAny<BlacklistRequest>()))
                .Returns(blacklist.AddBlacklistAsync(new BlacklistRequest
                {
                    CharacterId = _session!.Character.CharacterId,
                    BlInsPacket = new BlInsPacket
                    {
                        CharacterId = targetSession.Character.VisualId
                    }
                }));
            var blinsPacket = new BlInsPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

           await _blInsPacketHandler!.ExecuteAsync(blinsPacket, _session).ConfigureAwait(false);
            Assert.IsNotNull(
                TestHelpers.Instance.CharacterRelationDao.FirstOrDefaultAsync(s => (_session.Character.CharacterId == s.CharacterId)
                    && (targetSession.Character.CharacterId == s.RelatedCharacterId) &&
                    (s.RelationType == CharacterRelationType.Blocked)));
        }
    }
}