//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Dao;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs;
using Moq;
using NosCore.GameObject.Services.MateService;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Algorithm.SpeedService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Packets.ClientPackets.CharacterSelectionScreen;
using NosCore.Packets.Enumerations;
using NosCore.Networking.SessionGroup;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class SelectPacketHandlerTests
    {
        private TestWorld World;
        private SelectPacketHandler SelectPacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.ClearPlayerEntity();
            var mates = World.Freeze<Mock<NosCore.GameObject.Services.MateService.IMateService>>();
            mates.Setup(s => s.LoadAsync(It.IsAny<long>())).ReturnsAsync(new List<Mate>());
            SelectPacketHandler = World.Create<SelectPacketHandler>();
        }

        [TestMethod]
        public async Task SelectingValidSlotShouldSelectCharacter()
        {
            await new Spec("Selecting valid slot should select character")
                .WhenAsync(SelectingCharacterInSlot_, 1)
                .Then(CharacterShouldBeSelected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingACharacterKeepsItsSpecialistPoints()
        {
            await new Spec("Selecting a character loads the specialist points it was saved with")
                .GivenAsync(TheCharacterInSlot_HasSpecialistPoints_AndAdditionalPoints_, 1, 4000, 250)
                .WhenAsync(SelectingCharacterInSlot_, 1)
                .Then(TheCharacterShouldHaveSpecialistPoints_AndAdditionalPoints_, 4000, 250)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingACharacterLoadsItsStaticBuffs()
        {
            var staticBuffs = World.Freeze<Mock<NosCore.GameObject.Services.BattleService.IStaticBuffService>>();

            await World.Create<SelectPacketHandler>().ExecuteAsync(new SelectPacket { Slot = 1 }, Session);

            staticBuffs.Verify(s => s.Load(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>()), Times.Once);
        }

        [TestMethod]
        public async Task ASelectionThatFailsReachesThePacketBoundary()
        {
            World.Freeze<Mock<IMateService>>()
                .Setup(s => s.LoadAsync(It.IsAny<long>()))
                .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                World.Create<SelectPacketHandler>().ExecuteAsync(new SelectPacket { Slot = 1 }, Session));
        }

        [TestMethod]
        public async Task SelectingEmptySlotShouldFail()
        {
            await new Spec("Selecting empty slot should fail")
                .WhenAsync(SelectingCharacterInSlot_, 0)
                .Then(CharacterShouldNotBeSelected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingInvalidSlotShouldFail()
        {
            await new Spec("Selecting invalid slot should fail")
                .WhenAsync(SelectingCharacterInSlot_, 5)
                .Then(CharacterShouldNotBeSelected)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SelectingACharacterLoadsTheTimeSpacesItCompleted()
        {
            await new Spec("Selecting a character loads the time-spaces it completed on an earlier session")
                .GivenAsync(TheCharacterInSlot_CompletedTimeSpaces_, 1, new short[] { 4, 6 })
                .WhenAsync(SelectingCharacterInSlot_, 1)
                .Then(TheCharacterShouldHaveCompleted_, new short[] { 4, 6 })
                .ExecuteAsync();
        }

        private async Task TheCharacterInSlot_CompletedTimeSpaces_(int slot, short[] timeSpaces)
        {
            var accountId = Session.Account.AccountId;
            var row = (await World.CharacterDao.FirstOrDefaultAsync(s => s.AccountId == accountId && s.Slot == slot)).NotNull();
            foreach (var timeSpace in timeSpaces)
            {
                await World.CompletedTimeSpaceDao.TryInsertOrUpdateAsync(new CompletedTimeSpaceDto
                {
                    Id = Guid.NewGuid(), CharacterId = row.CharacterId, TimeSpaceId = timeSpace
                }).OrThrowAsync();
            }
        }

        private void TheCharacterShouldHaveCompleted_(short[] timeSpaces) =>
            CollectionAssert.AreEquivalent(timeSpaces, Session.Character.CompletedTimeSpaces?.ToArray() ?? []);

        [TestMethod]
        public async Task SelectingCharacterRestoresSavedOptions()
        {
            await new Spec("Selecting a character restores the options saved on the previous session")
                .GivenAsync(CharacterHasSavedOptions)
                .WhenAsync(SelectingCharacterInSlot_, 1)
                .Then(OptionsShouldBeRestored)
                .And(StatPacketShouldEchoOptions)
                .And(MasterServerShouldKnowWhispersAreBlocked)
                .ExecuteAsync();
        }

        private async Task CharacterHasSavedOptions()
        {
            var accountId = Session.Account.AccountId;
            var row = (await World.CharacterDao.FirstOrDefaultAsync(s => s.AccountId == accountId && s.Slot == 1)).NotNull();
            row.ExchangeBlocked = true;
            row.WhisperBlocked = true;
            row.GroupRequestBlocked = true;
            row.HeroChatBlocked = true;
            row.EmoticonsBlocked = true;
            await World.CharacterDao.TryInsertOrUpdateAsync(row).OrThrowAsync();
        }

        private void OptionsShouldBeRestored()
        {
            Assert.IsTrue(Session.Character.ExchangeBlocked);
            Assert.IsTrue(Session.Character.WhisperBlocked);
            Assert.IsTrue(Session.Character.GroupRequestBlocked);
            Assert.IsTrue(Session.Character.HeroChatBlocked);
            Assert.IsTrue(Session.Character.EmoticonsBlocked);
            Assert.IsFalse(Session.Character.FriendRequestBlocked);
        }

        private void StatPacketShouldEchoOptions()
        {
            var expected = (1 << ((int)GopPacketType.ExchangeBlocked - 1))
                | (1 << ((int)GopPacketType.WhisperBlocked - 1))
                | (1 << ((int)GopPacketType.GroupRequestBlocked - 1))
                | (1 << ((int)GopPacketType.MouseAimLock - 1))
                | (1 << ((int)GopPacketType.HeroChatBlocked - 1))
                | (1 << ((int)GopPacketType.EmoticonsBlocked - 1));
            Assert.AreEqual(expected, Session.Character.GenerateStat().Option);
        }

        private void MasterServerShouldKnowWhispersAreBlocked() =>
            World.PubSubHub.Verify(x => x.SubscribeAsync(It.Is<Subscriber>(s =>
                s.ConnectedCharacter != null && s.ConnectedCharacter.WhisperBlocked)), Times.AtLeastOnce);

        private async Task SelectingCharacterInSlot_(int slot)
        {
            var packet = new SelectPacket
            {
                Slot = (byte)slot
            };
            await SelectPacketHandler.ExecuteAsync(packet, Session);
        }

        private async Task TheCharacterInSlot_HasSpecialistPoints_AndAdditionalPoints_(int slot, int points, int additional)
        {
            var dto = (await World.CharacterDao.FirstOrDefaultAsync(c => c.Slot == slot)).NotNull();
            dto.SpPoint = points;
            dto.SpAdditionPoint = additional;
            await World.CharacterDao.TryInsertOrUpdateAsync(dto).OrThrowAsync();
        }

        private void TheCharacterShouldHaveSpecialistPoints_AndAdditionalPoints_(int points, int additional)
        {
            Assert.AreEqual(points, Session.Character.SpPoint);
            Assert.AreEqual(additional, Session.Character.SpAdditionPoint);
        }

        private void CharacterShouldBeSelected()
        {
            Assert.IsTrue(Session.HasSelectedCharacter);
        }

        private void CharacterShouldNotBeSelected()
        {
            Assert.IsFalse(Session.HasSelectedCharacter);
        }
    }
}
