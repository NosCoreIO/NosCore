//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SpecLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SaveService
{
    [TestClass]
    public class SaveServiceTests
    {
        private TestWorld World;
        private ClientSession Session;
        private ISaveService Service;
        private IItemGenerationService ItemProvider;
        private IDao<CharacterQuestObjectiveDto, Guid> CharacterQuestObjectiveDao;
        private IDao<IItemInstanceDto?, Guid> ItemInstanceDao;
        private IDao<InventoryItemInstanceDto, Guid> InventoryItemInstanceDao;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            ItemProvider = World.GenerateItemProvider();

            // Every DAO has to share one database: inside the transaction scope they all
            // write through the scope's context.
            var contextBuilder = World.DbContextBuilder;

            var itemInstanceDao = new Dao<ItemInstance, IItemInstanceDto?, Guid>(NullLogger<Dao<ItemInstance, IItemInstanceDto?, Guid>>.Instance, contextBuilder);
            var inventoryItemInstanceDao = new Dao<Database.Entities.InventoryItemInstance, InventoryItemInstanceDto, Guid>(NullLogger<Dao<Database.Entities.InventoryItemInstance, InventoryItemInstanceDto, Guid>>.Instance, contextBuilder);
            var staticBonusDao = new Dao<StaticBonus, StaticBonusDto, long>(NullLogger<Dao<StaticBonus, StaticBonusDto, long>>.Instance, contextBuilder);
            var quicklistEntriesDao = new Dao<QuicklistEntry, QuicklistEntryDto, Guid>(NullLogger<Dao<QuicklistEntry, QuicklistEntryDto, Guid>>.Instance, contextBuilder);
            var titleDao = new Dao<Title, TitleDto, Guid>(NullLogger<Dao<Title, TitleDto, Guid>>.Instance, contextBuilder);
            var characterQuestDao = new Dao<Database.Entities.CharacterQuest, CharacterQuestDto, Guid>(NullLogger<Dao<Database.Entities.CharacterQuest, CharacterQuestDto, Guid>>.Instance, contextBuilder);
            var characterQuestObjectiveDao = new Dao<Database.Entities.CharacterQuestObjective, CharacterQuestObjectiveDto, Guid>(NullLogger<Dao<Database.Entities.CharacterQuestObjective, CharacterQuestObjectiveDto, Guid>>.Instance, contextBuilder);
            CharacterQuestObjectiveDao = characterQuestObjectiveDao;
            ItemInstanceDao = itemInstanceDao;
            InventoryItemInstanceDao = inventoryItemInstanceDao;
            var respawnDao = new Dao<Database.Entities.Respawn, RespawnDto, long>(NullLogger<Dao<Database.Entities.Respawn, RespawnDto, long>>.Instance, contextBuilder);

            var minilandService = new Mock<IMinilandService>();
            minilandService.Setup(s => s.GetMiniland(It.IsAny<long>()))
                .Returns(new NosCore.GameObject.Services.MinilandService.Miniland { MinilandMessage = "Test" });

            Service = new GameObject.Services.SaveService.SaveService(
                World.CharacterDao,
                itemInstanceDao,
                inventoryItemInstanceDao,
                World.AccountDao,
                staticBonusDao,
                quicklistEntriesDao,
                World.MinilandDao,
                minilandService.Object,
                titleDao,
                characterQuestDao,
                characterQuestObjectiveDao,
                respawnDao,
                NullLogger<NosCore.GameObject.Services.SaveService.SaveService>.Instance,
                World.LogLanguageLocalizer,
                new NosCore.GameObject.Services.MateService.MateService(World.MateDao,
                    new List<NpcMonsterDto>(),
                    new NosCore.Core.Services.IdService.IdService<NosCore.GameObject.Services.MateService.Mate>(2000000),
                    new NosCore.Algorithm.MateExperienceService.MateExperienceService(),
                    NullLogger<NosCore.GameObject.Services.MateService.MateService>.Instance),
                new DaoTransactionScope(contextBuilder));
        }

        [TestMethod]
        public async Task SavingWritesThePetsCurrentState()
        {
            var pet = await OwnedPetAsync();
            pet.Loyalty = 2500;
            pet.Experience = 9;

            await Service.SaveAsync(Session);

            var row = (await World.MateDao.FirstOrDefaultAsync(s => s.MateId == pet.MateId)).NotNull();
            Assert.AreEqual(2500, row.Loyalty);
            Assert.AreEqual(9, row.Experience);
        }

        [TestMethod]
        public async Task SavingTwiceKeepsOneRow()
        {
            await OwnedPetAsync();

            await Service.SaveAsync(Session);
            await Service.SaveAsync(Session);

            var characterId = Session.Character.CharacterId;
            Assert.AreEqual(1, World.MateDao.Where(s => s.CharacterId == characterId).NotNull().Count());
        }

        private async Task<NosCore.GameObject.Services.MateService.Mate> OwnedPetAsync()
        {
            var pet = TestMates.Pet(Session.Character.CharacterId);
            var row = await World.MateDao.TryInsertOrUpdateAsync(pet.Adapt<MateDto>());
            pet.MateId = row.MateId;
            Session.Character.Mates[pet.MateTransportId] = pet;
            return pet;
        }

        [TestMethod]
        public async Task SavingCharacterShouldPersistData()
        {
            await new Spec("Saving character should persist data")
                .GivenAsync(CharacterHasModifiedGold)
                .WhenAsync(SavingCharacter)
                .ThenAsync(GoldShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingCharacterShouldPersistOptions()
        {
            await new Spec("Saving character should persist the options window choices")
                .Given(CharacterHasChangedOptions)
                .WhenAsync(SavingCharacter)
                .ThenAsync(OptionsShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingCharacterShouldPersistInventory()
        {
            await new Spec("Saving character should persist inventory")
                .GivenAsync(CharacterHasItemsInInventory)
                .WhenAsync(SavingCharacter)
                .ThenAsync(InventoryShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingQuestObjectiveProgressShouldRoundTrip()
        {
            await new Spec("Quest objective progress should persist across reloads")
                .GivenAsync(CharacterHasQuestWithProgress)
                .WhenAsync(SavingCharacter)
                .ThenAsync(ObjectiveProgressShouldBePersisted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UpdatingObjectiveProgressShouldOverwrite()
        {
            await new Spec("Updating quest objective should overwrite the earlier count")
                .GivenAsync(CharacterHasQuestWithProgress)
                .WhenAsync(SavingCharacter)
                .GivenAsync(ProgressAdvances)
                .WhenAsync(SavingCharacter)
                .ThenAsync(ObjectiveProgressShouldReflectLatestCount)
                .ExecuteAsync();
        }

        private Guid _trackedCharacterQuestId;
        private Guid _trackedObjectiveId;

        private async Task CharacterHasQuestWithProgress()
        {
            _trackedCharacterQuestId = Guid.NewGuid();
            _trackedObjectiveId = Guid.NewGuid();
            var cq = new GameObject.Services.QuestService.CharacterQuest
            {
                Id = _trackedCharacterQuestId,
                CharacterId = Session.Character.VisualId,
                QuestId = 1500,
                Quest = new GameObject.Services.QuestService.Quest { QuestId = 1500, QuestObjectives = new List<QuestObjectiveDto>() }
            };
            cq.ObjectiveProgress[_trackedObjectiveId] = 2;
            Session.Character.Quests = new ConcurrentDictionary<Guid, GameObject.Services.QuestService.CharacterQuest>(
                new[] { new KeyValuePair<Guid, GameObject.Services.QuestService.CharacterQuest>(_trackedCharacterQuestId, cq) });
        }

        private async Task ProgressAdvances()
        {
            Session.Character.Quests[_trackedCharacterQuestId].ObjectiveProgress[_trackedObjectiveId] = 5;
        }

        private async Task ObjectiveProgressShouldBePersisted()
        {
            var rows = CharacterQuestObjectiveDao
                .Where(o => o.CharacterQuestId == _trackedCharacterQuestId).NotNull()
                .ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(2, rows[0].Count);
        }

        private async Task ObjectiveProgressShouldReflectLatestCount()
        {
            var rows = CharacterQuestObjectiveDao
                .Where(o => o.CharacterQuestId == _trackedCharacterQuestId).NotNull()
                .ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(5, rows[0].Count);
        }

        [TestMethod]
        public async Task SavingKeepsAnItemInstanceAnotherInventoryStillHolds()
        {
            await new Spec("Saving keeps an item instance another inventory still holds")
                .GivenAsync(CharacterHasItemsInInventory)
                .AndAsync(SavingCharacter)
                .AndAsync(AnotherCharactersInventoryHoldsTheSameInstance)
                .And(TheCharacterNoLongerHoldsTheItem)
                .WhenAsync(SavingCharacter)
                .ThenAsync(TheSharedInstanceShouldStillExist)
                .ExecuteAsync();
        }

        private Guid _sharedInstanceId;

        private async Task AnotherCharactersInventoryHoldsTheSameInstance()
        {
            var held = Session.Character.InventoryService.Values.Single();
            _sharedInstanceId = held.ItemInstanceId;
            var other = await World.GenerateSessionAsync();
            await InventoryItemInstanceDao.TryInsertOrUpdateAsync(new InventoryItemInstanceDto
            {
                Id = Guid.NewGuid(),
                CharacterId = other.Character.CharacterId,
                ItemInstanceId = _sharedInstanceId,
                Slot = 0,
                Type = held.Type
            });
        }

        private void TheCharacterNoLongerHoldsTheItem()
        {
            Session.Character.InventoryService.Remove(_sharedInstanceId);
        }

        private async Task TheSharedInstanceShouldStillExist()
        {
            var id = _sharedInstanceId;
            Assert.IsNotNull(await ItemInstanceDao.FirstOrDefaultAsync(s => s.Id == id));
        }

        [TestMethod]
        public async Task SavingNonCharacterEntityShouldNotThrow()
        {
            await new Spec("Saving non-character entity should not throw")
                .WhenAsync(SavingNonCharacterEntity)
                .Then(NoExceptionShouldBeThrown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingOnBaseMapPersistsPositionAsMapXY()
        {
            await new Spec("Saving while on a BaseMap writes PositionX/Y to MapX/Y")
                .GivenAsync(CharacterIsOnBaseMap)
                .And(CharacterPositionIs_, (short)50, (short)60)
                .WhenAsync(SavingCharacter)
                .ThenAsync(PersistedMapXYShouldBe_, (short)50, (short)60)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SavingInsideMinilandKeepsPreviousMapXYValue()
        {
            await new Spec("Saving while on a non-base instance preserves the previously persisted MapX/Y")
                .GivenAsync(CharacterIsOnBaseMapAtPosition_, (short)48, (short)132)
                .WhenAsync(SavingCharacter)
                .Given(CharacterIsNowInsideAMinilandAtPosition_, (short)5, (short)8)
                .WhenAsync(SavingCharacter)
                .ThenAsync(PersistedMapXYShouldBe_, (short)48, (short)132)
                .ExecuteAsync();
        }

        private async Task CharacterIsOnBaseMap()
        {
            Session.Character.MapInstance.MapInstanceType = MapInstanceType.BaseMapInstance;
        }

        private async Task CharacterIsOnBaseMapAtPosition_(short x, short y)
        {
            Session.Character.MapInstance.MapInstanceType = MapInstanceType.BaseMapInstance;
            Session.Character.PositionX = x;
            Session.Character.PositionY = y;
        }

        private void CharacterPositionIs_(short x, short y)
        {
            Session.Character.PositionX = x;
            Session.Character.PositionY = y;
        }

        private void CharacterIsNowInsideAMinilandAtPosition_(short x, short y)
        {
            Session.Character.MapInstance.MapInstanceType = MapInstanceType.NormalInstance;
            Session.Character.PositionX = x;
            Session.Character.PositionY = y;
        }

        private async Task PersistedMapXYShouldBe_(short expectedX, short expectedY)
        {
            var characterId = Session.Character.CharacterId;
            var persisted = await World.CharacterDao.FirstOrDefaultAsync(c =>
                c.CharacterId == characterId);
            Assert.IsNotNull(persisted);
            Assert.AreEqual(expectedX, persisted.MapX);
            Assert.AreEqual(expectedY, persisted.MapY);
        }

        private bool SaveCompleted;

        private async Task CharacterHasModifiedGold()
        {
            Session.Character.Gold = 999999;
        }

        private async Task CharacterHasItemsInInventory()
        {
            var item = ItemProvider.Create(1012, 50);
            Session.Character.InventoryService.AddItemToPocket(
                GameObject.Services.InventoryService.InventoryItemInstance.Create(item, Session.Character.CharacterId));
        }

        private async Task SavingCharacter()
        {
            await Service.SaveAsync(Session);
            SaveCompleted = true;
        }

        private async Task SavingNonCharacterEntity()
        {
            await Service.SaveAsync(Session);
            SaveCompleted = true;
        }

        private async Task GoldShouldBePersisted()
        {
            var characterId = Session.Character.CharacterId;
            var character = await World.CharacterDao.FirstOrDefaultAsync(c =>
                c.CharacterId == characterId);
            Assert.IsNotNull(character);
            Assert.AreEqual(999999, character.Gold);
        }

        private async Task InventoryShouldBePersisted()
        {
            var characterId = Session.Character.CharacterId;
            var character = await World.CharacterDao.FirstOrDefaultAsync(c =>
                c.CharacterId == characterId);
            Assert.IsNotNull(character);
        }

        private void NoExceptionShouldBeThrown()
        {
            Assert.IsTrue(SaveCompleted);
        }

        private void CharacterHasChangedOptions()
        {
            foreach (var option in Enum.GetValues<GopPacketType>())
            {
                Session.Character.SetOption(option, true);
            }
        }

        private async Task OptionsShouldBePersisted()
        {
            var characterId = Session.Character.CharacterId;
            var row = (await World.CharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId)).NotNull();
            Assert.IsTrue(row.ExchangeBlocked);
            Assert.IsTrue(row.FriendRequestBlocked);
            Assert.IsTrue(row.FamilyRequestBlocked);
            Assert.IsTrue(row.WhisperBlocked);
            Assert.IsTrue(row.GroupRequestBlocked);
            Assert.IsTrue(row.PetAutoRelive);
            Assert.IsTrue(row.PartnerAutoRelive);
            Assert.IsTrue(row.GroupSharing);
            Assert.IsTrue(row.MouseAimLock);
            Assert.IsTrue(row.HeroChatBlocked);
            Assert.IsTrue(row.QuickGetUp);
            Assert.IsTrue(row.EmoticonsBlocked);
            Assert.IsTrue(row.HpBlocked);
            Assert.IsTrue(row.BuffBlocked);
            Assert.IsTrue(row.MinilandInviteBlocked);
            Assert.IsTrue(row.HatInvisible);
            Assert.IsTrue(row.LockInterface);
        }
    }
}
