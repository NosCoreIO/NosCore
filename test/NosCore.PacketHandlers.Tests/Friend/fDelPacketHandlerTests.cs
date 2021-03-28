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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.Holders;
using NosCore.GameObject.HubClients.FriendHubClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IDao<CharacterDto, long>>? _characterDao;
        private IDao<CharacterRelationDto, Guid>? _characterRelationDao;
        private FdelPacketHandler? _fDelPacketHandler;
        private FriendService? _friendController;
        private Mock<IFriendHubClient>? _friendHttpClient;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _characterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            TestHelpers.Instance.ChannelHubClient.Setup(s => s.GetCharacterAsync(It.IsAny<long?>(), It.IsAny<string?>()))
                .ReturnsAsync(
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session.Character.CharacterId } });
            _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
            _fDelPacketHandler = new FdelPacketHandler(_friendHttpClient.Object, TestHelpers.Instance.ChannelHubClient.Object);
            _characterDao = new Mock<IDao<CharacterDto, long>>();
            _friendController = new FriendService(Logger, _characterRelationDao, _characterDao.Object,
                new FriendRequestHolder(), TestHelpers.Instance.ChannelHubClient.Object);
            _friendHttpClient.Setup(s => s.GetListFriendsAsync(It.IsAny<long>()))
                .Returns((long id) => _friendController.GetFriendsAsync(id));
            _friendHttpClient.Setup(s => s.DeleteFriendAsync(It.IsAny<Guid>()))
                .Callback((Guid id) => Task.FromResult(_friendController.DeleteAsync(id)));
        }

        [TestMethod]
        public async Task Test_Delete_Friend_When_DisconnectedAsync()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session!.Character!,
                new CharacterDto {CharacterId = 2, Name = "test"}
            };
            _characterDao!.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);
            await _characterRelationDao!.TryInsertOrUpdateAsync(new[]
            {
               new CharacterRelationDto
               {
                   CharacterId = 2,
                   CharacterRelationId = guid,
                   RelatedCharacterId = _session.Character.CharacterId,
                   RelationType = CharacterRelationType.Friend
               },
               new CharacterRelationDto
               {
                   RelatedCharacterId = 2,
                   CharacterRelationId = targetGuid,
                   CharacterId = _session.Character.CharacterId,
                   RelationType = CharacterRelationType.Friend
               }
           }).ConfigureAwait(false);
            var fdelPacket = new FdelPacket
            {
                CharacterId = 2
            };

            await _fDelPacketHandler!.ExecuteAsync(fdelPacket, _session).ConfigureAwait(false);

            Assert.IsTrue(!(_characterRelationDao!.LoadAll()).Any());
        }

        [TestMethod]
        public async Task Test_Delete_FriendAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session!.Character!,
                targetSession.Character!
            };
            _characterDao!.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))!
                .ReturnsAsync((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            await _characterRelationDao!.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = targetSession.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = targetSession.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = _session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            }).ConfigureAwait(false);
            var fdelPacket = new FdelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            await _fDelPacketHandler!.ExecuteAsync(fdelPacket, _session).ConfigureAwait(false);

            Assert.IsTrue(!_characterRelationDao.LoadAll().Any());
        }

        [TestMethod]
        public async Task Test_Delete_Friend_No_FriendAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                _session!.Character!,
                targetSession.Character!
            };
            _characterDao!.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);

            var fdelPacket = new FdelPacket
            {
                CharacterId = targetSession.Character.CharacterId
            };

            await _fDelPacketHandler!.ExecuteAsync(fdelPacket, _session).ConfigureAwait(false);
            var lastpacket = (InfoPacket?)_session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(GameLanguage.Instance.GetMessageFromKey(LanguageKey.NOT_IN_FRIENDLIST,
                _session.Account.Language), lastpacket?.Message);
        }
    }
}