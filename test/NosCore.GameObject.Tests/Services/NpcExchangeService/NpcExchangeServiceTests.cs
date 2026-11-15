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
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.GameObject.Services.BattleService;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Tests.Services.NpcExchangeService
{
    [TestClass]
    public class NpcExchangeServiceTests
    {
        private const short LeftHalf = 5917;
        private const short RightHalf = 5918;
        private const short Seal = 5922;
        private const short Claw = 2522;
        private const short SwordsmanCard = 4500;
        private const short ArcherCard = 4501;
        private const short HalloweenBlessing = 375;

        private TestWorld World;
        private ClientSession _session;
        private IItemGenerationService _itemProvider;
        private GameObject.Services.NpcExchangeService.NpcExchangeService _service;
        private Mock<IBuffService> _buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.WorldConfiguration.Value.BackpackSize = 48;
            var items = new List<ItemDto>
            {
                new Item { VNum = LeftHalf, Type = NoscorePocketType.Main },
                new Item { VNum = RightHalf, Type = NoscorePocketType.Main },
                new Item { VNum = Seal, Type = NoscorePocketType.Main },
                new Item { VNum = Claw, Type = NoscorePocketType.Etc },
                new Item { VNum = SwordsmanCard, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, Class = 2 },
                new Item { VNum = ArcherCard, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist, Class = 4 },
            };
            World.Inject(items);
            World.Inject<IItemCatalog>(World.Create<ItemCatalog>());
            _itemProvider = World.GenerateItemProvider();
            World.Inject(_itemProvider);
            World.Inject(new List<NpcExchangeDto>
            {
                new() { NpcExchangeId = 1, Runner = NrunRunnerType.ExchangeForGrenigasRaidSeal, ItemVNum = Seal, Amount = 1 },
                new() { NpcExchangeId = 2, Runner = NrunRunnerType.ExchangeForSP5, ItemVNum = SwordsmanCard, Amount = 1 },
                new() { NpcExchangeId = 3, Runner = NrunRunnerType.ExchangeForSP5, ItemVNum = ArcherCard, Amount = 1 },
                new() { NpcExchangeId = 4, Runner = NrunRunnerType.ExchangeForSP6, ItemVNum = Seal, Amount = 1 },
                new() { NpcExchangeId = 5, Runner = NrunRunnerType.ExchangeForJackOLanternSealPlusHalloweenBlessing, ItemVNum = Seal, Amount = 1, CardId = HalloweenBlessing },
            });
            World.Inject(new List<NpcExchangeMaterialDto>
            {
                new() { NpcExchangeMaterialId = 1, NpcExchangeId = 1, ItemVNum = LeftHalf, Amount = 1 },
                new() { NpcExchangeMaterialId = 2, NpcExchangeId = 1, ItemVNum = RightHalf, Amount = 1 },
                new() { NpcExchangeMaterialId = 3, NpcExchangeId = 2, ItemVNum = Claw, Amount = 50 },
                new() { NpcExchangeMaterialId = 4, NpcExchangeId = 3, ItemVNum = Claw, Amount = 50 },
                new() { NpcExchangeMaterialId = 5, NpcExchangeId = 5, ItemVNum = LeftHalf, Amount = 1 },
            });
            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(s => s.GetCard(HalloweenBlessing)).Returns(new CardDto { CardId = HalloweenBlessing });
            cards.Setup(s => s.GetCardBCards(HalloweenBlessing)).Returns(new List<BCardDto>());
            _buffs = World.Freeze<Mock<IBuffService>>();
            _service = World.Create<GameObject.Services.NpcExchangeService.NpcExchangeService>();
        }

        [TestMethod]
        public async Task TheMaterialsAreTradedForTheItem()
        {
            await new Spec("Both halves of the seal are taken and the whole seal given")
                .Given(TheCharacterHas__, LeftHalf, (short)1)
                .And(TheCharacterHas__, RightHalf, (short)1)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForGrenigasRaidSeal)
                .Then(TheCharacterShouldHave__, LeftHalf, 0)
                .And(TheCharacterShouldHave__, RightHalf, 0)
                .And(TheCharacterShouldHave__, Seal, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMissingMaterialRefusesTheExchange()
        {
            await new Spec("Without the right half nothing is taken and the player is told materials are missing")
                .Given(TheCharacterHas__, LeftHalf, (short)1)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForGrenigasRaidSeal)
                .Then(TheCharacterShouldHave__, LeftHalf, 1)
                .And(TheCharacterShouldHave__, Seal, 0)
                .And(RawMaterialsMissingShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullPocketKeepsTheMaterials()
        {
            await new Spec("With no room for the seal both halves are kept")
                .Given(TheBackpackHas_Slots, 2)
                .And(TheCharacterHas__, LeftHalf, (short)1)
                .And(TheCharacterHas__, RightHalf, (short)1)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForGrenigasRaidSeal)
                .Then(TheCharacterShouldHave__, LeftHalf, 1)
                .And(TheCharacterShouldHave__, RightHalf, 1)
                .And(TheCharacterShouldHave__, Seal, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSpecialistCardIsTheOneOfThePlayersClass()
        {
            await new Spec("An archer trading claws receives the archer's card")
                .Given(TheCharacterIsA_, CharacterClassType.Archer)
                .And(TheCharacterHas__, Claw, (short)50)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForSP5)
                .Then(TheCharacterShouldHave__, ArcherCard, 1)
                .And(TheCharacterShouldHave__, SwordsmanCard, 0)
                .And(TheCharacterShouldHave__, Claw, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExchangeWithoutItsMaterialsGivesNothing()
        {
            await new Spec("An exchange whose materials were not imported is never free")
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForSP6)
                .Then(TheCharacterShouldHave__, Seal, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AClassWithoutACardTradesNothing()
        {
            await new Spec("A martial artist has no card to receive, so the claws are kept")
                .Given(TheCharacterIsA_, CharacterClassType.MartialArtist)
                .And(TheCharacterHas__, Claw, (short)50)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForSP5)
                .Then(TheCharacterShouldHave__, Claw, 50)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnExchangeWithABuffCastsItOnThePlayer()
        {
            await new Spec("Trading the bag of sweets also gives the Halloween Blessing")
                .Given(TheCharacterHas__, LeftHalf, (short)1)
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForJackOLanternSealPlusHalloweenBlessing)
                .Then(TheCharacterShouldHave__, Seal, 1)
                .And(TheHalloweenBlessingShouldBeCast)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARefusedExchangeCastsNoBuff()
        {
            await new Spec("Without the bag of sweets there is no blessing")
                .WhenAsync(Exchanging_, NrunRunnerType.ExchangeForJackOLanternSealPlusHalloweenBlessing)
                .Then(NoBuffShouldBeCast)
                .ExecuteAsync();
        }

        private void TheHalloweenBlessingShouldBeCast()
        {
            var player = _session.Character.VisualId;
            _buffs.Verify(s => s.ApplyAsync(It.Is<IAliveEntity>(e => e.VisualId == player),
            It.Is<CardDto>(c => c.CardId == HalloweenBlessing), It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), -1), Times.Once);
        }

        private void NoBuffShouldBeCast() => _buffs.Verify(s => s.ApplyAsync(It.IsAny<IAliveEntity>(),
            It.IsAny<CardDto>(), It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);

        private void TheBackpackHas_Slots(int slots) => World.WorldConfiguration.Value.BackpackSize = (byte)slots;

        private void TheCharacterIsA_(CharacterClassType characterClass) => _session.Character.Class = characterClass;

        private void TheCharacterHas__(short vnum, short amount) =>
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(_itemProvider.Create(vnum, amount), _session.Character.CharacterId));

        private Task Exchanging_(NrunRunnerType runner) => _service.ExchangeAsync(_session, runner);

        private void TheCharacterShouldHave__(short vnum, int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(vnum));

        private void RawMaterialsMissingShouldBeSent() =>
            Assert.AreEqual(Game18NConstString.RawMaterialsMissing, _session.LastPackets.OfType<MsgiPacket>().First().Message);
    }
}
