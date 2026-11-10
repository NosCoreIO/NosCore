using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Npgsql;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Tests.Shared;
using InventoryRow = NosCore.Database.Entities.InventoryItemInstance;

namespace NosCore.GameObject.Tests.Services.SaveService;

[TestClass]
public class InventoryPersistencePostgresTests
{
    /// <summary>Verifies real slot constraints, preserved references and whole-save rollback.</summary>
    [TestMethod]
    [DataRow(2, false, false)]
    [DataRow(3, false, false)]
    [DataRow(3, true, false)]
    [DataRow(3, false, true)]
    public async Task SlotExchangesSaveAtomically(int count, bool failAfterInventory, bool invalidSlots)
    {
        await TestHelpers.ResetAsync();
        var session = await TestHelpers.Instance.GenerateSessionAsync();
        var characterId = session.Character.CharacterId;
        var schema = "inventory_test_" + Guid.NewGuid().ToString("N");
        var connectionString = Environment.GetEnvironmentVariable("NOSCORE_TEST_POSTGRES");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connectionString), "Set NOSCORE_TEST_POSTGRES to a disposable PostgreSQL test database; CI supplies one.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE SCHEMA {schema}", connection))
            await command.ExecuteNonQueryAsync();
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema };
            var options = new DbContextOptionsBuilder<InventoryContext>().UseNpgsql(builder.ConnectionString).Options;
            Func<DbContext> factory = () => new InventoryContext(options);
            await using var context = new InventoryContext(options);
            await context.Database.ExecuteSqlRawAsync(context.Database.GenerateCreateScript());
            var inventoryDao = new Dao<InventoryRow, InventoryItemInstanceDto, Guid>(NullLogger<Dao<InventoryRow, InventoryItemInstanceDto, Guid>>.Instance, factory);
            var characterDao = new Dao<SavedCharacter, CharacterDto, long>(NullLogger<Dao<SavedCharacter, CharacterDto, long>>.Instance, factory);
            var mates = new Mock<IMateService>();
            mates.Setup(m => m.SaveAsync(It.IsAny<IEnumerable<NosCore.GameObject.Services.MateService.Mate>>())).ReturnsAsync(true);
            var miniland = new Mock<IMinilandService>();
            miniland.Setup(m => m.GetMiniland(It.IsAny<long>())).Returns(new NosCore.GameObject.Services.MinilandService.Miniland());
            var service = new NosCore.GameObject.Services.SaveService.SaveService(
                characterDao, SuccessfulDao<IItemInstanceDto?, Guid>(), inventoryDao,
                SuccessfulDao<AccountDto, long>(), SuccessfulDao<StaticBonusDto, long>(),
                SuccessfulDao<QuicklistEntryDto, Guid>(), SuccessfulDao<MinilandDto, Guid>(), miniland.Object,
                SuccessfulDao<TitleDto, Guid>(), SuccessfulDao<CharacterQuestDto, Guid>(),
                SuccessfulDao<CharacterQuestObjectiveDto, Guid>(), SuccessfulDao<RespawnDto, long>(),
                NullLogger<NosCore.GameObject.Services.SaveService.SaveService>.Instance,
                TestHelpers.Instance.LogLanguageLocalizer, mates.Object, new DaoTransactionScope(factory));
            var inventory = session.Character.InventoryService;
            inventory.Expensions[NoscorePocketType.Main] = 1;
            var provider = TestHelpers.Instance.GenerateItemProvider();
            for (short slot = 0; slot < count; slot++)
            {
                var item = NosCore.GameObject.Services.InventoryService.InventoryItemInstance.Create(provider.Create((short)(1012 + slot), 1), session.Character.CharacterId);
                Assert.IsNotNull(inventory.AddItemToPocket(item, NoscorePocketType.Main, slot));
            }
            var items = inventory.Values.OrderBy(i => i.Slot).ToArray();
            session.Character.Gold = 10;
            session.Character.LevelXp = 20;
            await service.SaveAsync(session);
            Assert.AreEqual(count, await context.Set<InventoryRow>().CountAsync());
            context.Set<InventoryReference>().Add(new InventoryReference { Id = Guid.NewGuid(), InventoryId = items[0].Id });
            await context.SaveChangesAsync();

            // Prove this provider reproduces the original failure before exercising SaveService.
            await using (var transaction = new DaoTransactionScope(factory).Begin())
            {
                var desired = inventoryDao.Where(i => i.CharacterId == characterId)!.ToArray();
                foreach (var row in desired) row.Slot = (short)((row.Slot + 1) % count);
                Assert.IsFalse(await inventoryDao.TryInsertOrUpdateAsync(desired));
            }
            foreach (var item in items) item.Slot = (short)((item.Slot + 1) % count);
            if (invalidSlots) items[0].Slot = items[1].Slot;
            var rollback = failAfterInventory || invalidSlots;
            session.Character.Gold = 444;
            session.Character.LevelXp = 123;
            mates.Setup(m => m.SaveAsync(It.IsAny<IEnumerable<NosCore.GameObject.Services.MateService.Mate>>())).ReturnsAsync(!failAfterInventory);
            await service.SaveAsync(session);

            var rows = await context.Set<InventoryRow>().AsNoTracking().ToListAsync();
            Assert.AreEqual(count, rows.Count);
            for (var index = 0; index < count; index++)
            {
                var row = rows.Single(r => r.Id == items[index].Id);
                Assert.AreEqual(rollback ? index : (index + 1) % count, row.Slot);
                Assert.AreEqual(items[index].ItemInstanceId, row.ItemInstanceId);
            }
            Assert.IsFalse(rows.Any(r => r.Slot < 0));
            var progress = await context.Set<SavedCharacter>().AsNoTracking().SingleAsync();
            Assert.AreEqual(rollback ? 10 : 444, progress.Gold);
            Assert.AreEqual(rollback ? 20 : 123, progress.LevelXp);
            Assert.AreEqual(items[0].Id, (await context.Set<InventoryReference>().SingleAsync()).InventoryId);
        }
        finally
        {
            await using var command = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Isolates unrelated save participants while keeping real inventory and progress writes.</summary>
    private static IDao<TDto, TKey> SuccessfulDao<TDto, TKey>() where TKey : struct
    {
        var dao = new Mock<IDao<TDto, TKey>>();
        dao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<TDto>())).ReturnsAsync((TDto row) => row);
        dao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<TDto>>())).ReturnsAsync(true);
        dao.Setup(d => d.TryDeleteAsync(It.IsAny<IEnumerable<TKey>>())).ReturnsAsync(Array.Empty<TDto>());
        dao.Setup(d => d.Where(It.IsAny<System.Linq.Expressions.Expression<Func<TDto, bool>>>())).Returns(Array.Empty<TDto>());
        return dao.Object;
    }

    private sealed class SavedCharacter
    {
        public long CharacterId { get; set; }
        public long Gold { get; set; }
        public long LevelXp { get; set; }
    }

    private sealed class InventoryReference
    {
        public Guid Id { get; set; }
        public Guid InventoryId { get; set; }
    }

    private sealed class InventoryContext(DbContextOptions<InventoryContext> options) : DbContext(options)
    {
        /// <summary>Models the production inventory index and a restricted inventory reference.</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<NosCore.Database.Entities.Character>();
            modelBuilder.Ignore<NosCore.Database.Entities.ItemInstance>();
            modelBuilder.Ignore<NosCore.Database.Entities.MinilandObject>();
            var inventory = modelBuilder.Entity<InventoryRow>();
            inventory.Ignore(i => i.Character);
            inventory.Ignore(i => i.ItemInstance);
            inventory.Ignore(i => i.MinilandObject);
            inventory.HasIndex(i => new { i.CharacterId, i.Slot, i.Type }).IsUnique();
            modelBuilder.Entity<SavedCharacter>().HasKey(c => c.CharacterId);
            modelBuilder.Entity<InventoryReference>().HasOne<InventoryRow>().WithMany().HasForeignKey(r => r.InventoryId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
