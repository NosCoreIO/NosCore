//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.EquipmentService
{
    // These tests defend something that did not happen at all: that what is worn counts.
    // CombatComponent is meant to hold the equipment statistics and nobody ever wrote into it, so
    // weapon and armour were decoration and one always fought as if bare-handed.
    //
    // The catalogue is local rather than the tests' shared one, which has no combat stats: these
    // need pieces with real numbers.
    [TestClass]
    public class EquipmentStatsServiceTests
    {
        private const short MainWeaponVnum = 900;
        private const short SecondaryWeaponVnum = 901;
        private const short ArmourVnum = 902;
        private const short HatVnum = 903;
        private const short FairyVnum = 904;
        private const short EnchantedGlovesVnum = 905;

        private static readonly List<ItemDto> Catalog = new()
        {
            new Item
            {
                VNum = MainWeaponVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, DamageMinimum = 40, DamageMaximum = 60, HitRate = 12
            },
            new Item
            {
                VNum = SecondaryWeaponVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.SecondaryWeapon, DamageMinimum = 30, DamageMaximum = 50
            },
            new Item
            {
                VNum = ArmourVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Armor,
                EquipmentSlot = EquipmentType.Armor, CloseDefence = 20, DistanceDefence = 15, MagicDefence = 10
            },
            new Item
            {
                VNum = HatVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Hat, CloseDefence = 6, FireResistance = 12
            },
            new Item
            {
                VNum = FairyVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Fairy, ElementRate = 30
            },
            // A piece with no stats of its own but declaring an effect: it tells the
            // two paths apart, because if the test used a piece with both there would be no telling
            // which of the two produced the number.
            new Item
            {
                VNum = EnchantedGlovesVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Gloves
            },
        };

        // The effects are not in the item: NosCore.Data keeps the navigation collections
        // internal, so they arrive flat from the DAOs and the catalogue groups them. It is why
        // the service needs an ICardCatalog and the piece in its hand is not enough.
        private static readonly List<BCardDto> ItemEffects = new()
        {
            new BCardDto
            {
                ItemVNum = EnchantedGlovesVnum,
                Type = (byte)BCardType.CardType.Defence,
                SubType = (byte)AdditionalTypes.Defence.AllIncreased,
                FirstData = 25
            }
        };

        private EquipmentStatsService _service = null!;
        private ClientSession _session = null!;
        private IItemGenerationService _items = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _items = new ItemGenerationService(Catalog, NullLoggerFactory.Instance,
                TestHelpers.Instance.LogLanguageLocalizer);
            _service = new EquipmentStatsService(new CardCatalog(new List<CardDto>(), ItemEffects));
        }

        /// <summary>
        /// Creates a piece with the real generator and puts it in the given slot. Going through the
        /// generator rather than building the instance by hand is the only way to have an object that
        /// behaves as it does in production.
        /// </summary>
        private void Wear(EquipmentType slot, short vnum)
        {
            var item = _items.Create(vnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _session.Character.CharacterId),
                NoscorePocketType.Wear, (short)slot);
        }

        [TestMethod]
        public void NothingWornMeansNoBonus()
        {
            Assert.AreEqual(EquipmentStats.None, _service.Resolve(_session.Character));
        }

        [TestMethod]
        public void TheMainWeaponFeedsTheMeleeProfile()
        {
            Wear(EquipmentType.MainWeapon, MainWeaponVnum);

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(40, stats.MinHit);
            Assert.AreEqual(60, stats.MaxHit);
            Assert.AreEqual(12, stats.HitRate);
        }

        [TestMethod]
        public void TheMainWeaponDoesNotFeedTheRangedProfile()
        {
            // The three profiles do not mix: it is what lets an archer have different numbers
            // from a swordsman at the same level.
            Wear(EquipmentType.MainWeapon, MainWeaponVnum);

            Assert.AreEqual(0, _service.Resolve(_session.Character).MinDistance);
        }

        [TestMethod]
        public void TheSecondaryWeaponFeedsTheRangedProfile()
        {
            Wear(EquipmentType.SecondaryWeapon, SecondaryWeaponVnum);

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(30, stats.MinDistance);
            Assert.AreEqual(50, stats.MaxDistance);
            Assert.AreEqual(0, stats.MinHit, "And it does not touch melee");
        }

        [TestMethod]
        public void ArmourFeedsTheDefences()
        {
            Wear(EquipmentType.Armor, ArmourVnum);

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(20, stats.CloseDefence);
            Assert.AreEqual(15, stats.DistanceDefence);
            Assert.AreEqual(10, stats.MagicDefence);
        }

        [TestMethod]
        public void OtherPiecesAddDefencesAndResistances()
        {
            Wear(EquipmentType.Hat, HatVnum);

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(6, stats.CloseDefence);
            Assert.AreEqual(12, stats.FireResistance);
            Assert.AreEqual(0, stats.MinHit, "A hat deals no damage");
        }

        [TestMethod]
        public void PiecesStack()
        {
            Wear(EquipmentType.Armor, ArmourVnum);
            Wear(EquipmentType.Hat, HatVnum);

            Assert.AreEqual(26, _service.Resolve(_session.Character).CloseDefence);
        }

        [TestMethod]
        public void TheFairyBringsTheElementRate()
        {
            Wear(EquipmentType.Fairy, FairyVnum);

            Assert.AreEqual(30, _service.Resolve(_session.Character).ElementRate);
        }

        [TestMethod]
        public void AWeaponInTheBagCountsForNothing()
        {
            // It was the clearest report of them all: "I attack with the sword but it uses the crossbow,
            // which I am not even wearing". Only what is in the slots counts
            // dell'equipaggiamento.
            var item = _items.Create(MainWeaponVnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _session.Character.CharacterId),
                NoscorePocketType.Equipment);

            Assert.AreEqual(EquipmentStats.None, _service.Resolve(_session.Character));
        }
        // --- The effects declared by the pieces -----------------------------------------------
        //
        // A piece carries effects as well as numbers - "chance of poisoning", "defence increased
        // by 25%". The parser already read them correctly from the official files and nobody ever
        // looked at them again. The sibling codebase folds them in with GetStuffBuff.

        [TestMethod]
        public void AWornPieceCarriesItsDeclaredEffects()
        {
            Wear(EquipmentType.Gloves, EnchantedGlovesVnum);

            var effects = _service.Resolve(_session.Character).BCards;

            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual((byte)BCardType.CardType.Defence, effects[0].Type);
            Assert.AreEqual(25, effects[0].FirstData);
        }

        [TestMethod]
        public void NothingWornMeansNoEffects()
        {
            Assert.AreEqual(0, _service.Resolve(_session.Character).BCards.Count);
        }

        // The effects have to reach the combat stats, not just the list. Collecting them and
        // then dropping them is the failure this defends against: it raises nothing, the gloves
        // look right in the inventory, and the 25% defence they promise is never applied.
        [TestMethod]
        public void AWornEffectReachesTheCombatStats()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>()))
                .Returns(new List<BuffInstance>());
            var provider = new BattleStatsProvider(buffs.Object, _service);

            var bare = provider.GetStats(_session.Character).Defence;
            Wear(EquipmentType.Gloves, EnchantedGlovesVnum);
            var gloved = provider.GetStats(_session.Character).Defence;

            Assert.AreEqual(bare + 25, gloved);
        }

        // This defends the case where being wrong makes no noise: a piece with stats but
        // no effects must not invent any, and a piece with effects but no stats must
        // not vanish from the count. Without this check a badly built catalogue would pass
        // unnoticed, because it raises no exception.
        [TestMethod]
        public void EffectsAndFlatStatsAreSeparateRoads()
        {
            Wear(EquipmentType.Armor, ArmourVnum);
            Wear(EquipmentType.Gloves, EnchantedGlovesVnum);

            var gear = _service.Resolve(_session.Character);

            Assert.AreEqual(20, gear.CloseDefence, "l'armatura porta la sua difesa piatta");
            Assert.AreEqual(1, gear.BCards.Count, "i guanti portano il loro effetto, l'armatura no");
        }

    }
}
