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
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FDelPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IChannelHub> ChannelHttpClient = null!;
        private Mock<IDao<CharacterDto, long>> CharacterDao = null!;
        private IDao<CharacterRelationDto, Guid> CharacterRelationDao = null!;
        private Mock<IPubSubHub> ConnectedAccountHttpClient = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private FdelPacketHandler FDelPacketHandler = null!;
        private FriendService FriendController = null!;
        private Mock<IFriendHub> FriendHttpClient = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            CharacterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            Broadcaster.Reset();
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            ChannelHttpClient = TestHelpers.Instance.ChannelHttpClient;
            ConnectedAccountHttpClient = TestHelpers.Instance.PubSubHub;
            ChannelHub = new Mock<IChannelHub>();
            ConnectedAccountHttpClient.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>(){
                    new Subscriber
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = Session.Character.CharacterId }
                    }
                });
            FriendHttpClient = TestHelpers.Instance.FriendHttpClient;
            FDelPacketHandler = new FdelPacketHandler(FriendHttpClient.Object, ChannelHttpClient.Object,
                TestHelpers.Instance.PubSubHub.Object, TestHelpers.Instance.GameLanguageLocalizer, new NosCore.GameObject.Services.BroadcastService.SessionRegistry(Logger));
            CharacterDao = new Mock<IDao<CharacterDto, long>>();
            FriendController = new FriendService(Logger, CharacterRelationDao, CharacterDao.Object,
                new FriendRequestRegistry(), ConnectedAccountHttpClient.Object, ChannelHub.Object, TestHelpers.Instance.LogLanguageLocalizer);
            FriendHttpClient.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .Returns((long id) => FriendController.GetFriendsAsync(id));
            FriendHttpClient.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
                .Callback((Guid id) => Task.FromResult(FriendController.DeleteAsync(id)));
        }

        [TestMethod]
        public async Task DeletingFriendWhenDisconnectedShouldSucceed()
        {
            await new Spec("Deleting friend when disconnected should succeed")
                .GivenAsync(TargetIsDisconnectedWithFriendRelation)
                .WhenAsync(DeletingFriend)
                .Then(NoRelationsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingFriendShouldSucceed()
        {
            await new Spec("Deleting friend should succeed")
                .GivenAsync(TargetIsOnlineWithFriendRelation)
                .WhenAsync(DeletingTargetFriend)
                .Then(NoRelationsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonFriendShouldShowError()
        {
            await new Spec("Deleting non friend should show error")
                .GivenAsync(TargetIsOnline)
                .WhenAsync(DeletingTargetFriend)
                .Then(ShouldReceiveNotInFriendlistMessage)
                .ExecuteAsync();
        }

        private ClientSession? TargetSession;
        private long TargetCharacterId = 2;

        private async Task TargetIsDisconnectedWithFriendRelation()
        {
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                Session.Character,
                new() { CharacterId = 2, Name = "test" }
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = 2,
                    CharacterRelationId = guid,
                    RelatedCharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = 2,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
        }

        private async Task TargetIsOnlineWithFriendRelation()
        {
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            TargetCharacterId = TargetSession.Character.CharacterId;
            var guid = Guid.NewGuid();
            var targetGuid = Guid.NewGuid();
            var list = new List<CharacterDto>
            {
                Session.Character,
                TargetSession.Character
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))!
                .ReturnsAsync((Expression<Func<CharacterDto, bool>> exp) => list.FirstOrDefault(exp.Compile()));
            await CharacterRelationDao.TryInsertOrUpdateAsync(new[]
            {
                new CharacterRelationDto
                {
                    CharacterId = TargetSession.Character.CharacterId,
                    CharacterRelationId = guid,
                    RelatedCharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                },
                new CharacterRelationDto
                {
                    RelatedCharacterId = TargetSession.Character.CharacterId,
                    CharacterRelationId = targetGuid,
                    CharacterId = Session.Character.CharacterId,
                    RelationType = CharacterRelationType.Friend
                }
            });
        }

        private async Task TargetIsOnline()
        {
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            TargetCharacterId = TargetSession.Character.CharacterId;
            var list = new List<CharacterDto>
            {
                Session.Character,
                TargetSession.Character
            };
            CharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<Expression<Func<CharacterDto, bool>>>()))
                .Returns((Expression<Func<CharacterDto, bool>> exp) => Task.FromResult(list.FirstOrDefault(exp.Compile()))!);
        }

        private async Task DeletingFriend()
        {
            await FDelPacketHandler.ExecuteAsync(new FdelPacket { CharacterId = 2 }, Session);
        }

        private async Task DeletingTargetFriend()
        {
            await FDelPacketHandler.ExecuteAsync(new FdelPacket { CharacterId = TargetCharacterId }, Session);
        }

        private void NoRelationsShouldExist()
        {
            Assert.IsFalse(CharacterRelationDao.LoadAll().Any());
        }

        private void ShouldReceiveNotInFriendlistMessage()
        {
            var lastpacket = (InfoPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoPacket);
            Assert.AreEqual(TestHelpers.Instance.GameLanguageLocalizer[LanguageKey.NOT_IN_FRIENDLIST, Session.Account.Language], lastpacket?.Message);
        }
    }
}
