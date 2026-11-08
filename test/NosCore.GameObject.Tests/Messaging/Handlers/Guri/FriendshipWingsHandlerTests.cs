//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class FriendshipWingsHandlerTests
    {
        private const short WingsVNum = 2160;
        private ClientSession _session = null!;
        private ClientSession _friend = null!;
        private ClientSession _otherFriend = null!;
        private Mock<IFriendHub> _friendHub = null!;
        private Mock<ISessionRegistry> _sessionRegistry = null!;
        private Mock<IMapChangeService> _mapChangeService = null!;
        private FriendshipWingsHandler _handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _friend = await TestHelpers.Instance.GenerateSessionAsync();
            _otherFriend = await TestHelpers.Instance.GenerateSessionAsync();
            _otherFriend.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _friend.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _friend.Character.MapId = 1;
            _friend.Character.PositionX = 7;
            _friend.Character.PositionY = 9;
            _friendHub = new Mock<IFriendHub>();
            _friendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>())).ReturnsAsync(new List<CharacterRelationStatus>());
            _sessionRegistry = new Mock<ISessionRegistry>();
            _mapChangeService = new Mock<IMapChangeService>();
            _handler = new FriendshipWingsHandler(_friendHub.Object, _sessionRegistry.Object, _mapChangeService.Object, TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task GoStartsTheDelayBarTowardsTheFriend()
        {
            await new Spec("The Go button starts a three second bar that sends the travel request back")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .WhenAsync(SendingArgument_, 1)
                .Then(TheDelayBarShouldTravelToTheFriend)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutWingsThePlayerIsTold()
        {
            await new Spec("Without wings the client's own No Wings of Friendship line is sent")
                .Given(TheOtherCharacterIsAFriend)
                .WhenAsync(SendingArgument_, 1)
                .Then(NoFriendshipWingsShouldBeSaid)
                .And(NoDelayBarShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SomeoneWhoIsNotAFriendCannotBeReached()
        {
            await new Spec("A character outside the friend list is ignored")
                .Given(CharacterHasWings)
                .WhenAsync(SendingArgument_, 1)
                .Then(NoDelayBarShouldBeSent)
                .And(NoMapChangeShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TravelTakesThePlayerToTheFriendAndUsesTheWings()
        {
            await new Spec("After the bar the player lands on the friend's spot and one pair of wings is used")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .And(TheFriendIsOnThisChannel)
                .AndAsync(TheBarHasRun)
                .WhenAsync(SendingArgument_, 2)
                .Then(ThePlayerShouldBeSentToTheFriend)
                .And(WingsCountShouldBe_, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TravelWithoutTheBarIsIgnored()
        {
            await new Spec("A travel request the server never started a bar for does nothing")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .And(TheFriendIsOnThisChannel)
                .WhenAsync(SendingArgument_, 2)
                .Then(NoMapChangeShouldHappen)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TravelBeforeTheBarEndsIsIgnored()
        {
            await new Spec("A travel request sent one second into the bar does nothing")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .And(TheFriendIsOnThisChannel)
                .AndAsync(SendingArgument_, 1)
                .And(SecondsPass_, 1)
                .WhenAsync(SendingArgument_, 2)
                .Then(NoMapChangeShouldHappen)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnOfflineFriendKeepsTheWings()
        {
            await new Spec("When the friend is not on this channel the player is told and keeps the wings")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .AndAsync(TheBarHasRun)
                .WhenAsync(SendingArgument_, 2)
                .Then(YourFriendIsOfflineShouldBeShown)
                .And(WingsCountShouldBe_, 1)
                .And(NoMapChangeShouldHappen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABarForOneFriendDoesNotTravelToAnother()
        {
            await new Spec("A travel request naming a different friend than the bar was started for does nothing")
                .Given(CharacterHasWings)
                .And(BothOtherCharactersAreFriends)
                .And(BothFriendsAreOnThisChannel)
                .AndAsync(TheBarHasRun)
                .WhenAsync(SendingTravelTowardsTheOtherFriend)
                .Then(NoMapChangeShouldHappen)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BeingHitInterruptsTheBar()
        {
            await new Spec("A hit taken during the bar cancels it, so the travel request that follows does nothing")
                .Given(CharacterHasWings)
                .And(TheOtherCharacterIsAFriend)
                .And(TheFriendIsOnThisChannel)
                .AndAsync(SendingArgument_, 1)
                .AndAsync(ThePlayerIsHit)
                .And(SecondsPass_, 3)
                .WhenAsync(SendingArgument_, 2)
                .Then(NoMapChangeShouldHappen)
                .And(WingsCountShouldBe_, 1)
                .ExecuteAsync();
        }

        private void BothOtherCharactersAreFriends()
        {
            var selfId = _session.Character.CharacterId;
            _friendHub.Setup(s => s.GetFriendsAsync(selfId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = _friend.Character.CharacterId, RelationType = CharacterRelationType.Friend, IsConnected = true },
                new() { CharacterId = _otherFriend.Character.CharacterId, RelationType = CharacterRelationType.Friend, IsConnected = true }
            });
        }

        private void BothFriendsAreOnThisChannel()
        {
            TheFriendIsOnThisChannel();
            var otherId = _otherFriend.Character.CharacterId;
            _sessionRegistry.Setup(s => s.GetSessionByCharacterId(otherId)).Returns(_otherFriend);
        }

        private Task SendingTravelTowardsTheOtherFriend()
        {
            return _handler.Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
            {
                Type = GuriPacketType.FriendshipWings,
                Argument = 2,
                VisualId = _otherFriend.Character.CharacterId
            }));
        }

        private Task ThePlayerIsHit()
        {
            return new DelayInterruptionHandler().Handle(
                new EntityDamagedEvent(_friend.Character, _session.Character, 10, false));
        }

        private void CharacterHasWings()
        {
            var item = new Item
            {
                VNum = WingsVNum,
                Type = NoscorePocketType.Main,
                Effect = ItemEffectType.FriendshipWings,
            };
            var inst = new ItemInstanceForTest(WingsVNum) { Amount = 1, Item = item };
            var wings = InventoryItemInstance.Create(inst, _session.Character.CharacterId);
            wings.Slot = 0;
            wings.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[wings.ItemInstanceId] = wings;
        }

        private void TheOtherCharacterIsAFriend()
        {
            var selfId = _session.Character.CharacterId;
            var friendId = _friend.Character.CharacterId;
            _friendHub.Setup(s => s.GetFriendsAsync(selfId)).ReturnsAsync(new List<CharacterRelationStatus>
            {
                new() { CharacterId = friendId, RelationType = CharacterRelationType.Friend, IsConnected = true }
            });
        }

        private void TheFriendIsOnThisChannel()
        {
            var friendId = _friend.Character.CharacterId;
            _sessionRegistry.Setup(s => s.GetSessionByCharacterId(friendId)).Returns(_friend);
        }

        private async Task TheBarHasRun()
        {
            await SendingArgument_(1);
            SecondsPass_(3);
        }

        private void SecondsPass_(int seconds) => TestHelpers.Instance.Clock.AdvanceSeconds(seconds);

        private async Task SendingArgument_(int argument)
        {
            await _handler.Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
            {
                Type = GuriPacketType.FriendshipWings,
                Argument = argument,
                VisualId = _friend.Character.CharacterId
            }));
        }

        private void TheDelayBarShouldTravelToTheFriend()
        {
            var delay = _session.LastPackets.OfType<DelayPacket>().SingleOrDefault();
            Assert.IsNotNull(delay);
            var next = (ClientGuriPacket)delay.Packet!;
            Assert.AreEqual(GuriPacketType.FriendshipWings, next.Type);
            Assert.AreEqual(2, next.Argument);
            Assert.AreEqual(_friend.Character.CharacterId, next.VisualId);
        }

        private void NoDelayBarShouldBeSent() => Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());

        private void NoFriendshipWingsShouldBeSaid() =>
            Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == Game18NConstString.NoFriendshipWings));

        private void YourFriendIsOfflineShouldBeShown() =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == Game18NConstString.YourFriendIsOffline));

        private void ThePlayerShouldBeSentToTheFriend() =>
            _mapChangeService.Verify(s => s.ChangeMapAsync(_session, (short?)1, (short?)7, (short?)9), Times.Once);

        private void NoMapChangeShouldHappen() =>
            _mapChangeService.Verify(s => s.ChangeMapAsync(It.IsAny<ClientSession>(), It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);

        private void WingsCountShouldBe_(int expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(WingsVNum));

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
