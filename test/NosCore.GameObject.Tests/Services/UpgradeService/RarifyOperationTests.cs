//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.UpgradeService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.UpgradeService
{
    [TestClass]
    public class RarifyOperationTests
    {
        private TestWorld World;
        private const short ArmorVNum = 1;
        private const short CellonVNum = 1014;
        private const short ScrollVNum = 1218;
        private const short RarityAmuletVNum = 259;

        private ClientSession _session;
        private Mock<IRandomProvider> _random;
        private RarifyOperation _unprotected;
        private RarifyProtectedOperation _protected;
        private InventoryItemInstance _wearable;
        private IReadOnlyList<IPacket> _result;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _random = World.Freeze<Mock<IRandomProvider>>();
            World.Inject(UpgradeMaterials.Catalog());
            _unprotected = World.Create<RarifyOperation>();
            _protected = World.Create<RarifyProtectedOperation>();
        }

        [TestMethod]
        public async Task LowRollLandsOnHighestRareAndIsCountedAsSuccess()
        {
            await new Spec("Roll inside the rare7 band rarifies up to 7")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(CellonRemainingShouldBe_, (short)95)
                .And(GoldShouldBe_, 99_500L)
                .And(TheChatLineShouldCarryTheRarity_, 7L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RollOutsideAllBandsDestroysTheItem()
        {
            await new Spec("Roll that matches no rare band destroys the wearable (unprotected)")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollAbsorbsNoBandMatchAndKeepsOriginalRare()
        {
            await new Spec("Protected scroll absorbs a no-band-match roll; item kept at original rare")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.50)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)5)
                .And(WearableShouldNotBeFixed)
                .And(ScrollShouldHaveBeenConsumed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedScrollSkipsBandsThatWouldDowngrade()
        {
            await new Spec("Protected scroll allows improvement bands (rare7 with originalRare=5)")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SuccessRerollsTheRarityBonusOfTheArmour()
        {
            await new Spec("Successful rarify re-rolls the rarity bonus of the armour")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(SourceWearableHasPreExistingDefenseStats)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(TheArmourShouldSpend_RarityPointsOnDefenceAndDodge, 10)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RarityAtMaxIsRejectedSilently()
        {
            await new Spec("Rarify at +8 cap is rejected with no charges")
                .Given(WearableAtRarity_, (sbyte)8)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NegativeRarityIsRejectedSilently()
        {
            await new Spec("Cursed (negative-rarity) item cannot be rarified")
                .Given(WearableAtRarity_, (sbyte)-1)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ProtectedWithoutScrollIsRejected()
        {
            await new Spec("Protected rarify with no scroll in inventory rejects with InfoiPacket")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(CellonInInventoryNoScroll)
                .And(CharacterHasGold_, 100_000L)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(SingleRejectionPacketShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARingCannotBeRarified()
        {
            await new Spec("A ring sent to the rarity window is left alone and nothing is charged")
                .Given(RingAtRarity_, (sbyte)0)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(NoPacketsShouldHaveBeenReturned)
                .And(WearableRarityShouldBe_, (sbyte)0)
                .And(CellonRemainingShouldBe_, (short)100)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABlessingAmuletWidensEveryBandAndSpendsAUse()
        {
            await new Spec("A roll just past the rare 1 band still lands on rare 1 with a Blessing Amulet worn")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.RedAmulet, 3)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.52)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)1)
                .And(TheAmuletShouldHave_UsesLeft, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AProtectionAmuletKeepsTheItemOnAFailedGamble()
        {
            await new Spec("A failed gamble with a Protection Amulet worn keeps the item at its rarity")
                .Given(WearableAtRarity_, (sbyte)5)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.BlueAmulet, 3)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.90)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)5)
                .And(TheAmuletShouldHave_UsesLeft, 2)
                .And(TheResultShouldSay_, Game18NConstString.ItemSurvivedWithProtection)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AProtectionAmuletDoesNotWidenTheBands()
        {
            await new Spec("A roll just past the rare 1 band fails with a Protection Amulet worn")
                .Given(WearableAtRarity_, (sbyte)3)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.BlueAmulet, 3)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.52)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnAmuletDisappearsWithItsLastUse()
        {
            await new Spec("The amulet is taken off and gone after its last gamble")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.RedAmulet, 1)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(NoAmuletShouldBeWorn)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AScrollGambleIsRefusedWhileAnAmuletIsWorn()
        {
            await new Spec("Gambling with a scroll while wearing a betting amulet is refused and charges nothing")
                .Given(WearableAtRarity_, (sbyte)0)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.RedAmulet, 3)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(TheRefusalShouldBe_, Game18NConstString.UnequipTheAmuletFirstThenYou)
                .And(WearableRarityShouldBe_, (sbyte)0)
                .And(GoldShouldBe_, 100_000L)
                .And(TheAmuletShouldHave_UsesLeft, 3)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ABettingAmuletRefusesChampionGear()
        {
            await new Spec("A champion item cannot be gambled while wearing a normal betting amulet")
                .Given(ChampionWearableAtRarity_, (sbyte)0)
                .And(AmuletWith_UsesIsWorn_, ItemEffectType.BlueAmulet, 3)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.02)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(TheRefusalShouldBe_, Game18NConstString.YouAreWearingAnIncorrectAmulet)
                .And(WearableRarityShouldBe_, (sbyte)0)
                .And(GoldShouldBe_, 100_000L)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACarriedRarityAmuletRaisesTheRarityByOneWithoutRolling()
        {
            await new Spec("A gamble with an Amulet for Increasing Rarity in the bag adds one rarity level and uses it up")
                .Given(WearableAtRarity_, (sbyte)3)
                .And(RarityAmuletIsCarried_, ItemEffectType.RarityIncreaseAmulet)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.90)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)4)
                .And(TheResultShouldSay_, Game18NConstString.RarityLevelIncreased)
                .And(NoRarityAmuletShouldBeCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ARarityAmuletIsNotUsedOnARareSevenItem()
        {
            await new Spec("An item already at rare 7 rolls normally and keeps the Amulet for Increasing Rarity")
                .Given(WearableAtRarity_, (sbyte)7)
                .And(RarityAmuletIsCarried_, ItemEffectType.RarityIncreaseAmulet)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.01)
                .WhenAsync(ProtectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)7)
                .And(TheRarityAmuletShouldStillBeCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AChampionRarityAmuletRaisesChampionGearToRareEight()
        {
            await new Spec("A champion item at rare 7 reaches rare 8 with a Champion Amulet for Increasing Rarity")
                .Given(ChampionWearableAtRarity_, (sbyte)7)
                .And(RarityAmuletIsCarried_, ItemEffectType.HeroicRarityIncreaseAmulet)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.90)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(WearableRarityShouldBe_, (sbyte)8)
                .And(NoRarityAmuletShouldBeCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AChampionRarityAmuletIsIgnoredOnOrdinaryGear()
        {
            await new Spec("An ordinary item gambles normally and keeps a Champion Amulet for Increasing Rarity")
                .Given(WearableAtRarity_, (sbyte)3)
                .And(RarityAmuletIsCarried_, ItemEffectType.HeroicRarityIncreaseAmulet)
                .And(MaterialsInInventory)
                .And(CharacterHasGold_, 100_000L)
                .And(NextRollWillBe_, 0.90)
                .WhenAsync(UnprotectedRarifyIsExecuted)
                .Then(SourceSlotShouldBeEmpty)
                .And(TheRarityAmuletShouldStillBeCarried)
                .ExecuteAsync();
        }

        // --- Givens ---

        private void WearableAtRarity_(sbyte rare) => PlaceWearable(rare, EquipmentType.Armor);

        private void ChampionWearableAtRarity_(sbyte rare)
        {
            PlaceWearable(rare, EquipmentType.Armor);
            _wearable.ItemInstance.Item.IsHeroic = true;
        }

        private void AmuletWith_UsesIsWorn_(ItemEffectType effect, int uses)
        {
            var item = new Item
            {
                VNum = 282,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Amulet,
                Effect = effect,
                EffectValue = 3,
            };
            var amulet = InventoryItemInstance.Create(
                new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object, World.LogLanguageLocalizer)
                {
                    DurabilityPoint = uses,
                },
                _session.Character.CharacterId);
            amulet.Slot = (short)EquipmentType.Amulet;
            amulet.Type = NoscorePocketType.Wear;
            _session.Character.InventoryService[amulet.ItemInstanceId] = amulet;
        }

        private void RingAtRarity_(sbyte rare) => PlaceWearable(rare, EquipmentType.Ring);

        private void RarityAmuletIsCarried_(ItemEffectType effect)
        {
            var item = new Item
            {
                VNum = RarityAmuletVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Amulet,
                Effect = effect,
            };
            var amulet = InventoryItemInstance.Create(
                new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object, World.LogLanguageLocalizer),
                _session.Character.CharacterId);
            amulet.Slot = 1;
            amulet.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[amulet.ItemInstanceId] = amulet;
        }

        private void PlaceWearable(sbyte rare, EquipmentType slot)
        {
            var item = new Item
            {
                VNum = ArmorVNum,
                Type = NoscorePocketType.Equipment,
                ItemType = ItemType.Armor,
                EquipmentSlot = slot,
            };
            var wearable = new WearableInstance(item, new Mock<ILogger<WearableInstance>>().Object,
                World.LogLanguageLocalizer)
            {
                Rare = rare,
            };
            _wearable = InventoryItemInstance.Create(wearable, _session.Character.CharacterId);
            _wearable.Slot = 0;
            _wearable.Type = NoscorePocketType.Equipment;
            _session.Character.InventoryService[_wearable.ItemInstanceId] = _wearable;
        }

        private void SourceWearableHasPreExistingDefenseStats()
        {
            var wearable = (WearableInstance)_wearable.ItemInstance;
            wearable.CloseDefence = 50;
            wearable.DistanceDefence = 50;
            wearable.MagicDefence = 50;
        }

        private void MaterialsInInventory()
        {
            AddStack(CellonVNum, 100, slot: 0);
            AddStack(ScrollVNum, 5, slot: 1);
        }

        private void CellonInInventoryNoScroll() => AddStack(CellonVNum, 100, slot: 0);

        private void AddStack(short vnum, short amount, short slot)
        {
            var inst = InventoryItemInstance.Create(
                new ItemInstanceForTest(vnum) { Amount = amount },
                _session.Character.CharacterId);
            inst.Slot = slot;
            inst.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[inst.ItemInstanceId] = inst;
        }

        private void CharacterHasGold_(long gold) => _session.Character.Gold = gold;

        private void NextRollWillBe_(double roll) => _random.Setup(r => r.NextDouble()).Returns(roll);

        // --- Whens ---

        private async Task UnprotectedRarifyIsExecuted() => _result = await _unprotected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.RarifyItem));

        private async Task ProtectedRarifyIsExecuted() => _result = await _protected.ExecuteAsync(
            _session, BuildPacket(UpgradePacketType.RarifyItemProtected));

        private static UpgradePacket BuildPacket(UpgradePacketType type) => new()
        {
            UpgradeType = type,
            InventoryType = PocketType.Equipment,
            Slot = 0,
        };

        // --- Thens ---

        private void WearableRarityShouldBe_(sbyte expected) =>
            Assert.AreEqual(expected, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment).NotNull().ItemInstance).Rare);

        private void WearableShouldNotBeFixed() =>
            Assert.AreNotEqual(true, ((WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment).NotNull().ItemInstance).IsFixed);

        private void SourceSlotShouldBeEmpty() =>
            Assert.IsNull(_session.Character.InventoryService.LoadBySlotAndType(0, NoscorePocketType.Equipment));

        private void TheArmourShouldSpend_RarityPointsOnDefenceAndDodge(int points)
        {
            var armour = (WearableInstance)_session.Character.InventoryService
                .LoadBySlotAndType(0, NoscorePocketType.Equipment).NotNull().ItemInstance;
            Assert.AreEqual(points, armour.CloseDefence + armour.DefenceDodge);
            Assert.AreEqual(armour.CloseDefence, armour.DistanceDefence);
            Assert.AreEqual(armour.CloseDefence, armour.MagicDefence);
            Assert.AreEqual(armour.DefenceDodge, armour.DistanceDefenceDodge);
        }

        private InventoryItemInstance? WornAmulet() =>
            _session.Character.InventoryService.LoadBySlotAndType(EquipmentType.Amulet, NoscorePocketType.Wear);

        private void TheAmuletShouldHave_UsesLeft(int uses) =>
            Assert.AreEqual(uses, WornAmulet().NotNull().ItemInstance.DurabilityPoint);

        private void NoAmuletShouldBeWorn() => Assert.IsNull(WornAmulet());

        private int CarriedRarityAmulets() => _session.Character.InventoryService.GetAll()
            .Count(i => i.ItemInstance.Item.VNum == RarityAmuletVNum);

        private void NoRarityAmuletShouldBeCarried() => Assert.AreEqual(0, CarriedRarityAmulets());

        private void TheRarityAmuletShouldStillBeCarried() => Assert.AreEqual(1, CarriedRarityAmulets());

        private void TheResultShouldSay_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<MsgiPacket>().Single().Message);

        private void TheChatLineShouldCarryTheRarity_(long rare)
        {
            var say = _result.OfType<NosCore.Packets.ServerPackets.Chats.SayiPacket>().Single();
            Assert.AreEqual(Game18NConstString.GambleSuccessful, say.Message);
            Assert.AreEqual(rare, say.Game18NArguments[0]);
        }

        private void TheRefusalShouldBe_(Game18NConstString message) =>
            Assert.AreEqual(message, _result.OfType<MsgiPacket>().Single().Message);

        private void CellonRemainingShouldBe_(short expected) =>
            Assert.AreEqual(expected, _session.Character.InventoryService.CountItem(CellonVNum));

        private void ScrollShouldHaveBeenConsumed() =>
            Assert.AreEqual(4, _session.Character.InventoryService.CountItem(ScrollVNum));

        private void GoldShouldBe_(long expected) => Assert.AreEqual(expected, _session.Character.Gold);

        private void NoPacketsShouldHaveBeenReturned() => Assert.AreEqual(0, _result?.Count ?? 0);

        private void SingleRejectionPacketShouldBeReturned() => Assert.AreEqual(1, _result?.Count ?? 0);

        private sealed class ItemInstanceForTest(short vnum) : NosCore.Data.Dto.ItemInstanceDto, IItemInstance
        {
            public new Guid Id { get; set; } = Guid.NewGuid();
            public new short ItemVNum { get; set; } = vnum;
            public Item Item { get; set; } = new() { VNum = vnum, Type = NoscorePocketType.Main };
            public object Clone() => MemberwiseClone();
        }
    }
}
