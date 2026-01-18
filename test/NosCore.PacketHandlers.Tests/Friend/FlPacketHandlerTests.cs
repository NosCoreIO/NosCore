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
using NosCore.GameObject.Services.FriendService;
using NosCore.GameObject.Networking;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.GameObject.Ecs;
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
            TestHelpers.Instance.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Type = ServerType.WorldServer,
                        Id = 1
                    }

                });
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _flPacketHandler = new FlCommandPacketHandler(new NosCore.GameObject.Services.BroadcastService.SessionRegistry());
        }

        [TestMethod]
        public async Task Test_Add_Distant_FriendAsync()
        {
            var targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var friendRequestRegistry = new FriendRequestRegistry();
            friendRequestRegistry.AddRequest(targetSession.Player.CharacterId, _session!.Player.CharacterId);
            var flPacket = new FlCommandPacket
            {
                CharacterName = targetSession.Player.Name
            };
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = targetSession.Player.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _session!.Player.CharacterId }
                    }

                });
            var friend = new FriendService(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                friendRequestRegistry, TestHelpers.Instance.PubSubHub.Object, TestHelpers.Instance.ChannelHub.Object, TestHelpers.Instance.LogLanguageLocalizer);
            TestHelpers.Instance.FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(_session.Player.CharacterId,
                    targetSession.Player.VisualId,
                    FinsPacketType.Accepted
        ));

            await _flPacketHandler!.ExecuteAsync(flPacket, _session).ConfigureAwait(false);
            Assert.IsTrue(await _characterRelationDao!.FirstOrDefaultAsync(s =>
                (s.CharacterId == _session.Player.CharacterId) &&
                (s.RelatedCharacterId == targetSession.Player.CharacterId)
                && (s.RelationType == CharacterRelationType.Friend)).ConfigureAwait(false) != null);
        }
    }
}