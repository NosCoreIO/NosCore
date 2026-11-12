//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.UseItem
{
    [TestClass]
    public class TarotCardGameHandlerTests
    {
        private const short GameVNum = 1904;
        private static readonly short[] Deck = [1894];

        private TestWorld World;
        private ClientSession _session;
        private TarotCardGameHandler _handler;
        private InventoryItemInstance _game;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.ItemList.Add(new Item { VNum = GameVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.TarotCardGame, EffectValue = 5 });
            World.ItemList.AddRange(Deck.Select(vnum => new Item { VNum = vnum, Type = NoscorePocketType.Main, Effect = ItemEffectType.SealedTarotCard }));
            World.Inject<IItemCatalog>(new ItemCatalog(World.ItemList));
            _session = await World.CreateSessionAsync();
            _handler = World.Create<TarotCardGameHandler>();
        }

        [TestMethod]
        public async Task AGameDealsFiveSealedCards()
        {
            await new Spec("A tarot card game deals five sealed cards and is used up")
                .Given(ATarotCardGame)
                .WhenAsync(OpeningIt)
                .Then(TheCharacterHolds_SealedCards, 5)
                .And(TheGameIsUsedUp)
                .And(EachDealtCardIsAnnounced)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFullInventoryKeepsTheGame()
        {
            await new Spec("With no room for the cards the game stays in the inventory")
                .Given(ATarotCardGame)
                .And(AFullMainPocket)
                .WhenAsync(OpeningIt)
                .Then(TheCharacterHolds_SealedCards, 0)
                .And(TheGameIsStillThere)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherEffectIsIgnored()
        {
            await new Spec("Another item effect deals nothing")
                .Given(ATarotCardGame)
                .And(TheGameHasAnotherEffect)
                .WhenAsync(OpeningIt)
                .Then(TheCharacterHolds_SealedCards, 0)
                .And(TheGameIsStillThere)
                .ExecuteAsync();
        }

        private void ATarotCardGame()
        {
            _game = InventoryItemInstance.Create(World.GenerateItemProvider().Create(GameVNum, 1), _session.Character.CharacterId);
            _session.Character.InventoryService.AddItemToPocket(_game);
        }

        private void TheGameHasAnotherEffect() => _game.ItemInstance.Item.Effect = ItemEffectType.Teleport;

        private void AFullMainPocket()
        {
            var inventory = _session.Character.InventoryService;
            var provider = World.GenerateItemProvider();
            while (inventory.CountItemInAnPocket(NoscorePocketType.Main) < inventory.GetMaxSlots(NoscorePocketType.Main))
            {
                inventory.AddItemToPocket(InventoryItemInstance.Create(provider.Create(1013, 999), _session.Character.CharacterId));
            }
        }

        private Task OpeningIt() => _handler.Handle(new ItemUsedEvent(_session, _game, new UseItemPacket
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = PocketType.Main,
            Slot = _game.Slot,
        }));

        private void TheCharacterHolds_SealedCards(int count) =>
            Assert.AreEqual(count, Deck.Sum(vnum => _session.Character.InventoryService.CountItem(vnum)));

        private void TheGameIsUsedUp() => Assert.AreEqual(0, _session.Character.InventoryService.CountItem(GameVNum));

        private void TheGameIsStillThere() => Assert.AreEqual(1, _session.Character.InventoryService.CountItem(GameVNum));

        private void EachDealtCardIsAnnounced() =>
            Assert.AreEqual(5, _session.LastPackets.OfType<SayiPacket>()
                .Where(p => p.Message == Game18NConstString.ReceivedThisItem)
                .Sum(p => (short)p.Game18NArguments[1]));
    }
}
