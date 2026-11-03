//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ProductionService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Production;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ProductionService
{
    [TestClass]
    public class ProductionServiceTests
    {
        private const int NpcId = 100;
        private const short Material = 1012;
        private const short Product = 1013;

        private ClientSession _session = null!;
        private IItemGenerationService _itemProvider = null!;
        private GameObject.Services.ProductionService.ProductionService _service = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _itemProvider = TestHelpers.Instance.GenerateItemProvider();
            _service = new GameObject.Services.ProductionService.ProductionService(_itemProvider, TestHelpers.Instance.DistanceCalculator);
        }

        [TestMethod]
        public async Task OpeningListsTheRecipesThenOpensTheWindow()
        {
            await new Spec("Opening production lists what the NPC makes, then opens the production window")
                .Given(AnNpcAt_CellsMakes_From_Materials, 0, (short)5)
                .WhenAsync(TheProductionWindowIsOpened)
                .Then(TheRecipeListShouldBeSentBeforeTheWindow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShowingARecipeSendsItsMaterials()
        {
            await new Spec("pdtse 1 answers with the amount made and the materials needed")
                .Given(AnNpcAt_CellsMakes_From_Materials, 0, (short)5)
                .WhenAsync(TheRecipeIsShown)
                .Then(TheMaterialsShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingConsumesTheMaterialsAndGivesTheItem()
        {
            await new Spec("Starting with every material, then filling the bar, takes them and gives the product")
                .Given(AnNpcAt_CellsMakes_From_Materials, 0, (short)5)
                .And(TheCharacterHas_Materials, (short)7)
                .WhenAsync(TheItemIsProduced)
                .Then(TheCharacterShouldHave_Materials, 2)
                .And(TheCharacterShouldHave_Products, 2)
                .And(TheProductionShouldBeConfirmed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingWithoutTheMaterialsIsRefused()
        {
            await new Spec("Starting without enough materials gives nothing and says why")
                .Given(AnNpcAt_CellsMakes_From_Materials, 0, (short)5)
                .And(TheCharacterHas_Materials, (short)4)
                .WhenAsync(TheItemIsProduced)
                .Then(TheCharacterShouldHave_Materials, 4)
                .And(TheCharacterShouldHave_Products, 0)
                .And(RawMaterialsMissingShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProducingAwayFromTheNpcDoesNothing()
        {
            await new Spec("Producing out of the NPC's reach is ignored")
                .Given(AnNpcAt_CellsMakes_From_Materials, 10, (short)5)
                .And(TheCharacterHas_Materials, (short)7)
                .WhenAsync(TheItemIsProduced)
                .Then(TheCharacterShouldHave_Materials, 7)
                .And(TheCharacterShouldHave_Products, 0)
                .ExecuteAsync();
        }

        private void AnNpcAt_CellsMakes_From_Materials(int distance, short materials)
        {
            var mapInstance = _session.Character.MapInstance;
            mapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new()
                    {
                        MapNpcId = NpcId,
                        MapId = mapInstance.Map.MapId,
                        MapX = (short)(_session.Character.PositionX + distance),
                        MapY = _session.Character.PositionY,
                        VNum = 1
                    }
                },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1 } });
            var npc = mapInstance.GetNpcById(NpcId)!.Value;
            npc.Recipes = new List<Recipe>
            {
                new()
                {
                    MapNpcId = NpcId,
                    ItemVNum = Product,
                    Amount = 2,
                    Items = new List<RecipeItemDto> { new() { ItemVNum = Material, Amount = materials } }
                },
                new() { MapNpcId = NpcId, ItemVNum = 1, Amount = 0 }
            };
        }

        private void TheCharacterHas_Materials(short amount) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(Material, amount), _session.Character.CharacterId));

        private Task TheProductionWindowIsOpened() =>
            _service.OpenAsync(_session, _session.Character.MapInstance.GetNpcById(NpcId)!.Value);

        private Task TheRecipeIsShown() => _service.ShowRecipeAsync(_session, Product);

        private Task TheItemIsProduced() => _service.ProduceAsync(_session, Product);

        private void TheRecipeListShouldBeSentBeforeTheWindow()
        {
            var list = _session.LastPackets.OfType<MlistPacket>().Single();
            CollectionAssert.AreEqual(new List<short> { Product }, list.ItemVNums);
            var wopen = _session.LastPackets.OfType<WopenPacket>().Single();
            Assert.AreEqual(WindowType.Production, wopen.Type);
            var packets = _session.LastPackets.ToList();
            Assert.IsTrue(packets.IndexOf(list) < packets.IndexOf(wopen));
        }

        private void TheMaterialsShouldBeSent()
        {
            var materials = _session.LastPackets.OfType<MlistMaterialsPacket>().Single();
            Assert.AreEqual(2, materials.Amount);
            Assert.AreEqual(Material, materials.Materials.Single().VNum);
            Assert.AreEqual(5, materials.Materials.Single().Amount);
        }

        private void TheCharacterShouldHave_Materials(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(Material));

        private void TheCharacterShouldHave_Products(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(Product));

        private void TheProductionShouldBeConfirmed()
        {
            var pdti = _session.LastPackets.OfType<PdtiPacket>().Single();
            Assert.AreEqual(PdtiPacketType.Produced, pdti.Type);
            Assert.AreEqual(Product, pdti.Vnum);
            Assert.AreEqual(2, pdti.RecipeAmount);
            var guri = _session.LastPackets.OfType<GuriPacket>().Single();
            Assert.AreEqual(GuriPacketType.AfterSumming, guri.Type);
            Assert.AreEqual(1324u, guri.Value);
        }

        private void RawMaterialsMissingShouldBeSent()
        {
            var msgi = _session.LastPackets.OfType<MsgiPacket>().First();
            Assert.AreEqual(Game18NConstString.RawMaterialsMissing, msgi.Message);
        }
    }
}
