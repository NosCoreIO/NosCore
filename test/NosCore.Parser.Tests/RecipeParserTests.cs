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
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class RecipeParserTests
    {
        private Mock<IDao<RecipeDto, short>> _recipeDao = null!;
        private Mock<IDao<RecipeItemDto, short>> _recipeItemDao = null!;
        private List<RecipeDto> _savedRecipes = null!;
        private List<RecipeItemDto> _savedItems = null!;
        private RecipeParser _parser = null!;

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
            itemDao.Setup(x => x.LoadAll()).Returns(new short[] { 1002, 1003, 2029, 2097, 2102, 2131 }
                .Select(v => new ItemDto { VNum = v }).ToList());

            _parser = new RecipeParser(_recipeDao.Object, _recipeItemDao.Object, mapNpcDao.Object, itemDao.Object,
                new Mock<ILogger<RecipeParser>>().Object, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        private static List<string[]> Packets(params string[] lines) => lines.Select(l => l.Split(' ')).ToList();

        [TestMethod]
        public async Task AListedRecipeTakesTheMaterialsShownForIt()
        {
            await _parser.InsertRecipesAsync(Packets(
                "n_run 14 17 2 1897",
                "m_list 2 1002 1003 -100",
                "pdtse 1 1002",
                "ncif 2 1897",
                "m_list 3 10 2029 10 2097 10 2102 30 -1"));

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
                "n_run 14 17 2 1897",
                "m_list 2 1002 1003 -100",
                "pdtse 1 1002"));

            Assert.AreEqual(0, _savedRecipes.Count);
        }

        [TestMethod]
        public async Task MaterialsAreOnlyKeptForTheNpcThatListsTheItem()
        {
            await _parser.InsertRecipesAsync(Packets(
                "n_run 14 17 2 1897",
                "m_list 2 1002 -100",
                "n_run 14 18 2 1895",
                "m_list 2 2131 -100",
                "pdtse 1 1002",
                "m_list 3 10 2029 10 -1"));

            Assert.AreEqual(0, _savedRecipes.Count);
        }

        [TestMethod]
        public async Task AnAnswerIsNotReusedForALaterQuestion()
        {
            await _parser.InsertRecipesAsync(Packets(
                "n_run 14 17 2 1897",
                "m_list 2 1002 1003 -100",
                "pdtse 1 1002",
                "m_list 3 10 2029 10 -1",
                "pdtse 2 1003",
                "m_list 3 1 2097 5 -1"));

            Assert.AreEqual(1002, _savedRecipes.Single().ItemVNum);
        }
    }
}
