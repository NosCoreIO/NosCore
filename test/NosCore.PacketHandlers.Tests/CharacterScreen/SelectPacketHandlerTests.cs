//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.CharacterScreen
{
    [TestClass]
    public class SelectPacketHandlerTests
    {
        private TestWorld World;
        private static readonly ILogger<SelectPacketHandler> Logger = new Mock<ILogger<SelectPacketHandler>>().Object;
        private SelectPacketHandler SelectPacketHandler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.ClearPlayerEntity();
            SelectPacketHandler = new SelectPacketHandler(
                World.CharacterDao,
                Logger,
                NullLoggerFactory.Instance,
                new Mock<IItemGenerationService>().Object,
                World.MapInstanceAccessorService,
                new Mock<IDao<IItemInstanceDto?, Guid>>().Object,
                new Mock<IDao<InventoryItemInstanceDto, Guid>>().Object,
                new Mock<IDao<StaticBonusDto, long>>().Object,
                new Mock<IDao<QuicklistEntryDto, Guid>>().Object,
                new Mock<IDao<TitleDto, Guid>>().Object,
                new Mock<IDao<CharacterQuestDto, Guid>>().Object,
                new Mock<IDao<CharacterQuestObjectiveDto, Guid>>().Object,
                new Mock<IDao<RespawnDto, long>>().Object,
                new Mock<IDao<ScriptDto, Guid>>().Object,
                new List<QuestDto>(),
                new List<QuestObjectiveDto>(),
                World.WorldConfiguration,
                World.LogLanguageLocalizer,
                World.PubSubHub.Object,
                World.Clock,
                new List<ItemDto>(),
                new HpService(),
                new MpService(),
                new SpeedService(),
                new Mock<NosCore.GameObject.Services.BattleService.IVitalityService>().Object,
                new Mock<ISessionGroupFactory>().Object,
                World.Create<CharacterInitializationService>(),
                new Mock<Wolverine.IMessageBus>().Object,
                new Mock<NosCore.GameObject.Services.MateService.IMateService>().Object,
                new Mock<NosCore.GameObject.Services.FamilyService.IFamilyService>().Object);
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
            await World.CharacterDao.TryInsertOrUpdateAsync(row);
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
            await World.CharacterDao.TryInsertOrUpdateAsync(dto);
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
