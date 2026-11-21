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
using NodaTime;
using NosCore.Dao;
using NosCore.Data.Dto;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.TransferService;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Tests.Shared;
using SpecLight;
using Entities = NosCore.Database.Entities;

namespace NosCore.Persistence.Tests
{
    [TestClass]
    public class TradeTransactionTests
    {
        private const long GiverGold = 1000;
        private const long OfferedGold = 400;

        private PostgresDatabase _database;
        private TestWorld World;
        private ClientSession Giver;
        private ClientSession Receiver;
        private long GiverId;
        private long ReceiverId;
        private InventoryItemInstance Stack;
        private List<KeyValuePair<long, IvnPacket>>? _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _database = await PostgresDatabase.CreateAsync();
            World = await TestWorld.CreateAsync(_database);
            World.WorldConfiguration.Value.MaxGoldAmount = 1_000_000;
            World.WorldConfiguration.Value.MaxBankGoldAmount = 1_000_000;
            World.Inject<NosCore.Dao.Interfaces.IDao<EquipmentOptionDto, Guid>>(
                new Dao<Entities.EquipmentOption, EquipmentOptionDto, Guid>(
                    NullLogger<Dao<Entities.EquipmentOption, EquipmentOptionDto, Guid>>.Instance, World.DbContextBuilder));
            Giver = await World.CreateSessionAsync();
            Receiver = await World.CreateSessionAsync();
            GiverId = Giver.Character.CharacterId;
            ReceiverId = Receiver.Character.CharacterId;
            Receiver.Character.Gold = 0;
            Receiver.Character.InventoryService.Clear();
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
        public async Task ATradeIsStoredOnBothSides()
        {
            await new Spec("A trade of gold and a whole stack is stored for both characters")
                .GivenAsync(TheGiverHasGoldAndAStoredStack)
                .WhenAsync(TheGiverTradesGoldAndTheWholeStack)
                .Then(TheTradeGoesThrough)
                .AndAsync(TheStoredGoldHasMoved)
                .AndAsync(TheStoredStackHasMoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATradeTheDatabaseRefusesAtItsLastWriteLeavesNoPartialRows()
        {
            await new Spec("A trade whose last write is refused leaves the gold and items of both characters as they were")
                .GivenAsync(TheGiverHasGoldAndAStoredStack)
                .AndAsync(AParcelStillPointsAtTheStackInstance)
                .WhenAsync(TheGiverTradesGoldAndTheWholeStack)
                .Then(TheTradeIsRefused)
                .AndAsync(TheStoredGoldHasNotMoved)
                .AndAsync(TheStoredStackHasNotMoved)
                .And(BothInventoriesAreAsBeforeTheTrade)
                .ExecuteAsync();
        }

        private async Task TheGiverHasGoldAndAStoredStack()
        {
            Giver.Character.Gold = GiverGold;
            Giver.Character.CharacterDto.Gold = GiverGold;
            Assert.IsNotNull(await World.CharacterDao.TryInsertOrUpdateAsync(Giver.Character.CharacterDto));
            Giver.Character.InventoryService.Clear();
            Stack = Giver.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(World.GenerateItemProvider().Create(1012, 10), GiverId))
                .NotNull().Single();
            Assert.IsNotNull(await World.ItemInstanceDao.TryInsertOrUpdateAsync(Stack.ItemInstance));
            Assert.IsNotNull(await World.InventoryItemInstanceDao.TryInsertOrUpdateAsync(Stack));
        }

        private async Task AParcelStillPointsAtTheStackInstance()
        {
            await using var context = _database.CreateContext();
            context.Set<Entities.Mail>().Add(new Entities.Mail
            {
                ReceiverId = GiverId,
                ItemInstanceId = Stack.ItemInstanceId,
                Title = "parcel",
                Message = "parcel",
                Date = Instant.FromUnixTimeSeconds(0)
            });
            await context.SaveChangesAsync();
        }

        private async Task TheGiverTradesGoldAndTheWholeStack() =>
            _result = await World.Create<TransferService>().TransferAsync(
                Giver.Character, new TransferOffer(OfferedGold, 0, new Dictionary<InventoryItemInstance, short> { [Stack] = 10 }),
                Receiver.Character, new TransferOffer(0, 0, new Dictionary<InventoryItemInstance, short>()));

        private void TheTradeGoesThrough() => Assert.IsNotNull(_result);

        private void TheTradeIsRefused() => Assert.IsNull(_result);

        private async Task TheStoredGoldHasMoved()
        {
            Assert.AreEqual(GiverGold - OfferedGold, await StoredGoldAsync(GiverId));
            Assert.AreEqual(OfferedGold, await StoredGoldAsync(ReceiverId));
        }

        private async Task TheStoredGoldHasNotMoved()
        {
            Assert.AreEqual(GiverGold, await StoredGoldAsync(GiverId));
            Assert.AreEqual(0, await StoredGoldAsync(ReceiverId));
        }

        private async Task TheStoredStackHasMoved()
        {
            await using var context = _database.CreateContext();
            Assert.IsFalse(await context.Set<Entities.ItemInstance>().AnyAsync(i => i.Id == Stack.ItemInstanceId));
            Assert.IsFalse(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.CharacterId == GiverId));
            var received = await context.Set<Entities.InventoryItemInstance>().Include(i => i.ItemInstance)
                .SingleAsync(i => i.CharacterId == ReceiverId);
            Assert.AreEqual((short)1012, received.ItemInstance.ItemVNum);
            Assert.AreEqual((short)10, received.ItemInstance.Amount);
        }

        private async Task TheStoredStackHasNotMoved()
        {
            await using var context = _database.CreateContext();
            Assert.IsTrue(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.Id == Stack.Id));
            Assert.AreEqual((short)10, (await context.Set<Entities.ItemInstance>().SingleAsync(i => i.Id == Stack.ItemInstanceId)).Amount);
            Assert.IsFalse(await context.Set<Entities.InventoryItemInstance>().AnyAsync(i => i.CharacterId == ReceiverId));
            Assert.AreEqual(1, await context.Set<Entities.ItemInstance>().CountAsync());
        }

        private void BothInventoriesAreAsBeforeTheTrade()
        {
            Assert.AreEqual(GiverGold, Giver.Character.Gold);
            Assert.AreEqual(0, Receiver.Character.Gold);
            Assert.IsTrue(Giver.Character.InventoryService.ContainsKey(Stack.ItemInstanceId));
            Assert.AreEqual((short)10, Stack.ItemInstance.Amount);
            Assert.IsEmpty(Receiver.Character.InventoryService.Values);
        }

        private async Task<long> StoredGoldAsync(long characterId)
        {
            await using var context = _database.CreateContext();
            return (await context.Set<Entities.Character>().SingleAsync(c => c.CharacterId == characterId)).Gold;
        }
    }
}
