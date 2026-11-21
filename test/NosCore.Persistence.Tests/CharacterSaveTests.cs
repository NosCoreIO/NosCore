//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.CharacterService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.SaveService;
using NosCore.Tests.Shared;
using SpecLight;
using Entities = NosCore.Database.Entities;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class CharacterSaveTests
    {
        private PostgresDatabase _database;
        private TestWorld World;
        private ClientSession Session;
        private long CharacterId;
        private InventoryItemInstance Sword;
        private InventoryItemInstance Stack;
        private InventoryItemInstance OtherStack;
        private bool _saved;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateAsync();
            World = await TestWorld.CreateAsync(_database);
            Session = await World.CreateSessionAsync();
            CharacterId = Session.Character.CharacterId;

            World.Inject(Dao<Entities.Title, TitleDto, Guid>());
            World.Inject(Dao<Entities.CharacterQuest, CharacterQuestDto, Guid>());
            World.Inject(Dao<Entities.CharacterQuestObjective, CharacterQuestObjectiveDto, Guid>());
            World.Inject(Dao<Entities.Respawn, RespawnDto, long>());
            var minilandId = Guid.NewGuid();
            World.Freeze<Mock<IMinilandService>>().Setup(s => s.GetMiniland(It.IsAny<long>()))
                .Returns(() => new Miniland { MinilandId = minilandId, OwnerId = CharacterId });
            World.Freeze<Mock<IStaticBuffService>>().Setup(s => s.SaveAsync(It.IsAny<PlayerComponentBundle>()))
                .ReturnsAsync(true);
            var mates = World.Freeze<Mock<IMateService>>();
            mates.Setup(s => s.LoadAsync(It.IsAny<long>())).ReturnsAsync(new List<Mate>());
            mates.Setup(s => s.SaveAsync(It.IsAny<IEnumerable<Mate>>())).ReturnsAsync(true);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_database is { } database)
            {
                await database.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task AnInventoryComesBackAsItWasSaved()
        {
            await new Spec("A saved inventory is loaded back with the same items, amounts, places and upgrades")
                .Given(TheCharacterHoldsAnUpgradedSwordAndAStack)
                .WhenAsync(SavingAndLoadingTheCharacter)
                .Then(TheSwordAndTheStackComeBackUnchanged)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemThatLeftTheInventoryIsDeleted()
        {
            await new Spec("An item that left the inventory since the last save is deleted with its instance")
                .Given(TheCharacterHoldsAnUpgradedSwordAndAStack)
                .AndAsync(TheCharacterIsSaved)
                .When(TheStackLeavesTheInventory)
                .AndAsync(SavingTheCharacter)
                .Then(TheSaveSucceeds)
                .AndAsync(TheStackIsNoLongerStored)
                .AndAsync(TheSwordIsStillStored)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TwoItemsThatSwappedPlacesAreSaved()
        {
            await new Spec("Two items that swapped places save without tripping the one-item-per-slot index")
                .Given(TheCharacterHoldsTwoStacksInTheMainPocket)
                .AndAsync(TheCharacterIsSaved)
                .When(TheTwoStacksSwapSlots)
                .AndAsync(SavingTheCharacter)
                .Then(TheSaveSucceeds)
                .AndAsync(TheStoredSlotsAreSwapped)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnInstanceAnotherCharacterStillHoldsIsKept()
        {
            await new Spec("An item instance another character still holds survives this character dropping it")
                .Given(TheCharacterHoldsAnUpgradedSwordAndAStack)
                .AndAsync(TheCharacterIsSaved)
                .AndAsync(AnotherCharacterHoldsTheSameStackInstance)
                .When(TheStackLeavesTheInventory)
                .AndAsync(SavingTheCharacter)
                .Then(TheSaveSucceeds)
                .AndAsync(TheStackInstanceIsStillStoredForTheOtherCharacter)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ASaveTheDatabaseRefusesChangesNothing()
        {
            await new Spec("A save the database refuses near its end leaves the gold and inventory written before it untouched")
                .Given(TheCharacterHoldsAnUpgradedSwordAndAStack)
                .AndAsync(TheCharacterIsSaved)
                .When(TheCharacterEarnsGoldAndLosesTheStack)
                .And(TheCharacterGainsARespawnOfAnUnknownType)
                .AndAsync(SavingTheCharacter)
                .Then(TheSaveFails)
                .AndAsync(TheStoredGoldIsTheOldOne)
                .AndAsync(TheStackIsStillStored)
                .ExecuteAsync();
        }

        private void TheCharacterHoldsAnUpgradedSwordAndAStack()
        {
            var items = World.GenerateItemProvider();
            Sword = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(items.Create(1, 1, 5, 7), CharacterId)).NotNull().Single();
            Stack = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(items.Create(1012, 42), CharacterId)).NotNull().Single();
        }

        private void TheCharacterHoldsTwoStacksInTheMainPocket()
        {
            var items = World.GenerateItemProvider();
            OtherStack = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(items.Create(1013, 3), CharacterId)).NotNull().Single();
            Stack = Session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(items.Create(1012, 42), CharacterId)).NotNull().Single();
        }

        private async Task TheCharacterIsSaved() => Assert.IsTrue(await SaveAsync());

        private async Task SavingTheCharacter() => _saved = await SaveAsync();

        private Task<bool> SaveAsync() => World.Create<SaveService>().SaveAsync(Session);

        private async Task SavingAndLoadingTheCharacter()
        {
            await TheCharacterIsSaved();
            var row = (await World.CharacterDao.FirstOrDefaultAsync(c => c.CharacterId == CharacterId)).NotNull();
            Session.ClearPlayerEntity();
            await World.Create<CharacterLoadService>().LoadAsync(Session, row);
        }

        private void TheSwordAndTheStackComeBackUnchanged()
        {
            var inventory = Session.Character.InventoryService.Values.OrderBy(i => i.ItemInstance.ItemVNum).ToList();
            Assert.HasCount(2, inventory);
            AssertSame(Sword, inventory[0]);
            Assert.AreEqual((short)5, inventory[0].ItemInstance.Rare);
            Assert.AreEqual((byte)7, inventory[0].ItemInstance.Upgrade);
            AssertSame(Stack, inventory[1]);
            Assert.AreEqual((short)42, inventory[1].ItemInstance.Amount);
        }

        private static void AssertSame(InventoryItemInstance expected, InventoryItemInstance loaded)
        {
            Assert.AreEqual(expected.Id, loaded.Id);
            Assert.AreEqual(expected.ItemInstanceId, loaded.ItemInstanceId);
            Assert.AreEqual(expected.ItemInstance.ItemVNum, loaded.ItemInstance.ItemVNum);
            Assert.AreEqual(expected.Slot, loaded.Slot);
            Assert.AreEqual(expected.Type, loaded.Type);
        }

        private void TheStackLeavesTheInventory()
        {
            Session.Character.InventoryService.DeleteById(Stack.ItemInstanceId);
            Assert.IsFalse(Session.Character.InventoryService.ContainsKey(Stack.ItemInstanceId));
        }

        private void TheTwoStacksSwapSlots()
        {
            (OtherStack.Slot, Stack.Slot) = (Stack.Slot, OtherStack.Slot);
        }

        private void TheCharacterEarnsGoldAndLosesTheStack()
        {
            Session.Character.Gold += 1000;
            TheStackLeavesTheInventory();
        }

        private void TheCharacterGainsARespawnOfAnUnknownType() =>
            Session.Character.Respawns.Add(new RespawnDto
            {
                CharacterId = CharacterId, MapId = 1, RespawnMapTypeId = (RespawnType)999, X = 1, Y = 1
            });

        private void TheSaveSucceeds() => Assert.IsTrue(_saved);

        private void TheSaveFails() => Assert.IsFalse(_saved);

        private async Task AnotherCharacterHoldsTheSameStackInstance()
        {
            var other = await World.CreateSessionAsync();
            await using var context = _database.CreateContext();
            context.Set<Entities.InventoryItemInstance>().Add(new Entities.InventoryItemInstance
            {
                Id = Guid.NewGuid(),
                CharacterId = other.Character.CharacterId,
                ItemInstanceId = Stack.ItemInstanceId,
                Slot = 0,
                Type = NoscorePocketType.Main
            });
            await context.SaveChangesAsync();
        }

        private async Task TheStackIsNoLongerStored()
        {
            await using var context = _database.CreateContext();
            Assert.IsFalse(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.Id == Stack.Id));
            Assert.IsFalse(await context.Set<Entities.ItemInstance>().AnyAsync(i => i.Id == Stack.ItemInstanceId));
        }

        private async Task TheStackIsStillStored()
        {
            await using var context = _database.CreateContext();
            Assert.IsTrue(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.Id == Stack.Id));
            Assert.IsTrue(await context.Set<Entities.ItemInstance>().AnyAsync(i => i.Id == Stack.ItemInstanceId));
        }

        private async Task TheSwordIsStillStored()
        {
            await using var context = _database.CreateContext();
            Assert.IsTrue(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.Id == Sword.Id));
        }

        private async Task TheStackInstanceIsStillStoredForTheOtherCharacter()
        {
            await using var context = _database.CreateContext();
            Assert.IsFalse(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.Id == Stack.Id));
            Assert.IsTrue(await context.Set<Entities.InventoryItemInstance>()
                .AnyAsync(i => i.ItemInstanceId == Stack.ItemInstanceId && i.CharacterId != CharacterId));
            Assert.IsTrue(await context.Set<Entities.ItemInstance>().AnyAsync(i => i.Id == Stack.ItemInstanceId));
        }

        private async Task TheStoredSlotsAreSwapped()
        {
            await using var context = _database.CreateContext();
            var slots = await context.Set<Entities.InventoryItemInstance>()
                .Where(i => i.CharacterId == CharacterId)
                .ToDictionaryAsync(i => i.Id, i => i.Slot);
            Assert.AreEqual(OtherStack.Slot, slots[OtherStack.Id]);
            Assert.AreEqual(Stack.Slot, slots[Stack.Id]);
            Assert.AreNotEqual(Stack.Slot, OtherStack.Slot);
        }

        private async Task TheStoredGoldIsTheOldOne()
        {
            await using var context = _database.CreateContext();
            var character = await context.Set<Entities.Character>().SingleAsync(c => c.CharacterId == CharacterId);
            Assert.AreEqual(Session.Character.Gold - 1000, character.Gold);
        }

        private IDao<TDto, TPk> Dao<TEntity, TDto, TPk>() where TEntity : class where TPk : struct =>
            new Dao<TEntity, TDto, TPk>(NullLogger<Dao<TEntity, TDto, TPk>>.Instance, World.DbContextBuilder);
    }
}
