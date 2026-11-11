//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Miniland
{
    [TestClass]
    public class MinilandInvitePacketHandlerTests
    {
        private TestWorld World;
        private const short SeedOfPower = 1012;
        private ClientSession _owner;
        private ClientSession _guest;
        private Mock<IMinilandService> _minilandService;
        private Mock<IMapChangeService> _mapChangeService;
        private GameObject.Services.MinilandService.Miniland _miniland;
        private long _guestId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _owner = await World.GenerateSessionAsync();
            _guest = await World.GenerateSessionAsync();
            _guestId = _guest.Character.CharacterId;
            _miniland = new GameObject.Services.MinilandService.Miniland
            {
                MapInstanceId = _owner.Character.MapInstanceId,
                State = MinilandState.Open
            };
            _minilandService = World.Freeze<Mock<IMinilandService>>();
            _minilandService.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(_miniland);
            _mapChangeService = World.Freeze<Mock<IMapChangeService>>();
            GiveSeeds(1);
        }

        private void GiveSeeds(short amount) =>
            _owner.Character.InventoryService.AddItemToPocket(InventoryItemInstance.Create(
                World.GenerateItemProvider().Create(SeedOfPower, amount), _owner.Character.CharacterId));

        private Task InviteAsync(string name) =>
            World.Create<MinilandInvitePacketHandler>()
                .ExecuteAsync(new MinilandInvitePacket { CharacterName = name }, _owner);

        private Task AnswerAsync(VisualType type) =>
            World.Create<MJoinPacketHandler>()
                .ExecuteAsync(new MJoinPacket { Type = type, VisualId = _owner.Character.CharacterId }, _guest);

        private Task AcceptAsync() => AnswerAsync(VisualType.Player);

        private static Game18NConstString? LastInfo(ClientSession session) =>
            session.LastPackets.OfType<InfoiPacket>().LastOrDefault()?.Message;

        [TestMethod]
        public async Task TheOwnerInvitesFromTheirMinilandForASeed()
        {
            await InviteAsync(_guest.Character.Name);

            var question = _guest.LastPackets.OfType<Dlgi2Packet>().Single();
            Assert.AreEqual(Game18NConstString.HasInvitedToMiniland, question.Question);
            Assert.AreEqual(_owner.Character.CharacterId, ((MJoinPacket)question.YesPacket.NotNull()).VisualId);
            Assert.AreEqual(VisualType.Npc, ((MJoinPacket)question.NoPacket.NotNull()).Type);
            Assert.AreEqual(0, _owner.Character.InventoryService.CountItem(SeedOfPower));
        }

        [TestMethod]
        public async Task InvitingOutsideTheMinilandIsRefused()
        {
            _miniland.MapInstanceId = Guid.NewGuid();

            await InviteAsync(_guest.Character.Name);

            Assert.AreEqual(Game18NConstString.InvitationOnlyPossibleInMiniland, LastInfo(_owner));
            Assert.AreEqual(1, _owner.Character.InventoryService.CountItem(SeedOfPower));
        }

        [TestMethod]
        public async Task ALockedMinilandCannotInvite()
        {
            _miniland.State = MinilandState.Lock;

            await InviteAsync(_guest.Character.Name);

            Assert.AreEqual(Game18NConstString.CanNotInviteInLockedMiniland, LastInfo(_owner));
        }

        [TestMethod]
        public async Task InvitingSomeoneOfflineIsRefused()
        {
            await InviteAsync("Nobody");

            Assert.AreEqual(Game18NConstString.PlayerIsNotLoggedIn, LastInfo(_owner));
        }

        [TestMethod]
        public async Task APlayerBlockingMinilandInvitesCannotBeInvited()
        {
            _guest.Character.MinilandInviteBlocked = true;

            await InviteAsync(_guest.Character.Name);

            Assert.AreEqual(Game18NConstString.PlayerCanNotBeInvited, LastInfo(_owner));
            Assert.AreEqual(1, _owner.Character.InventoryService.CountItem(SeedOfPower));
        }

        [TestMethod]
        public async Task InvitingWithoutASeedIsRefused()
        {
            var seed = _owner.Character.InventoryService.Values.Single(s => s.ItemInstance.ItemVNum == SeedOfPower);
            _owner.Character.InventoryService.DeleteById(seed.ItemInstanceId);

            await InviteAsync(_guest.Character.Name);

            Assert.AreEqual(Game18NConstString.NotEnoughSeedToInvite, LastInfo(_owner));
            Assert.AreEqual(0, _guest.LastPackets.OfType<Dlgi2Packet>().Count());
        }

        [TestMethod]
        public async Task TheInvitedPlayerEntersWithoutBeingAFriend()
        {
            World.FriendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            await InviteAsync(_guest.Character.Name);

            await AcceptAsync();

            _mapChangeService.Verify(s => s.ChangeMapInstanceAsync(_guest, _miniland.MapInstanceId, 5, 8), Times.Once);
            Assert.IsFalse(_owner.Character.MinilandInviteCharacterIds.ContainsKey(_guestId));
        }

        [TestMethod]
        public async Task AnInvitationIsUsedOnce()
        {
            World.FriendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            await InviteAsync(_guest.Character.Name);

            await AcceptAsync();
            await AcceptAsync();

            _mapChangeService.Verify(s => s.ChangeMapInstanceAsync(It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }

        [TestMethod]
        public async Task InvitingAgainAfterTheCooldownWhileAnInvitationIsPendingCostsNoSeed()
        {
            await new Spec("Inviting again after the cooldown while an invitation is pending costs no seed")
                .Given(TheOwnerHas_ExtraSeeds, (short)1)
                .WhenAsync(TheOwnerInvitesTheGuestAsync)
                .And(TheCooldownPasses)
                .AndAsync(TheOwnerInvitesTheGuestAsync)
                .Then(TheGuestIsAsked_Times, 2)
                .And(TheOwnerHas_SeedsLeft, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task InvitingAgainWithinTheCooldownDoesNotAskAgain()
        {
            await new Spec("Inviting again within the cooldown does not ask again")
                .WhenAsync(TheOwnerInvitesTheGuestAsync)
                .AndAsync(TheOwnerInvitesTheGuestAsync)
                .Then(TheGuestIsAsked_Times, 1)
                .And(TheOwnerIsToldToWait)
                .ExecuteAsync();
        }

        private void TheOwnerHas_ExtraSeeds(short amount) => GiveSeeds(amount);

        private Task TheOwnerInvitesTheGuestAsync() => InviteAsync(_guest.Character.Name);

        private void TheCooldownPasses() => World.Clock.Advance(Duration.FromSeconds(6));

        private void TheGuestIsAsked_Times(int times) =>
            Assert.AreEqual(times, _guest.LastPackets.OfType<Dlgi2Packet>().Count());

        private void TheOwnerHas_SeedsLeft(int amount) =>
            Assert.AreEqual(amount, _owner.Character.InventoryService.CountItem(SeedOfPower));

        private void TheOwnerIsToldToWait() => Assert.AreEqual(Game18NConstString.CannotSendInvite, LastInfo(_owner));

        [TestMethod]
        public async Task ALockedMinilandKeepsTheInvitationForLater()
        {
            World.FriendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            await InviteAsync(_guest.Character.Name);
            _miniland.State = MinilandState.Lock;

            await AcceptAsync();
            _miniland.State = MinilandState.Open;
            await AcceptAsync();

            Assert.AreEqual(Game18NConstString.MinilandLocked, _guest.LastPackets.OfType<InfoiPacket>().First().Message);
            _mapChangeService.Verify(s => s.ChangeMapInstanceAsync(_guest, _miniland.MapInstanceId, 5, 8), Times.Once);
        }

        [TestMethod]
        public async Task CancellingDropsTheInvitation()
        {
            World.FriendHub.Setup(s => s.GetFriendsAsync(It.IsAny<long>()))
                .ReturnsAsync(new List<CharacterRelationStatus>());
            await InviteAsync(_guest.Character.Name);

            await AnswerAsync(VisualType.Npc);
            await AcceptAsync();

            _mapChangeService.Verify(s => s.ChangeMapInstanceAsync(It.IsAny<ClientSession>(), It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
        }
    }
}
