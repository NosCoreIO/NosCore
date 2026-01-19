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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Services.FriendService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.FriendService
{
    [TestClass]
    public class FriendServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private IFriendService Service = null!;
        private IFriendRequestRegistry FriendRequestHolder = null!;
        private Mock<IPubSubHub> PubSubHub = null!;
        private Mock<IChannelHub> ChannelHub = null!;
        private long CharacterId;
        private long TargetCharacterId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            PubSubHub = new Mock<IPubSubHub>();
            ChannelHub = new Mock<IChannelHub>();
            FriendRequestHolder = new FriendRequestRegistry();

            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo { Type = ServerType.WorldServer, Id = 1 }
                });

            CharacterId = 1;
            TargetCharacterId = 2;

            Service = new GameObject.Services.FriendService.FriendService(
                Logger,
                TestHelpers.Instance.CharacterRelationDao,
                TestHelpers.Instance.CharacterDao,
                FriendRequestHolder,
                PubSubHub.Object,
                ChannelHub.Object,
                TestHelpers.Instance.LogLanguageLocalizer);
        }

        [TestMethod]
        public async Task AddingFriendWithoutRequestShouldSendRequest()
        {
            await new Spec("Adding friend without request should send request")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .WhenAsync(AddingFriendWithoutExistingRequest)
                .Then(ResultShouldBeFriendRequestSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AcceptingFriendRequestShouldCreateMutualRelation()
        {
            await new Spec("Accepting friend request should create mutual relation")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .And(FriendRequestExists)
                .WhenAsync(AcceptingFriendRequest)
                .ThenAsync(TwoFriendRelationsShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RejectingFriendRequestShouldNotCreateRelation()
        {
            await new Spec("Rejecting friend request should not create relation")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .And(FriendRequestExists)
                .WhenAsync(RejectingFriendRequest)
                .ThenAsync(NoFriendRelationShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingFriendWhenAlreadyFriendsShouldReturnAlreadyFriend()
        {
            await new Spec("Adding friend when already friends should return already friend")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .AndAsync(PlayersAreAlreadyFriends)
                .And(FriendRequestExists)
                .WhenAsync(AcceptingFriendRequest)
                .Then(ResultShouldBeAlreadyFriend)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AddingBlockedPlayerAsFriendShouldReturnBlacklistBlocked()
        {
            await new Spec("Adding blocked player as friend should return blacklist blocked")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .AndAsync(TargetIsBlocked)
                .And(FriendRequestExists)
                .WhenAsync(AcceptingFriendRequest)
                .Then(ResultShouldBeBlacklistBlocked)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetFriendsListShouldReturnFriends()
        {
            await new Spec("Get friends list should return friends")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .AndAsync(PlayersAreAlreadyFriends)
                .WhenAsync(GettingFriendsList)
                .Then(ListShouldContainTarget)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingFriendShouldRemoveBothRelations()
        {
            await new Spec("Deleting friend should remove both relations")
                .GivenAsync(BothPlayersAreOnlineSameChannel)
                .AndAsync(PlayersAreAlreadyFriends)
                .WhenAsync(DeletingFriend)
                .Then(DeleteShouldSucceed)
                .ThenAsync(NoFriendRelationShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingNonExistentFriendShouldReturnFalse()
        {
            await new Spec("Deleting non-existent friend should return false")
                .WhenAsync(DeletingNonExistentFriend)
                .Then(DeleteShouldFail)
                .ExecuteAsync();
        }

        private LanguageKey? AddFriendResult;
        private List<CharacterRelationStatus>? FriendsList;
        private bool DeleteResult;
        private Guid? FriendRelationId;

        private async Task BothPlayersAreOnlineSameChannel()
        {
            var session1 = await TestHelpers.Instance.GenerateSessionAsync();
            var session2 = await TestHelpers.Instance.GenerateSessionAsync();
            CharacterId = session1.Character.CharacterId;
            TargetCharacterId = session2.Character.CharacterId;

            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>
                {
                    new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = CharacterId, FriendRequestBlocked = false } },
                    new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = TargetCharacterId, FriendRequestBlocked = false } }
                });
        }

        private void FriendRequestExists()
        {
            FriendRequestHolder.RegisterRequest(Guid.NewGuid(), TargetCharacterId, CharacterId);
        }

        private async Task PlayersAreAlreadyFriends()
        {
            var relation1 = new CharacterRelationDto
            {
                CharacterId = CharacterId,
                RelatedCharacterId = TargetCharacterId,
                RelationType = CharacterRelationType.Friend
            };
            var inserted = await TestHelpers.Instance.CharacterRelationDao.TryInsertOrUpdateAsync(relation1);
            FriendRelationId = inserted.CharacterRelationId;

            var relation2 = new CharacterRelationDto
            {
                CharacterId = TargetCharacterId,
                RelatedCharacterId = CharacterId,
                RelationType = CharacterRelationType.Friend
            };
            await TestHelpers.Instance.CharacterRelationDao.TryInsertOrUpdateAsync(relation2);
        }

        private async Task TargetIsBlocked()
        {
            var relation = new CharacterRelationDto
            {
                CharacterId = CharacterId,
                RelatedCharacterId = TargetCharacterId,
                RelationType = CharacterRelationType.Blocked
            };
            await TestHelpers.Instance.CharacterRelationDao.TryInsertOrUpdateAsync(relation);
        }

        private async Task AddingFriendWithoutExistingRequest()
        {
            AddFriendResult = await Service.AddFriendAsync(CharacterId, TargetCharacterId, FinsPacketType.Accepted);
        }

        private async Task AcceptingFriendRequest()
        {
            AddFriendResult = await Service.AddFriendAsync(CharacterId, TargetCharacterId, FinsPacketType.Accepted);
        }

        private async Task RejectingFriendRequest()
        {
            AddFriendResult = await Service.AddFriendAsync(CharacterId, TargetCharacterId, FinsPacketType.Rejected);
        }

        private async Task GettingFriendsList()
        {
            FriendsList = await Service.GetFriendsAsync(CharacterId);
        }

        private async Task DeletingFriend()
        {
            DeleteResult = await Service.DeleteAsync(FriendRelationId!.Value);
        }

        private async Task DeletingNonExistentFriend()
        {
            DeleteResult = await Service.DeleteAsync(Guid.NewGuid());
        }

        private void ResultShouldBeFriendRequestSent()
        {
            Assert.AreEqual(LanguageKey.FRIEND_REQUEST_SENT, AddFriendResult);
        }

        private async Task TwoFriendRelationsShouldExist()
        {
            var relations = TestHelpers.Instance.CharacterRelationDao
                .Where(s => s.RelationType == CharacterRelationType.Friend)?.ToList();
            Assert.IsNotNull(relations);
            Assert.AreEqual(2, relations.Count);
        }

        private async Task NoFriendRelationShouldExist()
        {
            var relations = TestHelpers.Instance.CharacterRelationDao
                .Where(s => s.RelationType == CharacterRelationType.Friend)?.ToList();
            Assert.IsTrue(relations == null || relations.Count == 0);
        }

        private void ResultShouldBeAlreadyFriend()
        {
            Assert.AreEqual(LanguageKey.ALREADY_FRIEND, AddFriendResult);
        }

        private void ResultShouldBeBlacklistBlocked()
        {
            Assert.AreEqual(LanguageKey.BLACKLIST_BLOCKED, AddFriendResult);
        }

        private void ListShouldContainTarget()
        {
            Assert.IsNotNull(FriendsList);
            Assert.IsTrue(FriendsList.Any(s => s.CharacterId == TargetCharacterId));
        }

        private void DeleteShouldSucceed()
        {
            Assert.IsTrue(DeleteResult);
        }

        private void DeleteShouldFail()
        {
            Assert.IsFalse(DeleteResult);
        }
    }
}
