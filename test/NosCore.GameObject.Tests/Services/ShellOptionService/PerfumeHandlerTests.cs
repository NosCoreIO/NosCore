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
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using ServerGuriPacket = NosCore.Packets.ServerPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Services.ShellOptionService
{
    [TestClass]
    public class PerfumeHandlerTests
    {
        private const short PerfumeVNum = 1428;
        private const long OtherPlayer = 999_999;

        private TestWorld World;
        private ClientSession _session;
        private InventoryItemInstance _equipment;
        private InventoryItemInstance _perfume;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public async Task UsingPerfumeOpensItsWindow()
        {
            await new Spec("Using a perfume opens the perfume window")
                .Given(_PerfumesInTheInventory, (short)1)
                .WhenAsync(UsingThePerfume)
                .Then(ThePerfumeWindowShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PerfumeTakesOverAnItemBoundToAnotherPlayer()
        {
            await new Spec("A level 70 rarity 5 weapon costs 80 perfumes and 15 000 gold to take over")
                .Given(AWeaponOfLevel_AndRarity_BoundTo_, (byte)70, (short)5, OtherPlayer)
                .And(_PerfumesInTheInventory, (short)100)
                .And(_Gold, 20_000L)
                .WhenAsync(PerfumingTheWeapon)
                .Then(TheWeaponShouldBeBoundToTheCharacter)
                .And(PerfumesLeftShouldBe_, 20)
                .And(GoldLeftShouldBe_, 5_000L)
                .And(Message_ShouldBeSent, Game18NConstString.EquipmentPerfumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PerfumeNeedsEnoughBottles()
        {
            await new Spec("Without enough perfume the weapon stays bound to the other player")
                .Given(AWeaponOfLevel_AndRarity_BoundTo_, (byte)70, (short)5, OtherPlayer)
                .And(_PerfumesInTheInventory, (short)79)
                .And(_Gold, 20_000L)
                .WhenAsync(PerfumingTheWeapon)
                .Then(TheWeaponShouldStillBeBoundTo_, OtherPlayer)
                .And(PerfumesLeftShouldBe_, 79)
                .And(Message_ShouldBeSent, Game18NConstString.NotEnoughPerfume)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PerfumeNeedsEnoughGold()
        {
            await new Spec("Without enough gold the weapon stays bound to the other player")
                .Given(AWeaponOfLevel_AndRarity_BoundTo_, (byte)70, (short)5, OtherPlayer)
                .And(_PerfumesInTheInventory, (short)100)
                .And(_Gold, 14_999L)
                .WhenAsync(PerfumingTheWeapon)
                .Then(TheWeaponShouldStillBeBoundTo_, OtherPlayer)
                .And(PerfumesLeftShouldBe_, 100)
                .And(GoldLeftShouldBe_, 14_999L)
                .And(Message_ShouldBeSent, Game18NConstString.NotEnoughGold)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheCharactersOwnItemIsLeftAlone()
        {
            await new Spec("An item already bound to the character costs nothing and changes nothing")
                .Given(AWeaponOfLevel_AndRarity_BoundTo_, (byte)70, (short)5, 0L)
                .And(TheWeaponIsTheCharacters)
                .And(_PerfumesInTheInventory, (short)100)
                .And(_Gold, 20_000L)
                .WhenAsync(PerfumingTheWeapon)
                .Then(PerfumesLeftShouldBe_, 100)
                .And(GoldLeftShouldBe_, 20_000L)
                .ExecuteAsync();
        }

        private void AWeaponOfLevel_AndRarity_BoundTo_(byte level, short rare, long owner)
        {
            _equipment = ShellTestBench.Equipment(_session, 0, ItemType.Weapon, rare, level);
            _equipment.ItemInstance.BoundCharacterId = owner;
        }

        private void TheWeaponIsTheCharacters() => _equipment.ItemInstance.BoundCharacterId = _session.Character.CharacterId;

        private void _PerfumesInTheInventory(short amount) =>
            _perfume = ShellTestBench.Consumable(_session, 0, PerfumeVNum, ItemEffectType.Perfume, amount);

        private void _Gold(long gold) => _session.Character.Gold = gold;

        private Task UsingThePerfume() =>
            World.Create<PerfumeWindowHandler>().Handle(new ItemUsedEvent(_session, _perfume, new UseItemPacket
            {
                VisualType = VisualType.Player,
                VisualId = _session.Character.CharacterId,
                Type = PocketType.Main,
                Slot = _perfume.Slot,
            }));

        private Task PerfumingTheWeapon() =>
            new PerfumeHandler(new Algorithm.ShellService.ShellService(),
                    new ItemCatalog([new Item { VNum = PerfumeVNum, Type = NoscorePocketType.Main, Effect = ItemEffectType.Perfume }]))
                .Handle(new GuriPacketReceivedEvent(_session, new NosCore.Packets.ClientPackets.UI.GuriPacket
                {
                    Type = GuriPacketType.Perfume,
                    Argument = (int)PocketType.Equipment,
                    VisualId = _equipment.Slot
                }));

        private void ThePerfumeWindowShouldOpen() =>
            Assert.IsTrue(_session.LastPackets.OfType<ServerGuriPacket>()
                .Any(g => g.Type == GuriPacketType.PerfumAndIdentification && g.Argument == 1));

        private void TheWeaponShouldBeBoundToTheCharacter() =>
            Assert.AreEqual(_session.Character.CharacterId, _equipment.ItemInstance.BoundCharacterId);

        private void TheWeaponShouldStillBeBoundTo_(long owner) =>
            Assert.AreEqual(owner, _equipment.ItemInstance.BoundCharacterId);

        private void PerfumesLeftShouldBe_(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(PerfumeVNum));

        private void GoldLeftShouldBe_(long gold) => Assert.AreEqual(gold, _session.Character.Gold);

        private void Message_ShouldBeSent(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));
    }
}
