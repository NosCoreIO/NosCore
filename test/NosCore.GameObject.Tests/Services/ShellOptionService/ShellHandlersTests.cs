//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Messaging.Handlers.UseItem;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
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
    public class ShellHandlersTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ShellTestBench _bench;
        private InventoryItemInstance _shell;
        private InventoryItemInstance _equipment;
        private InventoryItemInstance _consumable;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _bench = new ShellTestBench();
            _bench.Random.Setup(r => r.NextDouble()).Returns(0d);
        }

        [TestMethod]
        public async Task UsingARainbowPearlAsksForAShell()
        {
            await new Spec("Using a Rainbow Pearl puts the client in shell-picking mode")
                .Given(_PearlsInTheInventory, (short)20)
                .WhenAsync(UsingThePearl)
                .Then(TheShellPickerShouldOpen)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task IdentifyingAShellRollsItsOptionsAndSpendsPearls()
        {
            await new Spec("A level 60 rarity 5 shell costs 11 pearls and gets its options")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(_PearlsInTheInventory, (short)20)
                .WhenAsync(PickingTheShellToIdentify)
                .Then(TheShellShouldHave_Options, 3)
                .And(PearlsLeftShouldBe_, 9)
                .And(Message_ShouldBeSent, Game18NConstString.OptionsIdentified)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task IdentifyingNeedsEnoughPearls()
        {
            await new Spec("Without enough pearls the shell stays unidentified")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(_PearlsInTheInventory, (short)10)
                .WhenAsync(PickingTheShellToIdentify)
                .Then(TheShellShouldHave_Options, 0)
                .And(PearlsLeftShouldBe_, 10)
                .And(Message_ShouldBeSent, Game18NConstString.NotEnoughPearls)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShellIsIdentifiedOnlyOnce()
        {
            await new Spec("An identified shell is not rolled again")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(_PearlsInTheInventory, (short)20)
                .WhenAsync(PickingTheShellToIdentify)
                .Then(TheShellShouldHave_Options, 1)
                .And(PearlsLeftShouldBe_, 20)
                .And(Message_ShouldBeSent, Game18NConstString.OptionsAlreadyIdentified)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DroppingAShellAsksFirst()
        {
            await new Spec("Dropping an identified shell on a weapon asks before binding it")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .WhenAsync(DroppingTheShellWithMode_, (short)0)
                .Then(TheQuestion_ShouldBeAsked, Game18NConstString.AskAddOptions)
                .And(TheEquipmentShouldHave_Options, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ConfirmedShellMovesItsOptionsOntoTheWeapon()
        {
            await new Spec("Confirming moves the shell's options onto the weapon, binds it and uses the shell up")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(TheEquipmentShouldHave_Options, 1)
                .And(TheEquipmentShouldBeBoundWithShellRarity_, (byte)5)
                .And(TheShellShouldBeGone)
                .And(Message_ShouldBeSent, Game18NConstString.OptionHasBeenAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AShellCanGoOnTheWornWeapon()
        {
            await new Spec("A shell dropped on the worn weapon moves its options onto it")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .And(TheEquipmentIsWorn)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(TheEquipmentShouldHave_Options, 1)
                .And(Message_ShouldBeSent, Game18NConstString.OptionHasBeenAdded)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWeaponShellDoesNotFitArmour()
        {
            await new Spec("A weapon shell is refused by armour")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Armor, (short)5, (byte)70)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(Message_ShouldBeSent, Game18NConstString.IncorrectEquipmentAndOptionType)
                .And(TheEquipmentShouldHave_Options, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnUnidentifiedShellIsRefused()
        {
            await new Spec("A shell must be identified before it goes on equipment")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(Message_ShouldBeSent, Game18NConstString.CantInsertUnidentifiedShells)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWeaponMustBeAtLeastAsRareAsTheShell()
        {
            await new Spec("A rarity 5 shell is refused by a rarity 4 weapon")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)4, (byte)70)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(Message_ShouldBeSent, Game18NConstString.ItemRarityTooLow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheWeaponMustReachTheShellLevel()
        {
            await new Spec("A level 60 shell is refused by a level 50 weapon")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)50)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(Message_ShouldBeSent, Game18NConstString.NoItemInsertionLevelTooLow)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ReplacingOptionsCanBreakTheShell()
        {
            await new Spec("Over existing options a failed half-chance destroys the shell and keeps the old options")
                .Given(AWeaponShellOfRarity_, (short)5)
                .And(TheShellIsIdentified)
                .And(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .And(TheEquipmentHasTwoOptions)
                .And(EveryRollIs_, 0.9d)
                .WhenAsync(DroppingTheShellWithMode_, (short)1)
                .Then(TheEquipmentShouldHave_Options, 2)
                .And(TheShellShouldBeGone)
                .And(Message_ShouldBeSent, Game18NConstString.ShellBroken)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheMagicEraserRemovesEveryOption()
        {
            await new Spec("The Magic Eraser clears a weapon's options and is used up")
                .Given(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .And(TheEquipmentHasTwoOptions)
                .And(AMagicEraser)
                .WhenAsync(DroppingTheEraser)
                .Then(TheEquipmentShouldHave_Options, 0)
                .And(ErasersLeftShouldBe_, 0)
                .And(Message_ShouldBeSent, Game18NConstString.AllOptionsRemoved)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheMagicEraserNeedsOptions()
        {
            await new Spec("The Magic Eraser is kept when the weapon has no option")
                .Given(A_OfRarity_AndLevel_, ItemType.Weapon, (short)5, (byte)70)
                .And(AMagicEraser)
                .WhenAsync(DroppingTheEraser)
                .Then(ErasersLeftShouldBe_, 1)
                .And(Message_ShouldBeSent, Game18NConstString.NoOptionsOnItem)
                .ExecuteAsync();
        }

        private void AWeaponShellOfRarity_(short rare) =>
            _shell = ShellTestBench.Shell(_session, 2, 0, ShellType.Full, rare, 60);

        private void TheShellIsIdentified() => _bench.GiveOptions(_shell, (1, 1, 50));

        private void A_OfRarity_AndLevel_(ItemType type, short rare, byte level) =>
            _equipment = ShellTestBench.Equipment(_session, 0, type, rare, level);

        private void TheEquipmentIsWorn() => _equipment.Type = NoscorePocketType.Wear;

        private void TheEquipmentHasTwoOptions() => _bench.GiveOptions(_equipment, (1, 3, 2), (2, 9, 5));

        private void _PearlsInTheInventory(short amount) =>
            _consumable = ShellTestBench.Consumable(_session, 0, ShellTestBench.PearlVNum, ItemEffectType.ShellIdentification, amount);

        private void AMagicEraser() =>
            _consumable = ShellTestBench.Consumable(_session, 1, ShellTestBench.EraserVNum, ItemEffectType.ShellOptionRemoval, 1);

        private void EveryRollIs_(double roll) => _bench.Random.Setup(r => r.NextDouble()).Returns(roll);

        private Task UsingThePearl() =>
            World.Create<RainbowPearlHandler>().Handle(new ItemUsedEvent(_session, _consumable, Use(PocketType.Main, _consumable.Slot, 0)));

        private Task PickingTheShellToIdentify() =>
            new ShellIdentificationHandler(_bench.Service, new Algorithm.ShellService.ShellService())
                .Handle(new GuriPacketReceivedEvent(_session, new NosCore.Packets.ClientPackets.UI.GuriPacket
                {
                    Type = GuriPacketType.ShellIdentification,
                    Argument = 0,
                    VisualId = _shell.Slot
                }));

        private Task DroppingTheShellWithMode_(short mode) =>
            new ShellHandler(_bench.Service, _bench.Random.Object, new Mock<IBattleStatsProvider>().Object)
                .Handle(new ItemUsedEvent(_session, _shell, Use(PocketType.Equipment, _shell.Slot, mode, _equipment)));

        private Task DroppingTheEraser() =>
            new MagicEraserHandler(_bench.Service, new Mock<IBattleStatsProvider>().Object)
                .Handle(new ItemUsedEvent(_session, _consumable, Use(PocketType.Main, _consumable.Slot, 0, _equipment)));

        private UseItemPacket Use(PocketType type, short slot, short mode, InventoryItemInstance? target = null) => new()
        {
            VisualType = VisualType.Player,
            VisualId = _session.Character.CharacterId,
            Type = type,
            Slot = slot,
            Mode = mode,
            Parameter = 0,
            TargetType = target == null ? null : target.Type == NoscorePocketType.Wear ? PocketType.Wear : (PocketType)target.Type,
            TargetSlot = target?.Slot,
        };

        private void TheShellPickerShouldOpen() =>
            Assert.IsTrue(_session.LastPackets.OfType<ServerGuriPacket>()
                .Any(g => g.Type == GuriPacketType.PerfumAndIdentification && g.Argument == 0));

        private void TheShellShouldHave_Options(int count) => Assert.AreEqual(count, _bench.OptionsOf(_shell).Count);

        private void TheEquipmentShouldHave_Options(int count) => Assert.AreEqual(count, _bench.OptionsOf(_equipment).Count);

        private void PearlsLeftShouldBe_(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(ShellTestBench.PearlVNum));

        private void ErasersLeftShouldBe_(int count) =>
            Assert.AreEqual(count, _session.Character.InventoryService.CountItem(ShellTestBench.EraserVNum));

        private void TheEquipmentShouldBeBoundWithShellRarity_(byte rarity)
        {
            var equipment = (WearableInstance)_equipment.ItemInstance;
            Assert.AreEqual(_session.Character.CharacterId, equipment.BoundCharacterId);
            Assert.AreEqual(rarity, equipment.ShellRarity);
        }

        private void TheShellShouldBeGone() =>
            Assert.IsFalse(_session.Character.InventoryService.ContainsKey(_shell.ItemInstanceId));

        private void TheQuestion_ShouldBeAsked(Game18NConstString question) =>
            Assert.IsTrue(_session.LastPackets.OfType<QnaiPacket>().Any(q => q.Question == question));

        private void Message_ShouldBeSent(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(m => m.Message == message));
    }
}
