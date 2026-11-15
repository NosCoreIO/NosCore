//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class CapturedNpcRerunTests
    {
        private static List<IPacket> Capture() =>
        [
            Captured.Arrival(1, 10, 10),
            Captured.Npc(1, 5, 10, 10),
            Captured.Shop(5, 0, 3, "Shop"),
            Captured.Shopping(1, 5),
            Captured.ItemShop(5,
                new NInvItemSubPacket { Type = PocketType.Main, Slot = 4, VNum = 1002, Price = 500 },
                new NInvItemSubPacket { Type = PocketType.Main, Slot = 2, VNum = 1003, Price = 500 }),
            Captured.Talk(NrunRunnerType.OpenProduction, 17, 5),
            Captured.Makes(1002),
            Captured.ShowRecipe(1002),
            Captured.Materials(1, (2029, 5)),
            Captured.Talk(NrunRunnerType.Teleport, 1, 5),
            Captured.Arrival(20, 8, 87),
        ];

        private sealed class Database
        {
            private static IDao<TDto, TPk> Dao<TEntity, TDto, TPk>(Func<NosCoreContext> context) where TEntity : class where TPk : struct =>
                new Dao<TEntity, TDto, TPk>(NullLogger<Dao<TEntity, TDto, TPk>>.Instance, context);

            public Database()
            {
                var options = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                NosCoreContext Context() => new(options);
                MapNpcs = Dao<MapNpc, MapNpcDto, int>(Context);
                Shops = Dao<Shop, ShopDto, int>(Context);
                ShopItems = Dao<ShopItem, ShopItemDto, int>(Context);
                ShopSkills = Dao<ShopSkill, ShopSkillDto, int>(Context);
                Teleporters = Dao<Teleporter, TeleporterDto, short>(Context);
                Recipes = Dao<Recipe, RecipeDto, short>(Context);
                RecipeItems = Dao<RecipeItem, RecipeItemDto, short>(Context);
            }

            public IDao<MapNpcDto, int> MapNpcs { get; }
            public IDao<ShopDto, int> Shops { get; }
            public IDao<ShopItemDto, int> ShopItems { get; }
            public IDao<ShopSkillDto, int> ShopSkills { get; }
            public IDao<TeleporterDto, short> Teleporters { get; }
            public IDao<RecipeDto, short> Recipes { get; }
            public IDao<RecipeItemDto, short> RecipeItems { get; }

            public async Task ImportAsync()
            {
                var log = new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object;
                var npcMonsters = new Mock<IDao<NpcMonsterDto, short>>();
                npcMonsters.Setup(s => s.LoadAll()).Returns([new NpcMonsterDto { NpcMonsterVNum = 1 }]);
                var npcTalks = new Mock<IDao<NpcTalkDto, short>>();
                npcTalks.Setup(s => s.LoadAll()).Returns([]);
                var items = new Mock<IDao<ItemDto, short>>();
                items.Setup(s => s.LoadAll()).Returns(new short[] { 1002, 1003, 2029 }.Select(v => new ItemDto { VNum = v }).ToList());
                var packets = Capture();
                CaptureSessions.MergeRepeatedEntities(packets);

                await new MapNpcParser(MapNpcs, npcMonsters.Object, npcTalks.Object, Shops, ShopItems, ShopSkills, Teleporters, Recipes,
                    RecipeItems, new Mock<ILogger<MapNpcParser>>().Object, log).InsertMapNpcsAsync(packets);
                await new ShopParser(Shops, ShopItems, ShopSkills, MapNpcs, new Mock<ILogger<ShopParser>>().Object, log).InsertShopsAsync(packets);
                await new TeleporterParser(Teleporters, MapNpcs, new Mock<ILogger<TeleporterParser>>().Object, log).InsertTeleportersAsync(packets);
                await new ShopItemParser(ShopItems, Shops, new Mock<ILogger<ShopItemParser>>().Object, log).InsertShopItemsAsync(packets);
                await new ShopSkillParser(ShopSkills, Shops, new Mock<ILogger<ShopSkillParser>>().Object, log).InsertShopSkillsAsync(packets);
                await new RecipeParser(Recipes, RecipeItems, MapNpcs, items.Object, new Mock<ILogger<RecipeParser>>().Object, log).InsertRecipesAsync(packets);
            }

            public string Contents()
            {
                var shops = Shops.LoadAll().ToDictionary(s => s.ShopId, s => s.MapNpcId);
                var recipes = Recipes.LoadAll().ToDictionary(s => s.RecipeId);
                return string.Join("\n", new[]
                {
                    MapNpcs.LoadAll().Select(s => $"npc {s.MapNpcId} {s.MapId} {s.VNum} {s.MapX} {s.MapY} {s.Direction} {s.IsSitting}"),
                    Shops.LoadAll().Select(s => $"shop {s.MapNpcId} {s.MenuType} {s.ShopType}"),
                    ShopItems.LoadAll().Select(s => $"item {shops[s.ShopId]} {s.Type} {s.Slot} {s.ItemVNum} {s.Rare} {s.Upgrade}"),
                    ShopSkills.LoadAll().Select(s => $"skill {shops[s.ShopId]} {s.Type} {s.Slot} {s.SkillVNum}"),
                    Teleporters.LoadAll().Select(s => $"teleporter {s.MapNpcId} {s.Runner} {s.Index} {s.MapId} {s.MapX} {s.MapY} {s.Type}"),
                    recipes.Values.Select(s => $"recipe {s.MapNpcId} {s.ItemVNum} {s.Amount}"),
                    RecipeItems.LoadAll().Select(s => $"material {recipes[s.RecipeId].MapNpcId} {recipes[s.RecipeId].ItemVNum} {s.ItemVNum} {s.Amount}"),
                }.SelectMany(s => s).Order());
            }
        }

        private static async Task<Database> AnEarlierImportAsync()
        {
            var database = new Database();
            await database.MapNpcs.TryInsertOrUpdateAsync(new[]
            {
                new MapNpcDto { MapNpcId = 5, MapId = 1, VNum = 1, MapX = 10, MapY = 10, Direction = 0 },
                new MapNpcDto { MapNpcId = 100, MapId = 1, VNum = 1, MapX = 10, MapY = 10, Direction = 2 },
            });
            await database.Shops.TryInsertOrUpdateAsync(new[]
            {
                new ShopDto { ShopId = 1, MapNpcId = 5, MenuType = 0, ShopType = 9 },
                new ShopDto { ShopId = 2, MapNpcId = 100, MenuType = 0, ShopType = 3 },
            });
            await database.ShopItems.TryInsertOrUpdateAsync(new[]
            {
                new ShopItemDto { ShopId = 1, Type = 1, Slot = 3, ItemVNum = 1002 },
                new ShopItemDto { ShopId = 1, Type = 1, Slot = 0, ItemVNum = 2029 },
                new ShopItemDto { ShopId = 2, Type = 1, Slot = 0, ItemVNum = 1003 },
            });
            await database.ShopSkills.TryInsertOrUpdateAsync(new[] { new ShopSkillDto { ShopId = 2, Type = 0, Slot = 0, SkillVNum = 200 } });
            await database.Teleporters.TryInsertOrUpdateAsync(new[]
            {
                new TeleporterDto { MapNpcId = 5, Index = 1, MapId = 20, MapX = 1, MapY = 1 },
                new TeleporterDto { MapNpcId = 100, Runner = NrunRunnerType.Teleport, Index = 1, MapId = 20, MapX = 8, MapY = 87 },
            });
            await database.Recipes.TryInsertOrUpdateAsync(new[]
            {
                new RecipeDto { RecipeId = 1, MapNpcId = 100, ItemVNum = 1002, Amount = 1 },
                new RecipeDto { RecipeId = 2, MapNpcId = 5, ItemVNum = 1002, Amount = 3 },
            });
            await database.RecipeItems.TryInsertOrUpdateAsync(new[]
            {
                new RecipeItemDto { RecipeId = 1, ItemVNum = 2029, Amount = 5 },
                new RecipeItemDto { RecipeId = 2, ItemVNum = 1003, Amount = 1 },
            });
            return database;
        }

        [TestMethod]
        public async Task RerunningOnAnEarlierImportGivesWhatAFreshImportGives()
        {
            var fresh = new Database();
            await fresh.ImportAsync();
            var rerun = await AnEarlierImportAsync();

            await rerun.ImportAsync();

            StringAssert.Contains(fresh.Contents(), "material 5 1002 2029 5");
            StringAssert.Contains(fresh.Contents(), "teleporter 5 Teleport 1 20 8 87");
            Assert.AreEqual(fresh.Contents(), rerun.Contents());
        }

        [TestMethod]
        public async Task TheCopyIsRemovedWithEverythingHangingOffIt()
        {
            var rerun = await AnEarlierImportAsync();

            await rerun.ImportAsync();

            Assert.IsFalse(rerun.MapNpcs.LoadAll().Any(s => s.MapNpcId == 100));
            Assert.IsFalse(rerun.Shops.LoadAll().Any(s => s.MapNpcId == 100));
            Assert.IsFalse(rerun.Teleporters.LoadAll().Any(s => s.MapNpcId == 100));
            Assert.IsFalse(rerun.Recipes.LoadAll().Any(s => s.MapNpcId == 100));
            Assert.AreEqual(0, rerun.ShopSkills.LoadAll().Count());
        }

        [TestMethod]
        public async Task ASecondRunChangesNothing()
        {
            var database = new Database();
            await database.ImportAsync();
            var first = database.Contents();
            var ids = database.ShopItems.LoadAll().Select(s => s.ShopItemId).Order().ToList();

            await database.ImportAsync();

            Assert.AreEqual(first, database.Contents());
            CollectionAssert.AreEqual(ids, database.ShopItems.LoadAll().Select(s => s.ShopItemId).Order().ToList());
        }
    }
}
