//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Buff;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Items;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyUpgradeType = NosCore.Data.Enumerations.Family.FamilyUpgradeType;
using MapType = NosCore.Database.Entities.MapType;

namespace NosCore.EndToEnd.Tests.Infrastructure
{
    // The smallest world a server will run: no client files, only what the scenarios stand on.
    // Two open maps joined by a portal, a miniland, a shopkeeper with one item to sell, a broker
    // to talk to, a chest and a weapon to hand out, what a new adventurer is given, a mount with its
    // booster, creatures to hit, a market where players may keep a shop, with a grocer, and a
    // gambler who rolls an item's rarity.

    // to talk to, a family manager, a chest and a weapon to hand out, what a new adventurer is
    // given, creatures to hit, and a market where players may keep a shop, with a grocer.
    // Characters are added by the scenarios.
    public static class WorldData
    {
        public const string Password = "test";
        public const short Town = 1;
        public const short Meadow = 2;
        public const short MinilandMap = 20001;
        public const short Seed = 2024;
        public const long SeedPrice = 100;
        public const short Chest = 3000;
        public const short Blade = 4000;
        public const short Pebble = 2025;
        public const short Potion = 1002;
        public const short PotionHp = 50;
        public const short Hat = 4001;
        public const short Fairy = 4002;
        public const short FairyElementRate = 30;
        public const short CostumeHat = 4003;

        // Lasts an hour from when it is first worn.
        public const short Amulet = 4004;
        public const long AmuletSeconds = 3600;
        public const byte AmuletLevel = 10;

        // Holds three seeds and two pebbles.
        public const short Box = 4100;
        public const short BoxSeeds = 3;
        public const short BoxPebbles = 2;

        // Holds one blade a swordsman may wield or one an adventurer may, whichever fits who opens it.
        public const short ClassBox = 4101;
        public const short SwordsmanBlade = 4102;
        public const short AdventurerBlade = 4103;

        // A box made to lie on a map, opened where it lies; it holds two pebbles.
        public const short GroundBox = 4104;
        public const short GroundBoxPebbles = 2;
        public const short Bench = 3001;
        public const short PowerSeed = 2026;

        // Kept in the etc pocket like the seeds, but food, so a sort puts it after them.
        public const short Kebab = 9200;
        public const int ChestSlots = 4;
        public const int Shopkeeper = 1;
        private const short ShopkeeperVNum = 1;
        public const int WeakMonster = 100;
        public const int SturdyMonster = 101;
        private const short WeakVNum = 2;
        private const short SturdyVNum = 3;

        // Falls to any hit and takes ten minutes to come back: only one scenario may kill it.
        public const int SlowMonster = 102;
        private const short SlowVNum = 4;
        public const byte FamilySize = 50;

        // Falls to any hit, is back a second later and leaves two pebbles every time.
        public const int Prey = 103;
        public const short PreyVNum = 6;
        public const int PreyDrop = 2;

        // Leaves everyone alone until it is hit, then hits back: a first blow is survived at full
        // HP and a second one is not. It cannot be killed in a scenario.
        public const int Biter = 104;
        private const short BiterVNum = 7;

        // Hits back whoever has its attention, for a single HP a blow, so a fight with it can last.
        public const int Brute = 105;
        private const short BruteVNum = 12;

        // Hits back once and blows up doing it: every enemy within two cells takes WigglerBlast, and
        // it is gone.
        public const int Wiggler = 1100;
        private const short WigglerVNum = 1100;
        public const int WigglerBlast = 50;
        public const short WigglerX = 50;
        public const short WigglerY = 15;

        // Charge Power as the client data has it: melee attack +250 on the Swordsman's next blow,
        // and a basic Swordsman blow to spend it on.
        public const short ChargeSkill = 400;
        public const short ChargeSkillCastId = 12;
        public const int ChargeAttack = 250;
        public const short SwordsmanSwing = 401;

        // Taunting Shot as the client data has it: a Swordsman's debuff from job level 9 that deals
        // no damage and draws the target's attention to the caster.
        public const short TauntSkill = 230;
        public const short TauntSkillCastId = 10;
        public const byte TauntSkillJobLevel = 9;

        // A skill no adventurer starts with: costly, and a long time before it can be used again.
        public const short SlowSkill = 210;
        public const short SlowSkillCastId = 2;
        public const short SlowSkillMp = 20;

        // A swordsman skill that uses up a bandage every time it is cast.
        public const short BandageSkill = 211;
        public const short BandageSkillCastId = 4;
        public const short Bandage = 2087;

        // A swordsman skill that carries its caster up to what it strikes.
        public const short RushSkill = 214;
        public const short RushSkillCastId = 11;

        // An upgrade of the adventurer's first skill that has it weaken what it strikes.
        public const short WeakeningUpgrade = 202;
        public const short WeakeningCard = 58;

        // A time-space stone on the meadow, far from everything else there.
        public const short StoneX = 5;
        public const short StoneY = 95;
        public const short StoneTimeSpace = 4;

        // The sample time-space, entered on foot from the meadow; its rooms are one open map.
        public const short SampleTimeSpaceX = 15;
        public const short SampleTimeSpaceY = 95;
        public const short SampleRoomMap = 2550;

        // A swordsman's skill cast on a friendly player, laid out like Sacrifice: a card for whoever
        // it is cast on, and one for whoever casts it.
        public const short SacrificeSkill = 213;
        public const short SacrificeSkillCastId = 8;
        public const short SacrificeCircleCard = 59;
        public const short SacrificeSpiritCard = 60;
        public const int SacrificeCardTime = 300;

        // A creature to own as a pet: it has mana, and one skill of its own for its skill bar that takes
        // a moment to cast and three seconds to come back.
        public const short PetVNum = 10;
        public const short PetSkill = 220;
        public const short PetSkillMp = 40;
        public const short PetSkillCooldown = 30;
        public const short PetBasicCooldown = 20;

        // A partner, and a partner card whose one skill is cast on the partner itself and buffs it.
        public const short PartnerVNum = 13;
        public const short PartnerCard = 4005;
        private const short PartnerMorph = 2333;
        private const byte PartnerSkillSet = 9;
        public const short PartnerSkill = 231;
        public const short PartnerSkillMp = 30;
        public const short PartnerSkillCooldown = 300;
        public const short PartnerBuff = 61;

        // Has something to say, so talking to it is answered, and stands away from the shopkeeper.
        public const int Broker = 2;
        public const short BrokerDialog = 1;
        private const short BrokerVNum = 5;

        // The one map where players may keep a shop, with a portal in a far corner.
        public const short Market = 3;
        public const short MarketPortalX = 95;
        public const short MarketPortalY = 5;

        // Sells a seed, a blade, a token for reputation and a gem on its first shelf, and a
        // trinket on its second.
        public const int Grocer = 3;
        private const short GrocerVNum = 8;
        public const short GrocerX = 20;
        public const short GrocerY = 20;
        public const long BladePrice = 1000;
        public const short Gem = 2027;
        public const long GemPrice = 5000;
        public const short Token = 2028;
        public const long TokenReputation = 300;
        public const short Trinket = 2030;
        public const long TrinketPrice = 40;

