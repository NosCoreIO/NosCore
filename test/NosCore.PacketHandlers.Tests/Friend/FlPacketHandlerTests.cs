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
using NosCore.Core;
using NosCore.Dao.Interfaces;
using NosCore.Data.CommandPackets;
using NosCore.Data.Dto;
using NosCore.Data.WebApi;
using NosCore.GameObject.Holders;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FlPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IDao<CharacterRelationDto, Guid>? _characterRelationDao;
        private FlCommandPacketHandler? _flPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _characterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            Broadcaster.Reset();
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Type = ServerType.WorldServer,
                        Id = 1
                    }

                });
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _flPacketHandler = new FlCommandPacketHandler();
        }

        [TestMethod]
        public async Task Test_Add_Distant_FriendAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var friendRequestHolder = new FriendRequestHolder();
            friendRequestHolder.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(targetSession.Character.CharacterId, _session!.Character.CharacterId));
            var flPacket = new FlCommandPacket
            {
                CharacterName = targetSession.Character.Name
            };
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = targetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Character.CharacterId }
                    }

                });
            var friend = new FriendService(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                friendRequestHolder, TestHelpers.Instance.PubSubHub.Object, TestHelpers.Instance.LogLanguageLocalizer);
            TestHelpers.Instance.FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(_session.Character.CharacterId,
                    targetSession.Character.VisualId,
                    FinsPacketType.Accepted
        ));

            await _flPacketHandler!.ExecuteAsync(flPacket, _session).ConfigureAwait(false);
            Assert.IsTrue(await _characterRelationDao!.FirstOrDefaultAsync(s =>
                (s.CharacterId == _session.Character.CharacterId) &&
                (s.RelatedCharacterId == targetSession.Character.CharacterId)
                && (s.RelationType == CharacterRelationType.Friend)).ConfigureAwait(false) != null);
        }
    }
}