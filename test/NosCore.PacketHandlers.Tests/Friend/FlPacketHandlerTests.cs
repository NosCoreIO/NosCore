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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FlPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao = null!;
        private FlCommandPacketHandler FlPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            CharacterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            TestHelpers.Instance.ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>(){
                    new ChannelInfo
                    {
                        Type = ServerType.WorldServer,
                        Id = 1
                    }
                });
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            FlPacketHandler = new FlCommandPacketHandler(new NosCore.GameObject.Services.BroadcastService.SessionRegistry(Logger));
        }

        [TestMethod]
        public async Task AddingDistantFriendShouldCreateRelation()
        {
            await new Spec("Adding distant friend should create relation")
                .GivenAsync(TargetPlayerIsOnlineAndFriendRequestExists)
                .WhenAsync(AddingFriendByName)
                .ThenAsync(FriendRelationShouldExist)
                .ExecuteAsync();
        }

        private ClientSession? TargetSession;

        private async Task TargetPlayerIsOnlineAndFriendRequestExists()
        {
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            var friendRequestHolder = new FriendRequestRegistry();
            friendRequestHolder.RegisterRequest(Guid.NewGuid(),
                TargetSession.Character.CharacterId, Session.Character.CharacterId);
            TestHelpers.Instance.PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = TargetSession.Character.CharacterId }
                    },
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            var friend = new FriendService(Logger, CharacterRelationDao, TestHelpers.Instance.CharacterDao,
                friendRequestHolder, TestHelpers.Instance.PubSubHub.Object, TestHelpers.Instance.ChannelHub.Object, TestHelpers.Instance.LogLanguageLocalizer);
            TestHelpers.Instance.FriendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(Session.Character.CharacterId,
                    TargetSession.Character.VisualId,
                    FinsPacketType.Accepted));
        }

        private async Task AddingFriendByName()
        {
            await FlPacketHandler.ExecuteAsync(new FlCommandPacket { CharacterName = TargetSession!.Character.Name }, Session);
        }

        private async Task FriendRelationShouldExist()
        {
            var result = await CharacterRelationDao.FirstOrDefaultAsync(s =>
                s.CharacterId == Session.Character.CharacterId &&
                s.RelatedCharacterId == TargetSession!.Character.CharacterId &&
                s.RelationType == CharacterRelationType.Friend);
            Assert.IsNotNull(result);
        }
    }
}
