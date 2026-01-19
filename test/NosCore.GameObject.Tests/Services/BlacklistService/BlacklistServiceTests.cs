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
using NosCore.GameObject.Services.BlackListService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BlacklistService
{
    [TestClass]
    public class BlacklistServiceTests
    {
        private IBlacklistService Service = null!;
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

            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo { Type = ServerType.WorldServer, Id = 1 }
                });

            CharacterId = 1;
            TargetCharacterId = 2;

            Service = new GameObject.Services.BlackListService.BlacklistService(
                PubSubHub.Object,
                ChannelHub.Object,
                TestHelpers.Instance.CharacterRelationDao,
                TestHelpers.Instance.CharacterDao);
        }

        [TestMethod]
        public async Task BlacklistingPlayerShouldSucceed()
        {
            await new Spec("Blacklisting player should succeed")
                .GivenAsync(BothPlayersAreOnline)
                .WhenAsync(BlacklistingTarget)
                .ThenAsync(BlacklistRelationShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistingDisconnectedPlayerShouldThrow()
        {
            await new Spec("Blacklisting disconnected player should throw")
                .Given(TargetIsOffline)
                .WhenAsync(BlacklistingTarget).Catch(out var exception)
                .Then(ShouldThrowArgumentException_, exception)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistingSamePlayerTwiceShouldReturnAlreadyBlacklisted()
        {
            await new Spec("Blacklisting same player twice should return already blacklisted")
                .GivenAsync(BothPlayersAreOnline)
                .AndAsync(TargetIsAlreadyBlacklisted)
                .WhenAsync(BlacklistingTarget)
                .Then(ResultShouldBeAlreadyBlacklisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistingFriendShouldReturnCantBlockFriend()
        {
            await new Spec("Blacklisting friend should return can't block friend")
                .GivenAsync(BothPlayersAreOnline)
                .AndAsync(TargetIsFriend)
                .WhenAsync(BlacklistingTarget)
                .Then(ResultShouldBeCantBlockFriend)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetBlacklistedListShouldReturnBlockedRelations()
        {
            await new Spec("Get blacklisted list should return blocked relations")
                .GivenAsync(BothPlayersAreOnline)
                .AndAsync(TargetIsAlreadyBlacklisted)
                .WhenAsync(GettingBlacklistedList)
                .Then(ListShouldContainTarget)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnblacklistingShouldRemoveRelation()
        {
            await new Spec("Unblacklisting should remove relation")
                .GivenAsync(BothPlayersAreOnline)
                .AndAsync(TargetIsAlreadyBlacklisted)
                .WhenAsync(UnblacklistingTarget)
                .Then(UnblacklistShouldSucceed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnblacklistingNonExistentShouldReturnFalse()
        {
            await new Spec("Unblacklisting non-existent should return false")
                .WhenAsync(UnblacklistingNonExistent)
                .Then(UnblacklistShouldFail)
                .ExecuteAsync();
        }

        private LanguageKey? BlacklistResult;
        private List<CharacterRelationStatus>? BlacklistedList;
        private bool UnblacklistResult;
        private Guid? BlockRelationId;

        private async Task BothPlayersAreOnline()
        {
            var session1 = await TestHelpers.Instance.GenerateSessionAsync();
            var session2 = await TestHelpers.Instance.GenerateSessionAsync();
            CharacterId = session1.Character.CharacterId;
            TargetCharacterId = session2.Character.CharacterId;

            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>
                {
                    new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = CharacterId } },
                    new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = TargetCharacterId } }
                });
        }

        private void TargetIsOffline()
        {
            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(new List<Subscriber>
                {
                    new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = CharacterId } }
                });
        }

        private async Task TargetIsAlreadyBlacklisted()
        {
            var relation = new CharacterRelationDto
            {
                CharacterId = CharacterId,
                RelatedCharacterId = TargetCharacterId,
                RelationType = CharacterRelationType.Blocked
            };
            var inserted = await TestHelpers.Instance.CharacterRelationDao.TryInsertOrUpdateAsync(relation);
            BlockRelationId = inserted.CharacterRelationId;
        }

        private async Task TargetIsFriend()
        {
            var relation = new CharacterRelationDto
            {
                CharacterId = CharacterId,
                RelatedCharacterId = TargetCharacterId,
                RelationType = CharacterRelationType.Friend
            };
            await TestHelpers.Instance.CharacterRelationDao.TryInsertOrUpdateAsync(relation);
        }

        private async Task BlacklistingTarget()
        {
            BlacklistResult = await Service.BlacklistPlayerAsync(CharacterId, TargetCharacterId);
        }

        private void ShouldThrowArgumentException_(Lazy<Exception> exception)
        {
            Assert.IsInstanceOfType(exception.Value, typeof(ArgumentException));
        }

        private async Task GettingBlacklistedList()
        {
            BlacklistedList = await Service.GetBlacklistedListAsync(CharacterId);
        }

        private async Task UnblacklistingTarget()
        {
            UnblacklistResult = await Service.UnblacklistAsync(BlockRelationId!.Value);
        }

        private async Task UnblacklistingNonExistent()
        {
            UnblacklistResult = await Service.UnblacklistAsync(Guid.NewGuid());
        }

        private async Task BlacklistRelationShouldExist()
        {
            var relation = await TestHelpers.Instance.CharacterRelationDao.FirstOrDefaultAsync(s =>
                s.CharacterId == CharacterId &&
                s.RelatedCharacterId == TargetCharacterId &&
                s.RelationType == CharacterRelationType.Blocked);
            Assert.IsNotNull(relation);
            Assert.AreEqual(LanguageKey.BLACKLIST_ADDED, BlacklistResult);
        }

        private void ResultShouldBeAlreadyBlacklisted()
        {
            Assert.AreEqual(LanguageKey.ALREADY_BLACKLISTED, BlacklistResult);
        }

        private void ResultShouldBeCantBlockFriend()
        {
            Assert.AreEqual(LanguageKey.CANT_BLOCK_FRIEND, BlacklistResult);
        }

        private void ListShouldContainTarget()
        {
            Assert.IsNotNull(BlacklistedList);
            Assert.IsTrue(BlacklistedList.Any(s => s.CharacterId == TargetCharacterId));
        }

        private void UnblacklistShouldSucceed()
        {
            Assert.IsTrue(UnblacklistResult);
        }

        private void UnblacklistShouldFail()
        {
            Assert.IsFalse(UnblacklistResult);
        }
    }
}
