//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using Moq;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Tests.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Algorithm.SpecialistPointService;
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
        private TestWorld World;
        private const short MainWeaponVnum = 900;
        private const short SecondaryWeaponVnum = 901;
        private const short ArmourVnum = 902;
        private const short HatVnum = 903;
        private const short FairyVnum = 904;
        private const short EnchantedGlovesVnum = 905;
        private const short SpecialistVnum = 906;
        private const short NecklaceVnum = 907;
        private const short WingSkinVnum = 908;

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
                EquipmentSlot = EquipmentType.Fairy, ElementRate = 30, Element = ElementType.Fire
            },
            // A piece with no stats of its own but declaring an effect: it tells the
            // two paths apart, because if the test used a piece with both there would be no telling
            // which of the two produced the number.
            new Item
            {
                VNum = EnchantedGlovesVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Gloves
            },
            new Item
            {
                VNum = SpecialistVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, FireResistance = 4, WaterResistance = 13
            },
            new Item
            {
                VNum = NecklaceVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Necklace, MaxCellon = 3, MaxCellonLvl = 10
            },
            new Item
            {
                VNum = WingSkinVnum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.WingSkin
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
                Type = BCardEffect.DefenceAllIncreased.Type(),
                SubType = BCardEffect.DefenceAllIncreased.SubType(),
                FirstData = 25
            },
            new BCardDto
            {
                ItemVNum = WingSkinVnum,
                Type = BCardEffect.DefenceAllIncreased.Type(),
                SubType = BCardEffect.DefenceAllIncreased.SubType(),
                FirstData = 10
            }
        };

        private EquipmentStatsService _service;
        private ClientSession _session;
        private IItemGenerationService _items;
        private List<EquipmentOptionDto> _options;
        private int _optionReads;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _items = new ItemGenerationService(Catalog, NullLoggerFactory.Instance,
                World.LogLanguageLocalizer);
            _options = new List<EquipmentOptionDto>();
            _optionReads = 0;
            var optionDao = new Mock<IDao<EquipmentOptionDto, Guid>>();
            optionDao.Setup(d => d.Where(It.IsAny<Expression<Func<EquipmentOptionDto, bool>>>()))
                .Returns((Expression<Func<EquipmentOptionDto, bool>> filter) =>
                {
                    _optionReads++;
                    return _options.Where(filter.Compile()).ToList();
                });
            _service = new EquipmentStatsService(new CardCatalog(new List<CardDto>(), ItemEffects, new List<ItemDto>()), optionDao.Object,
                new SpecialistCardService(new SpecialistPointService()));

        }

        /// <summary>
        /// Creates a piece with the real generator and puts it in the given slot. Going through the
        /// generator rather than building the instance by hand is the only way to have an object that
        /// behaves as it does in production.
        /// </summary>
        private IItemInstance Wear(EquipmentType slot, short vnum)
        {
            var item = _items.Create(vnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _session.Character.CharacterId),
                NoscorePocketType.Wear, (short)slot);
            return item;
        }

        private void AddOption(IItemInstance jewel, CellonType type, int value) =>
            _options.Add(new EquipmentOptionDto
            {
                Id = Guid.NewGuid(), WearableInstanceId = jewel.Id, Level = 1, Type = (byte)type, Value = value
            });

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

        // Elemental damage only flows when the attacker has an element, and a character's is its
        // fairy's: without it every element rate, fairy or specialist, was dead weight.
        [TestMethod]
        public void TheFairyGivesTheCharacterItsAttackElement()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>()))
                .Returns(new List<BuffInstance>());
            var provider = BattleStats.Provider(buffs.Object, _service);

            Assert.AreEqual(0, provider.GetStats(_session.Character).Element);
            Wear(EquipmentType.Fairy, FairyVnum);

            Assert.AreEqual((byte)ElementType.Fire, provider.GetStats(_session.Character).Element);
        }

        [TestMethod]
        public void AnEffectOnTheFairysElementWaitsForAFairy()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>
            {
                new(CardId: 4408, BuffType: BuffType.Good, Caster: null,
                    StartedAt: Instant.MinValue, ExpiresAt: Instant.MaxValue,
                    BCards: new[]
                    {
                        new BCardDto
                        {
                            Type = BCardEffect.Type96IncreaseFairyElement.Type(),
                            SubType = BCardEffect.Type96IncreaseFairyElement.SubType(),
                            FirstData = 5,
                        },
                    }),
            });
            var provider = BattleStats.Provider(buffs.Object, _service);

            Assert.AreEqual(0, provider.GetStats(_session.Character).ElementRate);
            Wear(EquipmentType.Fairy, FairyVnum);

            Assert.AreEqual(35, provider.GetStats(_session.Character).ElementRate);
        }

        [TestMethod]
        public void AWeaponInTheBagCountsForNothing()
        {
            var item = _items.Create(MainWeaponVnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(item, _session.Character.CharacterId),
                NoscorePocketType.Equipment);

            Assert.AreEqual(EquipmentStats.None, _service.Resolve(_session.Character));
        }
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
        public void AWornWingSkinCarriesItsDeclaredEffects()
        {
            Wear(EquipmentType.WingSkin, WingSkinVnum);

            var effects = _service.Resolve(_session.Character).BCards;

            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual(10, effects[0].FirstData);
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
            var provider = BattleStats.Provider(buffs.Object, _service);

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

        private SpecialistInstance WearSpecialist()
        {
            var card = (SpecialistInstance)_items.Create(SpecialistVnum, 1);
            card.DamageMinimum = 30;
            card.DamageMaximum = 45;
            card.CloseDefence = 18;
            card.SpWater = 5;
            card.ElementRate = 10;
            card.SpElement = 7;
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(card, _session.Character.CharacterId),
                NoscorePocketType.Wear, (short)EquipmentType.Sp);
            return card;
        }

        [TestMethod]
        public void AWornSpecialistCardCountsForNothingUntilTransformed()
        {
            WearSpecialist();
            _session.Character.UseSp = false;

            Assert.AreEqual(EquipmentStats.None, _service.Resolve(_session.Character));
        }

        [TestMethod]
        public void TheSpecialistCardAddsItsStatsOnceTransformed()
        {
            WearSpecialist();
            _session.Character.UseSp = true;

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(30, stats.MinHit);
            Assert.AreEqual(45, stats.MaxHit);
            Assert.AreEqual(30, stats.MinDistance, "The card's skills hit from range too");
            Assert.AreEqual(18, stats.CloseDefence);
            Assert.AreEqual(4, stats.FireResistance);
            Assert.AreEqual(18, stats.WaterResistance, "The card's own 13 plus 5 from perfection");
            Assert.AreEqual(17, stats.SpecialistElementRate);
            Assert.AreEqual(0, stats.ElementRate, "The card's element rate is kept apart from the fairy's");
        }

        [TestMethod]
        public void TheSpecialistElementRateReachesTheCombatStats()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>()))
                .Returns(new List<BuffInstance>());
            var provider = BattleStats.Provider(buffs.Object, _service);
            WearSpecialist();
            _session.Character.UseSp = true;

            Assert.AreEqual(17, provider.GetStats(_session.Character).ElementRateSp);
        }

        [TestMethod]
        public void AWornJewelsCellonOptionsBecomeEffects()
        {
            var necklace = Wear(EquipmentType.Necklace, NecklaceVnum);
            AddOption(necklace, CellonType.Hp, 300);
            AddOption(necklace, CellonType.Mp, 250);
            AddOption(necklace, CellonType.HpRecovery, 20);
            AddOption(necklace, CellonType.MpRecovery, 25);
            AddOption(necklace, CellonType.CriticalDamageDecrease, 15);

            var effects = _service.Resolve(_session.Character).BCards
                .ToDictionary(b => b.Effect(), b => b.FirstData);

            Assert.AreEqual(300, effects[BCardEffect.MaxHpmpMaximumHpIncreased]);
            Assert.AreEqual(250, effects[BCardEffect.MaxHpmpMaximumMpIncreased]);
            Assert.AreEqual(20, effects[BCardEffect.RecoveryHpRecoveryIncreased]);
            Assert.AreEqual(25, effects[BCardEffect.RecoveryMpRecoveryIncreased]);
            Assert.AreEqual(15, effects[BCardEffect.CriticalDamageFromCriticalDecreased]);
        }

        [TestMethod]
        public void MpConsumptionOptionsAddUpAsAPercentage()
        {
            var necklace = Wear(EquipmentType.Necklace, NecklaceVnum);
            AddOption(necklace, CellonType.MpConsumption, 7);
            var ring = Wear(EquipmentType.Ring, NecklaceVnum);
            AddOption(ring, CellonType.MpConsumption, 5);

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(12, stats.MpConsumptionDecrease);
            Assert.AreEqual(0, stats.BCards.Count, "It is not an effect card");
        }

        [TestMethod]
        public void AJewelInTheBagGivesNoOption()
        {
            var necklace = _items.Create(NecklaceVnum, 1);
            _session.Character.InventoryService.AddItemToPocket(
                InventoryItemInstance.Create(necklace, _session.Character.CharacterId),
                NoscorePocketType.Equipment);
            AddOption(necklace, CellonType.Hp, 300);

            Assert.AreEqual(0, _service.Resolve(_session.Character).BCards.Count);
        }

        // Combat resolves the gear on every hit, so the options must not cost a query each time.
        [TestMethod]
        public void AJewelsOptionsAreReadOnce()
        {
            var necklace = Wear(EquipmentType.Necklace, NecklaceVnum);
            AddOption(necklace, CellonType.Hp, 300);

            _service.Resolve(_session.Character);
            _service.Resolve(_session.Character);

            Assert.AreEqual(1, _optionReads);
        }

        [TestMethod]
        public void ACriticalDamageDecreaseReachesTheCombatStats()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>()))
                .Returns(new List<BuffInstance>());
            var provider = BattleStats.Provider(buffs.Object, _service);
            var necklace = Wear(EquipmentType.Necklace, NecklaceVnum);
            AddOption(necklace, CellonType.CriticalDamageDecrease, 15);

            Assert.AreEqual(-15, provider.GetStats(_session.Character).CriticalDamageReceived);
        }

        [TestMethod]
        public void SkillPointsSpentOnTheCardAddToTheTransformedStats()
        {
            var card = WearSpecialist();
            card.SlDamage = 10;
            card.SlDefence = 10;
            card.SlElement = 1;
            card.SlHp = 10;
            _session.Character.UseSp = true;

            var stats = _service.Resolve(_session.Character);

            Assert.AreEqual(30 + 50 + 5 + 10, stats.MinHit, "attack 10 adds 50, its first milestone 5, HP/MP 10 adds 10");
            Assert.AreEqual(18 + 10, stats.CloseDefence, "defence 10 adds 10");
            Assert.AreEqual(5, stats.DefenceDodge, "defence 10 adds 5 dodge");
            Assert.AreEqual(17 + 1 + 2, stats.SpecialistElementRate, "element 1 adds 1, its first milestone 2");
            Assert.AreEqual(10, stats.SpecialistHpMpPercent);
        }

        [TestMethod]
        public void SkillPointsCountForNothingUntilTransformed()
        {
            var card = WearSpecialist();
            card.SlDamage = 10;
            _session.Character.UseSp = false;

            Assert.AreEqual(EquipmentStats.None, _service.Resolve(_session.Character));
        }
    }
}