        // Worth something, yet neither sold to a shop nor handed to another player.
        public const short Keepsake = 2029;

        // Teaches one skill nobody starts with, on the market.
        public const int Teacher = 9700;
        private const short TeacherVNum = 9701;
        public const short TeacherX = 30;
        public const short TeacherY = 20;
        public const short TaughtSkill = SlowSkill;

        // A mount whose booster gives one buff when it is used and another when the boost runs out,
        // and restores some HP.
        public const short Steed = 5000;
        public const short SteedLook = 2000;
        public const byte SteedSpeed = 20;
        public const byte SteedBoost = 2;
        public const byte SteedBoostSeconds = 2;
        public const byte SteedHpPerLevel = 10;
        public const short Booster = 5001;
        public const short BoostCard = 50;
        public const short SteedStartCard = 51;
        public const short SteedEndCard = 52;
        public const int SteedCardTime = 30;

        // Two adventurer skills nobody starts with, cast on oneself: one leaves a light and a heavy
        // bleeding, the other stops bleedings up to the light one's level. Bleedings are in card
        // group 0, as in the client data.
        public const short WoundSkill = 206;
        public const short WoundSkillCastId = 6;
        public const short StopBleedingSkill = 207;
        public const short StopBleedingSkillCastId = 7;
        public const short LightBleeding = 56;
        public const short HeavyBleeding = 57;

        // A skill cast on oneself that puts on a card whose effect stays on its bearer. An adventurer
        // keeps only skills 200 to 210, so it takes one of those nobody else uses.
        public const short WingsSkill = 208;
        public const short WingsSkillCastId = 17;
        public const short WingsCard = 9300;
        public const short WingsCardTime = 600;

        // The Chef card and the skin that dresses it as a masterchef.
        public const short ChefCard = 4575;
        public const short ChefLook = 38;
        public const short MasterchefSkin = 9227;
        public const short MasterchefLook = 37;

        // A buff whose main weapon always puts one mark on what it hits, and whose secondary weapon
        // always puts another. The marks do nothing else, so a monster wearing one fights as before.
        public const short WeaponCard = 53;
        public const short MainWeaponMark = 54;
        public const short SecondaryWeaponMark = 55;

        // The Mother Cuby raid as its script lays it out: its gate on the entrance map, the seal
        // that opens a team, and its boss, which falls to any hit.
        public const short JellyEntrance = 2510;
        public const short JellySquare = 2512;
        public const short RaidGateX = 20;
        public const short RaidGateY = 5;
        public const short RaidSeal = 1127;
        public const short RaidReward = 4684;
        public const byte RaidLevel = 30;
        public const int RaidTeamMinimum = 5;
        public const short RaidMissionId = 9100;

        // The Instant Combat arena, with one standing monster for each wave of the lowest bracket and a
        // prize of two after the first wave.
        public const short InstantCombatArena = 2004;
        public const short InstantCombatWaveVNum = 17;
        public const short InstantCombatPrize = 9600;
        public const short InstantCombatPrizeAmount = 2;

        // Sells a family its warehouse, to families of the second level and above only.
        public const int FamilyManager = 4;
        private const short FamilyManagerVNum = 9;
        private const byte FamilyShop = 48;
        public const short FamilyWarehouse = 3002;
        public const long FamilyWarehousePrice = 500;
        public const byte FamilyWarehouseLevel = 2;

        // Starts the one flower quest when picked up, which a character below its level cannot take.
        public const short WildSoundFlower = 1086;

        // Examines a relict for the gold it costs, giving the one piece the relict holds.
        public const int Archaeologist = 5;
        private const short ArchaeologistVNum = 11;
        private const short ArchaeologistDialog = 2;
        private const byte ArchaeologistShop = 31;
        public const short ArchaeologistX = 100;
        public const short ArchaeologistY = 130;
        public const short Relict = 2031;
        public const int RelictFee = 10000;
        public const short RelictPiece = 2032;
        public const short FlowerQuest = 5981;
        public const byte FlowerQuestLevel = 20;

        // Done with a single kill of the prey and handed in by nobody, so the kill completes it.
        public const short PreyQuest = 600;

        // Hands out a score quest per button: the first for any level, the second only from level 50.
        public const int ScoreKeeper = 1000;
        private const short ScoreKeeperVNum = 1000;
        public const short ScoreKeeperDialog = 1000;
        public const short ScoreKeeperX = 70;
        public const short ScoreKeeperY = 130;
        public const short AnyLevelScoreQuest = 1000;
        public const short HighLevelScoreQuest = 1001;
        public const byte HighLevelScoreQuestLevel = 50;

        // Glacernon: the angels' citadel the ship lands on, the demons' beside it, and the deck the
        // act4-ship script opens. The shipmaster in town takes passengers.
        public const short AngelCitadel = 130;
        public const short DemonCitadel = 131;
        public const short ShipDeck = 148;
        public const int Shipmaster = 6;
        private const short ShipmasterVNum = 14;
        public const short ShipmasterX = 90;
        public const short ShipmasterY = 112;
        private const short ShipmasterDialog = 3;

        // Gambles an item's rarity, and does nothing else: its shop type offers that one service.
        public const int Gambler = 8;
        private const short GamblerVNum = 16;
        public const short GamblerX = 94;
        public const short GamblerY = 112;
        private const short GamblerDialog = 4;
        private const byte GamblingShop = 4;
        public const short CellaPowder = 1014;
        public const short BlessingAmulet = 282;
        public const short ChampionBlade = 4007;
        // Guards a gamble only on an item bound to the gambler.
        public const short EventAmulet = 4006;
        public const int EventAmuletUses = 3;

