//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.BlacklistService
{
    [TestClass]
    public class BlacklistServiceTests
    {
        private TestWorld World;
        private IBlacklistService Service;
        private Mock<IPubSubHub> PubSubHub;
        private Mock<IChannelHub> ChannelHub;
        private long CharacterId;
        private long TargetCharacterId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            PubSubHub = World.Freeze<Mock<IPubSubHub>>();
            ChannelHub = World.Freeze<Mock<IChannelHub>>();

            ChannelHub.Setup(s => s.GetCommunicationChannels())
                .ReturnsAsync(new List<ChannelInfo>
                {
                    new ChannelInfo { Host = "127.0.0.1", Type = ServerType.WorldServer, Id = 1 }
                });

            CharacterId = 1;
            TargetCharacterId = 2;

            Service = World.Create<GameObject.Services.BlackListService.BlacklistService>();
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
        public async Task BlacklistingAnOfflinePlayerShouldSucceed()
        {
            await new Spec("Blacklisting an offline player should succeed")
                .GivenAsync(BothPlayersAreOnline)
                .And(TargetIsOffline)
                .WhenAsync(BlacklistingTarget)
                .ThenAsync(BlacklistRelationShouldExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BlacklistingACharacterThatDoesNotExistStoresNothing()
        {
            await new Spec("Blacklisting a character that does not exist stores nothing")
                .GivenAsync(BothPlayersAreOnline)
                .WhenAsync(BlacklistingCharacter_, 999_999L)
                .Then(NoRelationShouldBeStored)
                .ExecuteAsync();
        }

        private async Task BlacklistingCharacter_(long characterId) =>
            BlacklistResult = await Service.BlacklistPlayerAsync(CharacterId, characterId);

        private void NoRelationShouldBeStored()
        {
            Assert.AreEqual(LanguageKey.USER_NOT_CONNECTED, BlacklistResult);
            Assert.IsFalse(World.CharacterRelationDao.Where(r => r.CharacterId == CharacterId)?.Any() ?? false);
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

        [TestMethod]
        public async Task BlacklistAsksTheHubsOnceWhateverItsLength()
        {
            await new Spec("Blacklist asks the hubs once whatever its length")
                .GivenAsync(ThreeBlockedCharactersOfWhichOneIsOnline)
                .WhenAsync(GettingBlacklistedList)
                .Then(HubsShouldBeAskedOnce)
                .And(EveryBlockedCharacterShouldBeListedWithItsNameAndStatus)
                .ExecuteAsync();
        }

        private LanguageKey? BlacklistResult;
        private readonly Dictionary<long, (string Name, bool Online)> ExpectedBlocked = new();

        private async Task ThreeBlockedCharactersOfWhichOneIsOnline()
        {
            CharacterId = (await World.CreateSessionAsync()).Character.CharacterId;
            for (var i = 0; i < 3; i++)
            {
                var blocked = (await World.CreateSessionAsync()).Character;
                ExpectedBlocked[blocked.CharacterId] = (blocked.Name, i == 0);
                await World.CharacterRelationDao.TryInsertOrUpdateAsync(new CharacterRelationDto
                {
                    CharacterId = CharacterId,
                    RelatedCharacterId = blocked.CharacterId,
                    RelationType = CharacterRelationType.Blocked
                });
            }

            PubSubHub.Setup(s => s.GetSubscribersAsync())
                .ReturnsAsync(ExpectedBlocked.Where(f => f.Value.Online)
                    .Select(f => new Subscriber { ChannelId = 1, ConnectedCharacter = new Character { Id = f.Key } })
                    .ToList());
        }

        private void HubsShouldBeAskedOnce()
        {
            PubSubHub.Verify(s => s.GetSubscribersAsync(), Times.Once);
            ChannelHub.Verify(s => s.GetCommunicationChannels(), Times.Once);
        }

        private void EveryBlockedCharacterShouldBeListedWithItsNameAndStatus()
        {
            Assert.AreEqual(ExpectedBlocked.Count, BlacklistedList.Count);
            foreach (var blocked in BlacklistedList)
            {
                Assert.AreEqual(ExpectedBlocked[blocked.CharacterId].Name, blocked.CharacterName);
                Assert.AreEqual(ExpectedBlocked[blocked.CharacterId].Online, blocked.IsConnected);
            }
        }


        private List<CharacterRelationStatus> BlacklistedList;
        private bool UnblacklistResult;
        private Guid? BlockRelationId;

        private async Task BothPlayersAreOnline()
        {
            var session1 = await World.CreateSessionAsync();
            var session2 = await World.CreateSessionAsync();
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
            var inserted = await World.CharacterRelationDao.TryInsertOrUpdateAsync(relation);
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
            await World.CharacterRelationDao.TryInsertOrUpdateAsync(relation);
        }

        private async Task BlacklistingTarget()
        {
            BlacklistResult = await Service.BlacklistPlayerAsync(CharacterId, TargetCharacterId);
        }

        private async Task GettingBlacklistedList()
        {
            BlacklistedList = await Service.GetBlacklistedListAsync(CharacterId);
        }

        private async Task UnblacklistingTarget()
        {
            UnblacklistResult = await Service.UnblacklistAsync(BlockRelationId.NotNull());
        }

        private async Task UnblacklistingNonExistent()
        {
            UnblacklistResult = await Service.UnblacklistAsync(Guid.NewGuid());
        }

        private async Task BlacklistRelationShouldExist()
        {
            var relation = await World.CharacterRelationDao.FirstOrDefaultAsync(s =>
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
