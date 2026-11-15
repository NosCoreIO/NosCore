//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class RecipeParserTests
    {
        private Mock<IDao<RecipeDto, short>> _recipeDao;
        private Mock<IDao<RecipeItemDto, short>> _recipeItemDao;
        private List<RecipeDto> _savedRecipes;
        private List<RecipeItemDto> _savedItems;
        private RecipeParser _parser;

        [TestInitialize]
        public void Setup()
        {
            _savedRecipes = [];
            _savedItems = [];
            _recipeDao = new Mock<IDao<RecipeDto, short>>();
            _recipeDao.Setup(x => x.LoadAll()).Returns(new List<RecipeDto>());
            _recipeDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<RecipeDto>()))
                .ReturnsAsync((RecipeDto r) =>
                {
                    r.RecipeId = (short)(_savedRecipes.Count + 1);
                    _savedRecipes.Add(r);
                    return r;
                });
            _recipeItemDao = new Mock<IDao<RecipeItemDto, short>>();
            _recipeItemDao.Setup(x => x.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<RecipeItemDto>>()))
                .Callback<IEnumerable<RecipeItemDto>>(items => _savedItems.AddRange(items))
                .ReturnsAsync(true);

            var mapNpcDao = new Mock<IDao<MapNpcDto, int>>();
            mapNpcDao.Setup(x => x.LoadAll()).Returns(new List<MapNpcDto> { new() { MapNpcId = 1897 }, new() { MapNpcId = 1895 } });
            var itemDao = new Mock<IDao<ItemDto, short>>();
            itemDao.Setup(x => x.LoadAll()).Returns(new short[] { 1002, 1003, 2029, 2097, 2102, 2131, 1246 }
                .Select(v => new ItemDto { VNum = v })
                .Append(new ItemDto { VNum = 1241, ItemType = ItemType.Production, Effect = ItemEffectType.OpenProduction })
                .Append(new ItemDto { VNum = 2024, ItemType = ItemType.Potion })
                .ToList());

            _parser = new RecipeParser(_recipeDao.Object, _recipeItemDao.Object, mapNpcDao.Object, itemDao.Object,
                new Mock<ILogger<RecipeParser>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        private static List<IPacket> Packets(params IPacket[] packets) => [.. packets];

        [TestMethod]
        public async Task AListedRecipeTakesTheMaterialsShownForIt()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
                Captured.Makes(1002, 1003),
                Captured.ShowRecipe(1002),
                new NcifPacket { Type = VisualType.Npc, TargetId = 1897 },
                Captured.Materials(10, (2029, 10), (2097, 10), (2102, 30))));

            var recipe = _savedRecipes.Single();
            Assert.AreEqual(1897, recipe.MapNpcId);
            Assert.AreEqual(1002, recipe.ItemVNum);
            Assert.AreEqual(10, recipe.Amount);
            CollectionAssert.AreEqual(new short[] { 2029, 2097, 2102 }, _savedItems.Select(s => s.ItemVNum).ToList());
            CollectionAssert.AreEqual(new short[] { 10, 10, 30 }, _savedItems.Select(s => s.Amount).ToList());
            Assert.IsTrue(_savedItems.All(s => s.RecipeId == recipe.RecipeId));
        }

        [TestMethod]
        public async Task ARecipeWithoutMaterialsIsSkipped()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
                Captured.Makes(1002, 1003),
                Captured.ShowRecipe(1002)));

            Assert.AreEqual(0, _savedRecipes.Count);
        }

        [TestMethod]
        public async Task MaterialsAreOnlyKeptForTheNpcThatListsTheItem()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
                Captured.Makes(1002),
                Captured.Talk(NrunRunnerType.OpenProduction, 18, 1895),
                Captured.Makes(2131),
                Captured.ShowRecipe(1002),
                Captured.Materials(10, (2029, 10))));

            Assert.AreEqual(0, _savedRecipes.Count);
        }

        [TestMethod]
        public async Task AnAnswerIsNotReusedForALaterQuestion()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
                Captured.Makes(1002, 1003),
                Captured.ShowRecipe(1002),
                Captured.Materials(10, (2029, 10)),
                new PdtsePacket { Type = (PdtsePacketType)2, VNum = 1003 },
                Captured.Materials(1, (2097, 5))));

            Assert.AreEqual(1002, _savedRecipes.Single().ItemVNum);
        }

        [TestMethod]
        public async Task AScrollRecipeIsTakenFromTheScrollUsedInTheMainPocket()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.MainPocket((0, 2024), (5, 1241)),
                Captured.Use(PocketType.Main, 5),
                Captured.Makes(1246),
                Captured.ShowRecipe(1246),
                Captured.Materials(1, (1241, 1), (2029, 5), (2097, 5))));

            var recipe = _savedRecipes.Single();
            Assert.AreEqual((short)1241, recipe.ScrollVNum);
            Assert.IsNull(recipe.MapNpcId);
            Assert.AreEqual(1246, recipe.ItemVNum);
            CollectionAssert.AreEqual(new short[] { 1241, 2029, 2097 }, _savedItems.Select(s => s.ItemVNum).ToList());
        }

        [TestMethod]
        public async Task UsingAnItemThatIsNotAScrollListsNothing()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.MainPocket((0, 2024), (5, 1241)),
                Captured.Use(PocketType.Main, 0),
                Captured.Makes(1246),
                Captured.ShowRecipe(1246),
                Captured.Materials(1, (2029, 5))));

            Assert.AreEqual(0, _savedRecipes.Count);
        }

        [TestMethod]
        public async Task AScrollMovedIntoASlotIsFollowed()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.MainPocket((0, 2024)),
                Captured.MainPocketChange(7, 1241),
                Captured.Use(PocketType.Main, 7),
                Captured.Makes(1246),
                Captured.ShowRecipe(1246),
                Captured.Materials(1, (2029, 5))));

            Assert.AreEqual((short)1241, _savedRecipes.Single().ScrollVNum);
        }

        [TestMethod]
        public async Task AnNpcRecipeIsNotCreditedToAScrollUsedEarlier()
        {
            await _parser.InsertRecipesAsync(Packets(
                Captured.MainPocket((5, 1241)),
                Captured.Use(PocketType.Main, 5),
                Captured.Talk(NrunRunnerType.OpenProduction, 17, 1897),
                Captured.Makes(1002),
                Captured.ShowRecipe(1002),
                Captured.Materials(10, (2029, 10))));

            var recipe = _savedRecipes.Single();
            Assert.AreEqual(1897, recipe.MapNpcId);
            Assert.IsNull(recipe.ScrollVNum);
        }
    }
}