        public static async Task SeedAsync(NosCoreContext context)
        {
            context.Add(OpenMap(Town, 200, 200));
            context.Add(OpenMap(Meadow, 100, 100));
            context.Add(OpenMap(MinilandMap, 40, 40, NosCore.Packets.Enumerations.MapType.Miniland));
            context.Add(new Portal
            {
                SourceMapId = Town, SourceX = 90, SourceY = 116,
                DestinationMapId = Meadow, DestinationX = 10, DestinationY = 10,
                Type = PortalType.Open
            });
            context.Add(new Portal
            {
                SourceMapId = Meadow, SourceX = 8, SourceY = 10,
                DestinationMapId = Town, DestinationX = 88, DestinationY = 116,
                Type = PortalType.Open
            });
            context.Add(new Portal
            {
                SourceMapId = Town, SourceX = 70, SourceY = 116,
                DestinationMapId = MinilandMap, DestinationX = 5, DestinationY = 8,
                Type = PortalType.Miniland
            });
            context.Add(new Item
            {
                VNum = Seed,
                Name = "seed",
                Type = NoscorePocketType.Etc,
                ItemType = ItemType.Main,
                Price = SeedPrice,
                IsDroppable = true,
                IsSoldable = true,
                IsTradable = true
            });
            // Kept in the miniland pocket, a chest is a warehouse of as many slots as its points.
            context.Add(new Item
            {
                VNum = Chest, Name = "chest", Type = NoscorePocketType.Miniland, ItemType = ItemType.House,
                IsWarehouse = true, MinilandObjectPoint = ChestSlots
            });
            // Something to stand in a miniland wherever its owner likes, and what an invitation there costs.
            context.Add(new Item
            {
                VNum = Bench, Name = "bench", Type = NoscorePocketType.Miniland, ItemType = ItemType.Garden, Width = 1, Height = 1,
                MinilandObjectPoint = 3
            });
            context.Add(new Item
            {
                VNum = PowerSeed, Name = "powerseed", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, Material = MaterialType.SeedOfPower
            });
            context.Add(new Item { VNum = Kebab, Name = "kebab", Type = NoscorePocketType.Etc, ItemType = ItemType.Food });
            // A weapon that may change hands, unlike what an adventurer starts with.
            context.Add(new Item
            {
                VNum = Blade, Name = "blade", Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, Price = 1000, IsSoldable = true, IsTradable = true
            });
            // A second thing to stack, a potion, and things to wear: a hat, a fairy and a costume hat.
            context.Add(new Item
            {
                VNum = Pebble, Name = "pebble", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, IsDroppable = true
            });
            context.Add(new I18NItem { Key = "seed", RegionType = RegionType.EN, Text = "seed" });
            context.Add(new I18NItem { Key = "pebble", RegionType = RegionType.EN, Text = "pebble" });
            context.Add(new Item
            {
                VNum = Potion, Name = "potion", Type = NoscorePocketType.Main, ItemType = ItemType.Potion, Hp = PotionHp
            });
            context.Add(new Item
            {
                VNum = Hat, Name = "hat", Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Hat
            });
            context.Add(new Item
            {
                VNum = Fairy, Name = "fairy", Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Fairy, Element = ElementType.Fire, ElementRate = FairyElementRate
            });
            context.Add(new Item
            {
                VNum = CostumeHat, Name = "costumehat", Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.CostumeHat
            });
            context.Add(new Item
            {
                VNum = Amulet, Name = "amulet", Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Amulet, ItemValidTime = AmuletSeconds, LevelMinimum = AmuletLevel, Price = 200
            });
            context.Add(new Item
            {
                VNum = Box, Name = "box", Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = ItemEffectType.BoxEffect
            });
            context.Add(new Item
            {
                VNum = ClassBox, Name = "classbox", Type = NoscorePocketType.Main, ItemType = ItemType.Special,
                Effect = ItemEffectType.ClassBox
            });
            context.Add(new Item
            {
                VNum = GroundBox, Name = "groundbox", Type = NoscorePocketType.Main, ItemType = ItemType.Map,
                Effect = ItemEffectType.BoxEffect, IsDroppable = true
            });
            context.Add(new Item
            {
                VNum = SwordsmanBlade, Name = "swordsmanblade", Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, Class = (byte)(1 << (byte)CharacterClassType.Swordsman)
            });
            context.Add(new Item
            {
                VNum = AdventurerBlade, Name = "adventurerblade", Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, Class = (byte)(1 << (byte)CharacterClassType.Adventurer)
            });
            context.Add(new Item
            {
                VNum = Steed, Name = "steed", Type = NoscorePocketType.Mount, ItemType = ItemType.Special, Speed = SteedSpeed,
                Morph = SteedLook, SpeedBoost = SteedBoost, SpeedBoostDuration = SteedBoostSeconds,
                SpeedBoostRestoresHpPerLevel = SteedHpPerLevel, SpeedBoostStartCardId = SteedStartCard, SpeedBoostEndCardId = SteedEndCard
            });
            context.Add(new Item
            {
                VNum = Booster, Name = "booster", Type = NoscorePocketType.Main, ItemType = ItemType.Special,
                Effect = ItemEffectType.SpeedBooster
            });
            context.Add(new Item
            {
                VNum = ChefCard, Name = "chefcard", Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, Morph = ChefLook
            });
            context.Add(new Item
            {
                VNum = MasterchefSkin, Name = "masterchefskin", Type = NoscorePocketType.Main, ItemType = ItemType.Special,
                Effect = ItemEffectType.SpecialistSkin, EffectValue = ChefLook, Morph = MasterchefLook
            });
            context.Add(new Card { CardId = BoostCard, Name = "boost" });
            context.Add(new Card { CardId = SteedStartCard, Name = "steedstart", Duration = SteedCardTime });
            context.Add(new Card { CardId = SteedEndCard, Name = "steedend", Duration = SteedCardTime });
            context.Add(new Card { CardId = WeaponCard, Name = "weapon", Duration = 6000 });
            context.Add(new Card { CardId = MainWeaponMark, Name = "mainmark", Duration = 10, BuffType = BuffType.Bad });
            context.Add(new Card { CardId = SecondaryWeaponMark, Name = "secondarymark", Duration = 10, BuffType = BuffType.Bad });
            context.Add(new BCard
            {
                CardId = WeaponCard, Type = BCardType.CardType.SpecialEffects2, SubType = 31, FirstData = 100, SecondData = MainWeaponMark
            });
            context.Add(new BCard
            {
                CardId = WeaponCard, Type = BCardType.CardType.SpecialEffects2, SubType = 41, FirstData = 100, SecondData = SecondaryWeaponMark
            });
            // The card an item gives is the one its buff effect names.
            context.Add(new BCard { ItemVNum = Booster, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = BoostCard });
            context.Add(new Skill
            {
                SkillVNum = WoundSkill, Name = "skill206", CastId = WoundSkillCastId, SkillType = 1, TargetType = 1, Cooldown = 6,
                Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = StopBleedingSkill, Name = "skill207", CastId = StopBleedingSkillCastId, SkillType = 1, TargetType = 1, Cooldown = 6,
                Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = WingsSkill, Name = "skill208", CastId = WingsSkillCastId, SkillType = 1, TargetType = 1, Cooldown = 6,
                Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Card { CardId = WingsCard, Name = "wings", Level = 1, Duration = WingsCardTime, BuffType = BuffType.Good, HasLastingEffect = true });
            context.Add(new BCard { SkillVNum = WingsSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = WingsCard });
            context.Add(new Card { CardId = LightBleeding, Name = "lightbleeding", Level = 1, Duration = 600, BuffType = BuffType.Bad });
            context.Add(new Card { CardId = HeavyBleeding, Name = "heavybleeding", Level = 3, Duration = 600, BuffType = BuffType.Bad });
            context.Add(new BCard { SkillVNum = WoundSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = LightBleeding });
            context.Add(new BCard { SkillVNum = WoundSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = HeavyBleeding });
            context.Add(new BCard { SkillVNum = StopBleedingSkill, Type = BCardType.CardType.Buff, SubType = 51, FirstData = 0, SecondData = 2 });
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = Box, ItemGeneratedVNum = Seed, ItemGeneratedAmount = BoxSeeds, Probability = 1, MaximumOriginalItemRare = 8
            });
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = Box, ItemGeneratedVNum = Pebble, ItemGeneratedAmount = BoxPebbles, Probability = 1, MaximumOriginalItemRare = 8
            });
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = GroundBox, ItemGeneratedVNum = Pebble, ItemGeneratedAmount = GroundBoxPebbles, Probability = 1,
                MaximumOriginalItemRare = 8
            });
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = ClassBox, ItemGeneratedVNum = SwordsmanBlade, ItemGeneratedAmount = 1, Probability = 1,
                MaximumOriginalItemRare = 8, RollGroup = 0
            });
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = ClassBox, ItemGeneratedVNum = AdventurerBlade, ItemGeneratedAmount = 1, Probability = 1,
                MaximumOriginalItemRare = 8, RollGroup = 1
            });
            // What a new adventurer is given: its club, its catapult, its clothes, and its three skills.
            context.Add(Worn(1, EquipmentType.MainWeapon, ItemType.Weapon));
            context.Add(Worn(8, EquipmentType.SecondaryWeapon, ItemType.Weapon));
            context.Add(Worn(12, EquipmentType.Armor, ItemType.Armor));
            context.Add(new Item { VNum = 2081, Name = "item2081", Type = NoscorePocketType.Etc, ItemType = ItemType.Main });
            // As the client data has them: skill type 1 is an active skill.
            context.Add(new Skill
            {
                SkillVNum = 200, Name = "skill200", CastId = 0, SkillType = 1, Type = 0, Range = 1, CastTime = 2, Cooldown = 6,
                Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = 201, Name = "skill201", CastId = 1, SkillType = 1, Type = 1, Range = 7, CastTime = 6, Cooldown = 6,
                Effect = 201, AttackAnimation = 23, CastAnimation = 26, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = 209, Name = "skill209", CastId = 16, SkillType = 1, CastTime = 6, Cooldown = 150, MpCost = 10,
                UpgradeSkill = 3, LevelMinimum = 1, Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            // As the client data has them: skill type 2 is an upgrade of the skill its UpgradeSkill names.
            context.Add(new Skill
            {
                SkillVNum = WeakeningUpgrade, Name = "skill202", CastId = 0, SkillType = 2, UpgradeSkill = 200, Effect = -1,
                AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Card { CardId = WeakeningCard, Name = "weakening", Duration = 300, BuffType = BuffType.Bad });
            await context.SaveChangesAsync();
            context.Add(new BCard { SkillVNum = WeakeningUpgrade, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = WeakeningCard });
            context.Add(new Skill
            {
                SkillVNum = SlowSkill, Name = "skill210", CastId = SlowSkillCastId, SkillType = 1, Type = 0, Range = 1, CastTime = 2,
                Cooldown = 300, MpCost = SlowSkillMp, Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });

            context.Add(new Item { VNum = Bandage, Name = "bandage", Type = NoscorePocketType.Etc, ItemType = ItemType.Magical, Effect = ItemEffectType.Bandage });
            context.Add(new Skill
            {
                SkillVNum = BandageSkill, Name = "skill211", CastId = BandageSkillCastId, SkillType = 1, Class = 1, Type = 0, Range = 1,
                CastTime = 2, Cooldown = 6, ItemVNum = Bandage, Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });

            context.Add(new Skill
            {
                SkillVNum = SacrificeSkill, Name = "skill213", CastId = SacrificeSkillCastId, SkillType = 1,
                Class = (byte)CharacterClassType.Swordsman, Type = 0, TargetType = 2, Range = 10, CastTime = 0, Cooldown = 40,
                Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Card { CardId = SacrificeCircleCard, Name = "sacrificecircle", Duration = SacrificeCardTime });
            context.Add(new Card { CardId = SacrificeSpiritCard, Name = "sacrificespirit", Duration = SacrificeCardTime });
            await context.SaveChangesAsync();
            context.Add(new BCard { ItemVNum = Bandage, Type = BCardType.CardType.Buff, SubType = 51, FirstData = 0, SecondData = 2 });
            context.Add(new BCard
            {
                SkillVNum = SacrificeSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = SacrificeCircleCard
            });
            context.Add(new BCard
            {
                SkillVNum = SacrificeSkill, Type = BCardType.CardType.MeditationSkill, SubType = 51, FirstData = 100,
                SecondData = SacrificeSpiritCard
            });

            context.Add(new Skill
            {
                SkillVNum = TauntSkill, Name = "skill230", CastId = TauntSkillCastId, SkillType = 1, Type = 1, Class = 1,
                LevelMinimum = TauntSkillJobLevel, Range = 10, CastTime = 6, Cooldown = 100, MpCost = 30, Kind = SkillKind.Debuff,
                UsesSecondaryWeapon = true, Effect = 528, AttackAnimation = 23, CastAnimation = 26, CastEffect = 527
            });
            context.Add(new Skill
            {
                SkillVNum = ChargeSkill, Name = "skill400", CastId = ChargeSkillCastId, SkillType = 1,
                Class = (byte)CharacterClassType.Swordsman, Type = 4, TargetType = 1, Cooldown = 6,
                Effect = -1, AttackAnimation = -1, CastAnimation = -1, CastEffect = -1
            });
            context.Add(new Skill
            {
                SkillVNum = SwordsmanSwing, Name = "skill401", CastId = 0, SkillType = 1, Class = (byte)CharacterClassType.Swordsman,
                Type = 0, Range = 1, CastTime = 2, Cooldown = 6, Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });
            await context.SaveChangesAsync();
            context.Add(new BCard { SkillVNum = TauntSkill, Type = BCardType.CardType.SpecialEffects2, SubType = 11, FirstData = 1 });
            context.Add(new BCard { SkillVNum = ChargeSkill, Type = BCardType.CardType.AttackPower, SubType = 21, FirstData = ChargeAttack });

            context.Add(new Skill
            {
                SkillVNum = RushSkill, Name = "skill214", CastId = RushSkillCastId, SkillType = 1, Type = 0, Class = 1, Range = 8,
                CastTime = 2, Cooldown = 6, Effect = 200, AttackAnimation = 11, CastAnimation = -1, CastEffect = -1
            });
            await context.SaveChangesAsync();
            context.Add(new BCard { SkillVNum = RushSkill, Type = BCardType.CardType.SpecialActions, SubType = 31, FirstData = 8 });

            // As the client data has them: skill type 5 is a skill for a NosMate's skill bar.
            context.Add(new Skill
            {
                SkillVNum = PetSkill, Name = "skill220", CastId = 3, SkillType = 5, Type = 0, Range = 1, CastTime = 2,
                Cooldown = PetSkillCooldown, MpCost = PetSkillMp, Effect = 220, AttackAnimation = 12, CastAnimation = -1, CastEffect = -1
            });

            // Creatures that stand still on the meadow: one that falls to any hit, one that does
            // not, and one that stays dead for a while.
            context.Add(new NpcMonster { NpcMonsterVNum = WeakVNum, Name = "weak", Level = 1, MaxHp = 1, Xp = 40, JobXp = 10 });
            context.Add(new NpcMonster { NpcMonsterVNum = SturdyVNum, Name = "sturdy", Level = 1, MaxHp = 100000 });
            context.Add(new NpcMonster { NpcMonsterVNum = SlowVNum, Name = "slow", Level = 1, MaxHp = 1, RespawnTime = 6000 });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = WeakMonster, MapId = Meadow, MapX = 30, MapY = 30, VNum = WeakVNum });
            context.Add(new MapMonster { MapMonsterId = SturdyMonster, MapId = Meadow, MapX = 60, MapY = 60, VNum = SturdyVNum });
            context.Add(new MapMonster { MapMonsterId = SlowMonster, MapId = Meadow, MapX = 45, MapY = 45, VNum = SlowVNum });

            context.Add(new NpcMonster { NpcMonsterVNum = PreyVNum, Name = "prey", Level = 1, MaxHp = 1, Xp = 40, JobXp = 10 });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = Prey, MapId = Meadow, MapX = 30, MapY = 70, VNum = PreyVNum });
            context.Add(new Drop { MonsterVNum = PreyVNum, VNum = Pebble, Amount = PreyDrop, DropChance = 10000 });
            context.Add(new NpcMonster
            {
                NpcMonsterVNum = BiterVNum, Name = "biter", Level = 1, MaxHp = 100000, DamageMinimum = 150, DamageMaximum = 150,
                Concentrate = 2000, BasicRange = 1, BasicCooldown = 20
            });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = Biter, MapId = Meadow, MapX = 80, MapY = 20, VNum = BiterVNum });

            context.Add(new NpcMonster
            {
                NpcMonsterVNum = BruteVNum, Name = "brute", Level = 1, MaxHp = 100000, DamageMinimum = 1, DamageMaximum = 1,
                Concentrate = 2000, BasicRange = 1, BasicCooldown = 20
            });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = Brute, MapId = Meadow, MapX = 80, MapY = 80, VNum = BruteVNum });

            context.Add(new NpcMonster
            {
                NpcMonsterVNum = WigglerVNum, Name = "wiggler", Level = 1, MaxHp = 100000, DamageMinimum = 1, DamageMaximum = 1,
                Concentrate = 2000, BasicRange = 1, BasicCooldown = 20, DisappearAfterHitting = true
            });
            await context.SaveChangesAsync();
            context.Add(new MapMonster { MapMonsterId = Wiggler, MapId = Meadow, MapX = WigglerX, MapY = WigglerY, VNum = WigglerVNum });
            context.Add(new BCard
            {
                NpcMonsterVNum = WigglerVNum, Type = BCardType.CardType.SpecialDamageAndExplosions, SubType = 21, FirstData = 2,
                SecondData = WigglerBlast, Recipient = BCardRecipient.Enemy
            });

            context.Add(new NpcMonster
            {
                NpcMonsterVNum = PetVNum, Name = "pet", Level = 1, MaxHp = 100, MaxMp = 100, HpBonus = 100, MpBonus = 100,
                BasicRange = 1, BasicCooldown = PetBasicCooldown
            });
            await context.SaveChangesAsync();
            context.Add(new NpcMonsterSkill { NpcMonsterVNum = PetVNum, SkillVNum = PetSkill });

            context.Add(new NpcMonster
            {
                NpcMonsterVNum = PartnerVNum, Name = "partner", Level = 1, MaxHp = 100, MaxMp = 100, HpBonus = 100, MpBonus = 100,
                BasicRange = 1, BasicCooldown = 20
            });
            context.Add(new Item
            {
                VNum = PartnerCard, Name = "partnercard", Type = NoscorePocketType.Equipment, ItemType = ItemType.Specialist,
                EquipmentSlot = EquipmentType.Sp, ItemSubType = 4, Morph = PartnerMorph, PartnerSkillSet = PartnerSkillSet
            });
            // As the client data has them: target type 1 centres the skill on its caster.
            context.Add(new Skill
            {
                SkillVNum = PartnerSkill, Name = "skill231", CastId = 0, SkillType = 5, UpgradeType = PartnerSkillSet, TargetType = 1,
                Cooldown = PartnerSkillCooldown, MpCost = PartnerSkillMp, Effect = 230, AttackAnimation = 12, CastAnimation = -1,
                CastEffect = -1
            });
            context.Add(new Card { CardId = PartnerBuff, Name = "partnerbuff", Duration = 600, BuffType = BuffType.Good });
            await context.SaveChangesAsync();
            context.Add(new BCard { SkillVNum = PartnerSkill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = 100, SecondData = PartnerBuff });

            context.Add(new NpcMonster { NpcMonsterVNum = ShopkeeperVNum, Name = "shopkeeper", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();

            var shopkeeper = new MapNpc { MapNpcId = Shopkeeper, MapId = Town, MapX = 84, MapY = 118, VNum = ShopkeeperVNum };
            context.Add(shopkeeper);
            var shop = new Shop { MapNpcId = Shopkeeper, MenuType = 0, ShopType = 0 };
            context.Add(shop);
            await context.SaveChangesAsync();
            context.Add(new ShopItem { ShopId = shop.ShopId, ItemVNum = Seed, Slot = 0, Type = 0 });
            context.Add(new NpcTalk { DialogId = BrokerDialog, Name = "broker" });
            context.Add(new NpcMonster { NpcMonsterVNum = BrokerVNum, Name = "broker", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapNpc { MapNpcId = Broker, MapId = Town, MapX = 76, MapY = 118, VNum = BrokerVNum, Dialog = BrokerDialog });
            await context.SaveChangesAsync();
            await SeedMarketAsync(context);
            await SeedFamilyManagerAsync(context);
            await SeedArchaeologistAsync(context);
            await SeedGamblerAsync(context);
            await SeedRaidAsync(context);
            await SeedInstantCombatAsync(context);
            await SeedSampleTimeSpaceAsync(context);
            SeedSoundFlower(context);
            await SeedAct4Async(context);
            SeedDignity(context);
            SeedPreyQuest(context);
            await SeedScoreKeeperAsync(context);
            await context.SaveChangesAsync();
        }

        private static async Task SeedMarketAsync(NosCoreContext context)
        {
            var market = OpenMap(Market, 100, 100);
            market.ShopAllowed = true;
            context.Add(market);
            context.Add(new Portal
            {
                SourceMapId = Market, SourceX = MarketPortalX, SourceY = MarketPortalY,
                DestinationMapId = Town, DestinationX = 60, DestinationY = 100,
                Type = PortalType.Open
            });
            context.Add(new Item
            {
                VNum = Gem, Name = "gem", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, Price = GemPrice,
                IsDroppable = true, IsSoldable = true, IsTradable = true
            });
            context.Add(new Item
            {
                VNum = Token, Name = "token", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, ReputPrice = TokenReputation,
                IsDroppable = true, IsSoldable = true, IsTradable = true
            });
            context.Add(new Item
            {
                VNum = Keepsake, Name = "keepsake", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, Price = 100, IsDroppable = true
            });
            context.Add(new Item
            {
                VNum = Trinket, Name = "trinket", Type = NoscorePocketType.Etc, ItemType = ItemType.Main, Price = TrinketPrice,
                IsDroppable = true, IsSoldable = true, IsTradable = true
            });
            context.Add(new NpcMonster { NpcMonsterVNum = GrocerVNum, Name = "grocer", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapNpc { MapNpcId = Grocer, MapId = Market, MapX = GrocerX, MapY = GrocerY, VNum = GrocerVNum });
            var shelves = new Shop { MapNpcId = Grocer, MenuType = 0, ShopType = 0 };
            context.Add(shelves);
            await context.SaveChangesAsync();
            context.Add(new ShopItem { ShopId = shelves.ShopId, ItemVNum = Seed, Slot = 0, Type = 0 });
            context.Add(new ShopItem { ShopId = shelves.ShopId, ItemVNum = Blade, Slot = 1, Type = 0 });
            context.Add(new ShopItem { ShopId = shelves.ShopId, ItemVNum = Token, Slot = 2, Type = 0 });
            context.Add(new ShopItem { ShopId = shelves.ShopId, ItemVNum = Gem, Slot = 3, Type = 0 });
            context.Add(new ShopItem { ShopId = shelves.ShopId, ItemVNum = Trinket, Slot = 4, Type = 1 });
            await context.SaveChangesAsync();

            context.Add(new NpcMonster { NpcMonsterVNum = TeacherVNum, Name = "teacher", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapNpc { MapNpcId = Teacher, MapId = Market, MapX = TeacherX, MapY = TeacherY, VNum = TeacherVNum });
            var lessons = new Shop { MapNpcId = Teacher, MenuType = 0, ShopType = 0 };
            context.Add(lessons);
            await context.SaveChangesAsync();
            context.Add(new ShopSkill { ShopId = lessons.ShopId, SkillVNum = TaughtSkill, Slot = 0, Type = 0 });
            await context.SaveChangesAsync();
        }

        private static async Task SeedInstantCombatAsync(NosCoreContext context)
        {
            context.Add(OpenMap(InstantCombatArena, 75, 75));
            context.Add(new NpcMonster { NpcMonsterVNum = InstantCombatWaveVNum, Name = "wavemonster", Level = 1, MaxHp = 100000 });
            await context.SaveChangesAsync();
            for (byte wave = 0; wave <= 4; wave++)
            {
                context.Add(new InstantCombatMonster
                {
                    InstantCombatMonsterId = (short)(wave + 1), LevelMinimum = 1, Wave = wave, NpcMonsterVNum = InstantCombatWaveVNum, Amount = 1
                });
            }

            context.Add(new Item { VNum = InstantCombatPrize, Name = "icprize", Type = NoscorePocketType.Etc, ItemType = ItemType.Main });
            context.Add(new InstantCombatDrop
            {
                InstantCombatDropId = 9600, LevelMinimum = 1, Wave = 0, ItemVNum = InstantCombatPrize, Amount = InstantCombatPrizeAmount
            });

            await context.SaveChangesAsync();
        }

        private static async Task SeedRaidAsync(NosCoreContext context)
        {
            context.Add(OpenMap(JellyEntrance, 60, 60));
            context.Add(OpenMap(JellySquare, 60, 60));
            context.Add(new Item
            {
                VNum = RaidSeal, Name = "raidseal", Type = NoscorePocketType.Main, ItemType = ItemType.Special, Effect = ItemEffectType.RaidSeal
            });
            context.Add(new Item { VNum = RaidReward, Name = "raidreward", Type = NoscorePocketType.Etc, ItemType = ItemType.Main });
            context.Add(new NpcMonster { NpcMonsterVNum = 282, Name = "mothercuby", Level = 40, MaxHp = 1 });
            foreach (short vnum in new short[] { 283, 167, 168, 169, 170 })
            {
                context.Add(new NpcMonster { NpcMonsterVNum = vnum, Name = "jelly" + vnum, Level = 30, MaxHp = 100000 });
            }

            await context.SaveChangesAsync();
            context.Add(new ScriptedInstance
            {
                MapId = Meadow, PositionX = StoneX, PositionY = StoneY, Type = ScriptedInstanceType.TimeSpace,
                TimeSpaceId = StoneTimeSpace, LevelMinimum = 1, LevelMaximum = 99
            });
            context.Add(new ScriptedInstance
            {
                MapId = JellyEntrance, PositionX = RaidGateX, PositionY = RaidGateY, Type = ScriptedInstanceType.Raid,
                Script = "raid-mother-cuby", LevelMinimum = 30, LevelMaximum = 49
            });
            context.Add(new FamilyQuest
            {
                QuestId = RaidMissionId, Kind = FamilyQuestKind.Mission, Objective = FamilyQuestObjective.Raid, Parameter = 0,
                Target = 10, Experience = 100, ResetsDaily = true, OncePerMemberPerDay = true
            });
            await context.SaveChangesAsync();
        }

        private static void SeedSoundFlower(NosCoreContext context)
        {
            context.Add(new Item
            {
                VNum = WildSoundFlower, Name = "soundflower", Type = NoscorePocketType.Main, ItemType = ItemType.Map, Effect = ItemEffectType.SoundFlower,
                IsDroppable = true
            });
            context.Add(new Quest
            {
                QuestId = FlowerQuest, QuestType = QuestType.FlowerQuest, LevelMin = FlowerQuestLevel, LevelMax = 99,
                QuestLineId = FlowerQuest, Title = "flowerquest", Desc = "flowerquest"
            });
        }

        // The sample's monsters stand still and fall to one blow.
        private static async Task SeedSampleTimeSpaceAsync(NosCoreContext context)
        {
            context.Add(OpenMap(SampleRoomMap, 60, 60));
            foreach (short vnum in new short[] { 58, 59, 60 })
            {
                context.Add(new NpcMonster { NpcMonsterVNum = vnum, Name = "sample" + vnum, Level = 1, MaxHp = 1 });
            }

            await context.SaveChangesAsync();
            context.Add(new ScriptedInstance
            {
                MapId = Meadow, PositionX = SampleTimeSpaceX, PositionY = SampleTimeSpaceY, Type = ScriptedInstanceType.TimeSpace,
                Script = "timespace-sample", LevelMinimum = 1, LevelMaximum = 99
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedAct4Async(NosCoreContext context)
        {
            context.Add(OpenMap(AngelCitadel, 60, 60));
            context.Add(OpenMap(DemonCitadel, 60, 60));
            context.Add(OpenMap(ShipDeck, 60, 60));
            context.Add(new MapType { MapTypeId = MapTypeType.Act4, MapTypeName = "act4" });
            foreach (short vnum in new short[] { 540, 613 })
            {
                context.Add(new NpcMonster { NpcMonsterVNum = vnum, Name = "crew" + vnum, Level = 1, MaxHp = 100 });
            }

            context.Add(new NpcTalk { DialogId = ShipmasterDialog, Name = "shipmaster" });
            context.Add(new NpcMonster { NpcMonsterVNum = ShipmasterVNum, Name = "shipmaster", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapTypeMap { MapId = AngelCitadel, MapTypeId = MapTypeType.Act4 });
            context.Add(new MapTypeMap { MapId = DemonCitadel, MapTypeId = MapTypeType.Act4 });
            context.Add(new MapNpc
            {
                MapNpcId = Shipmaster, MapId = Town, MapX = ShipmasterX, MapY = ShipmasterY, VNum = ShipmasterVNum, Dialog = ShipmasterDialog
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedGamblerAsync(NosCoreContext context)
        {
            context.Add(new Item
            {
                VNum = CellaPowder, Name = "cellapowder", Type = NoscorePocketType.Etc, ItemType = ItemType.Main,
                Material = MaterialType.CellaPowder
            });
            context.Add(new Item
            {
                VNum = BlessingAmulet, Name = "blessingamulet", Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Amulet, Effect = ItemEffectType.RedAmulet
            });
            context.Add(new Item
            {
                VNum = ChampionBlade, Name = "championblade", Type = NoscorePocketType.Equipment, ItemType = ItemType.Weapon,
                EquipmentSlot = EquipmentType.MainWeapon, IsHeroic = true
            });
            context.Add(new Item
            {
                VNum = EventAmulet, Name = "eventamulet", Type = NoscorePocketType.Equipment, ItemType = ItemType.Jewelery,
                EquipmentSlot = EquipmentType.Amulet, Effect = ItemEffectType.RedAmulet, EffectValue = EventAmuletUses,
                OnlyForBoundItems = true
            });
            context.Add(new NpcTalk { DialogId = GamblerDialog, Name = "gambler" });
            context.Add(new NpcMonster { NpcMonsterVNum = GamblerVNum, Name = "gambler", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new MapNpc
            {
                MapNpcId = Gambler, MapId = Town, MapX = GamblerX, MapY = GamblerY, VNum = GamblerVNum, Dialog = GamblerDialog
            });
            context.Add(new Shop { MapNpcId = Gambler, MenuType = 0, ShopType = GamblingShop });
            context.Add(new ShopTypeService { ShopTypeServiceId = 3, ShopType = GamblingShop, Service = NpcService.EquipmentRarify });
            await context.SaveChangesAsync();
        }

        // The bands of dignity as the client names them, each by the highest value still in it.
        private static void SeedDignity(NosCoreContext context)
        {
            short?[] ceilings = [100, -100, -201, -401, -601, -801];
            for (byte level = 1; level <= ceilings.Length; level++)
            {
                context.Add(new DignityLevel { DignityLevelId = level, MaxDignity = ceilings[level - 1] });
            }
        }

        private static void SeedPreyQuest(NosCoreContext context)
        {
            context.Add(new Quest
            {
                QuestId = PreyQuest, QuestType = QuestType.Hunt, LevelMin = 1, LevelMax = 99,
                QuestLineId = PreyQuest, Title = "preyquest", Desc = "preyquest"
            });
            context.Add(new QuestObjective { QuestObjectiveId = Guid.NewGuid(), QuestId = PreyQuest, FirstData = PreyVNum, SecondData = 1 });
        }

        private static async Task SeedScoreKeeperAsync(NosCoreContext context)
        {
            context.Add(new NpcTalk { DialogId = ScoreKeeperDialog, Name = "scorekeeper" });
            context.Add(new NpcMonster { NpcMonsterVNum = ScoreKeeperVNum, Name = "scorekeeper", Level = 1, MaxHp = 100 });
            context.Add(new Quest
            {
                QuestId = AnyLevelScoreQuest, QuestType = QuestType.TsPoint, LevelMin = 1, LevelMax = 99,
                QuestLineId = AnyLevelScoreQuest, OfferedBy = NrunRunnerType.GetQuestHigherTimeSpaceScore, OfferOption = 0,
                Title = "anylevelscorequest", Desc = "anylevelscorequest"
            });
            context.Add(new Quest
            {
                QuestId = HighLevelScoreQuest, QuestType = QuestType.TsPoint, LevelMin = HighLevelScoreQuestLevel, LevelMax = 99,
                QuestLineId = HighLevelScoreQuest, OfferedBy = NrunRunnerType.GetQuestHigherTimeSpaceScore, OfferOption = 1,
                Title = "highlevelscorequest", Desc = "highlevelscorequest"
            });
            await context.SaveChangesAsync();
            context.Add(new MapNpc
            {
                MapNpcId = ScoreKeeper, MapId = Town, MapX = ScoreKeeperX, MapY = ScoreKeeperY, VNum = ScoreKeeperVNum,
                Dialog = ScoreKeeperDialog
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedFamilyManagerAsync(NosCoreContext context)
        {
            context.Add(new Item
            {
                VNum = FamilyWarehouse, Name = "familywarehouse", Type = NoscorePocketType.Etc, ItemType = ItemType.Main,
                Price = FamilyWarehousePrice
            });
            context.Add(new NpcMonster { NpcMonsterVNum = FamilyManagerVNum, Name = "familymanager", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new FamilyUpgrade
            {
                ItemVNum = FamilyWarehouse, FamilyLevelMinimum = FamilyWarehouseLevel, UpgradeType = FamilyUpgradeType.Warehouse
            });
            context.Add(new MapNpc { MapNpcId = FamilyManager, MapId = Town, MapX = 76, MapY = 108, VNum = FamilyManagerVNum });
            context.Add(new Shop { MapNpcId = FamilyManager, MenuType = 0, ShopType = FamilyShop });
            context.Add(new ShopTypeService { ShopTypeServiceId = 1, ShopType = FamilyShop, Service = NpcService.FamilyManager });
            await context.SaveChangesAsync();
        }

        private static async Task SeedArchaeologistAsync(NosCoreContext context)
        {
            context.Add(new Item
            {
                VNum = Relict, Name = "relict", Type = NoscorePocketType.Main, ItemType = ItemType.Main,
                Effect = ItemEffectType.Relict, EffectValue = RelictFee
            });
            context.Add(new Item { VNum = RelictPiece, Name = "relictpiece", Type = NoscorePocketType.Etc, ItemType = ItemType.Main });
            context.Add(new NpcTalk { DialogId = ArchaeologistDialog, Name = "archaeologist" });
            context.Add(new NpcMonster { NpcMonsterVNum = ArchaeologistVNum, Name = "archaeologist", Level = 1, MaxHp = 100 });
            await context.SaveChangesAsync();
            context.Add(new RollGeneratedItem
            {
                OriginalItemVNum = Relict, ItemGeneratedVNum = RelictPiece, ItemGeneratedAmount = 1, Probability = 1
            });
            context.Add(new MapNpc
            {
                MapNpcId = Archaeologist, MapId = Town, MapX = ArchaeologistX, MapY = ArchaeologistY, VNum = ArchaeologistVNum,
                Dialog = ArchaeologistDialog
            });
            context.Add(new Shop { MapNpcId = Archaeologist, MenuType = 0, ShopType = ArchaeologistShop });
            context.Add(new ShopTypeService { ShopTypeServiceId = 2, ShopType = ArchaeologistShop, Service = NpcService.RelictExamination });
            await context.SaveChangesAsync();
        }

        private static Item Worn(short vnum, EquipmentType slot, ItemType type) => new()
        {
            VNum = vnum, Name = "item" + vnum, Type = NoscorePocketType.Equipment, ItemType = type, EquipmentSlot = slot
        };

        // What a character carries has to be there before it is selected: the bag is read then.
        public static Task GiveAsync(NosCoreContext context, long characterId, short vnum, short amount, short slot,
            NoscorePocketType pocket = NoscorePocketType.Etc) =>
            GiveAsync(context, characterId, new ItemInstance { ItemVNum = vnum, Amount = amount }, slot, pocket);

        public static async Task GiveAsync(NosCoreContext context, long characterId, ItemInstance item, short slot, NoscorePocketType pocket)
        {
            item.Id = Guid.NewGuid();
            item.CharacterId = characterId;
            context.Add(item);
            context.Add(new InventoryItemInstance
            {
                Id = Guid.NewGuid(), CharacterId = characterId, ItemInstanceId = item.Id, Slot = slot, Type = pocket
            });
            await context.SaveChangesAsync();
        }

        // A time-space cleared on an earlier session. It has to be recorded before the character is selected.
        public static async Task CompleteTimeSpaceAsync(NosCoreContext context, long characterId, short timeSpaceId)
        {
            context.Add(new CompletedTimeSpace { Id = Guid.NewGuid(), CharacterId = characterId, TimeSpaceId = timeSpaceId });
            await context.SaveChangesAsync();
        }

        public static async Task GiveQuestAsync(NosCoreContext context, long characterId, short questId)
        {
            context.Add(new CharacterQuest { Id = Guid.NewGuid(), CharacterId = characterId, QuestId = questId });
            await context.SaveChangesAsync();
        }

        // A skill has to be known before the character is selected, like what it carries.
        public static async Task TeachAsync(NosCoreContext context, long characterId, short skillVNum)
        {
            context.Add(new CharacterSkill { Id = Guid.NewGuid(), CharacterId = characterId, SkillVNum = skillVNum });
            await context.SaveChangesAsync();
        }

        // A buff that outlasts a logout is put back when the character is selected.
        public static async Task GiveBuffAsync(NosCoreContext context, long characterId, short cardId, int seconds)
        {
            context.Add(new StaticBuff { CharacterId = characterId, CardId = cardId, RemainingTime = seconds });
            await context.SaveChangesAsync();
        }

        // A pet has to be owned before the character is selected, like what it carries. It comes into the
        // world in the team, at its owner's side, and as full as it can be.
        public static async Task GivePetAsync(NosCoreContext context, long characterId, short vnum)
        {
            context.Add(new Mate
            {
                CharacterId = characterId, VNum = vnum, MateType = MateType.Pet, Level = 1, Loyalty = 1000, Hp = short.MaxValue,
                Mp = short.MaxValue, IsTeamMember = true, IsSummonable = true
            });
            await context.SaveChangesAsync();
        }

        // A partner in the team wearing the partner card. It has to be owned before the character is selected.
        public static async Task GivePartnerAsync(NosCoreContext context, long characterId)
        {
            context.Add(new Mate
            {
                CharacterId = characterId, VNum = PartnerVNum, MateType = MateType.Partner, Level = 1, Loyalty = 1000, Hp = short.MaxValue,
                Mp = short.MaxValue, IsTeamMember = true, IsSummonable = true
            });
            await context.SaveChangesAsync();
            await GiveAsync(context, characterId, new SpecialistInstance { ItemVNum = PartnerCard, Amount = 1 }, (short)EquipmentType.Sp,
                NoscorePocketType.FirstPartnerInventory);
        }

        // An account of its own for every character, so that scenarios never meet on one.
        public static async Task<Player> AddPlayerAsync(NosCoreContext context, string name, short mapId, short x, short y,
            Action<Character>? configure = null, AuthorityType authority = AuthorityType.User)
        {
            var account = new Account
            {
                Name = name.ToLowerInvariant(),
                Password = Convert.ToHexString(SHA512.HashData(Encoding.Default.GetBytes(Password))).ToLowerInvariant(),
                Authority = authority,
                Language = RegionType.EN
            };
            context.Add(account);
            await context.SaveChangesAsync();

            var character = new Character
            {
                AccountId = account.AccountId,
                Name = name,
                Slot = 0,
                MapId = mapId,
                MapX = x,
                MapY = y,
                Level = 1,
                JobLevel = 1,
                Hp = 221,
                Mp = 60,
                State = CharacterState.Active
            };
            configure?.Invoke(character);
            context.Add(character);
            await context.SaveChangesAsync();
            foreach (var skill in new short[] { 200, 201, 209 })
            {
                context.Add(new CharacterSkill { Id = Guid.NewGuid(), CharacterId = character.CharacterId, SkillVNum = skill });
            }

            await context.SaveChangesAsync();
            return new Player(account.Name, name, character.CharacterId, character.Slot);
        }

        public static async Task<FoundedFamily> AddFamilyAsync(NosCoreContext context, string name, Action<Family>? configure,
            Player head, params Player[] members)
        {
            var family = new Family { Name = name, FamilyLevel = 1, MaxSize = FamilySize, FamilyMessage = string.Empty };
            configure?.Invoke(family);
            context.Add(family);
            await context.SaveChangesAsync();
            context.Add(new FamilyCharacter
            {
                FamilyId = family.FamilyId, CharacterId = head.CharacterId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            foreach (var member in members)
            {
                context.Add(new FamilyCharacter
                {
                    FamilyId = family.FamilyId, CharacterId = member.CharacterId, Authority = FamilyAuthority.Member,
                    DailyMessage = string.Empty
                });
            }

            await context.SaveChangesAsync();
            return new FoundedFamily(family.FamilyId, name);
        }

        // Width and height, then one byte a cell; 0 is a cell that can be walked on.
        private static Map OpenMap(short mapId, short width, short height,
            NosCore.Packets.Enumerations.MapType mapType = NosCore.Packets.Enumerations.MapType.Normal)
        {
            var data = new byte[4 + width * height];
            BitConverter.TryWriteBytes(data.AsSpan(0, 2), width);
            BitConverter.TryWriteBytes(data.AsSpan(2, 2), height);
            return new Map { MapId = mapId, Data = data, Name = "map" + mapId, MapType = mapType };
        }
    }

    public sealed record Player(string Account, string Name, long CharacterId, byte Slot);

    public sealed record FoundedFamily(long FamilyId, string Name);
}
