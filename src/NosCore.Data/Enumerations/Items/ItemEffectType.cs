//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Items
{
    public enum ItemEffectType : ushort
    {
        NoEffect = 0,
        Teleport = 1,
        ApplyHairDie = 10,
        // On a Main item: refines a Gillion Stone. EffectValue is the RefinerType.
        Refiner = 10,
        ApplyHairStyle = 11,
        RestoreDignity = 14,
        Speaker = 15,
        // EffectValue is how many minutes the bubble stays up.
        SpeechBubble = 16,
        // EffectValue is the card the item puts on its user.
        ApplyCard = 20,
        // EffectValue is how many colours a wig can be dyed.
        WigDye = 30,
        // Dyes the hairstyle its HairStyleDye rows name; any other hairstyle refuses it.
        HairStyleDye = 31,
        MarriageProposal = 34,
        // EffectValue is the passive skill the book teaches.
        PassiveSkillBook = 99,
        // EffectValue is the time-space the item opens where its user stands.
        TimeSpaceStone = 140,
        MateCapture = 100,
        OpenProduction = 100,
        // A special item's EffectValue is the NPC the signpost puts up.
        MinilandSignpost = 100,
        Undefined = 69,
        // The map item that adds its amount to the gold counter when picked up.
        Gold = 70,
        MedalOfHonour = 69,
        Holder = 69,
        SpCharger = 71,
        DroppedSpRecharger = 150,
        PremiumSpRecharger = 151,
        CraftedSpRecharger = 152,
        InventoryUpgrade = 207,
        PetBasketUpgrade = 252,
        PermanentInventoryUpgrade = 601,
        PermanentPartnerBackpack = 602,
        PermanentPetBasket = 603,
        PermanentInventoryTicketUpgrade = 604,
        InventoryTicketUpgrade = 605,

        // EffectValue names the loa whose tattoos the pattern can ink.
        TattooPattern = 606,

        RenamingCard = 767,
        MedalOfErenia = 201,
        AdventurersMedal = 202,
        SelfIntroduction = 203,
        SpecialistMedal = 204,
        MedalOfFriendship = 205,
        MimisMedal = 206,
        AncelloansBlessing = 208,
        GuardianAngelsBlessing = 210,
        FairyBooster = 250,
        FriendshipWings = 199,
        // Brings the user's raid team to them while they lead it at a raid gate.
        TeamStone = 300,
        // Used on a weakened monster its ItemTargetMonster rows list, it kills that monster and gives
        // the item its EffectValue names.
        SuctionFunnel = 400,
        RaidSeal = 301,
        // A raid's reward box sold or handed out apart from the raid; EffectValue is that raid's number.
        SimpleRaidBox = 304,
        ApplySkinPartner = 305,
        // Morph is the look the skin gives a specialist card; EffectValue is the morph of the card it fits.
        SpecialistSkin = 306,
        // A special item's EffectValue is the NPC it puts up where its user stands, to cook at for a while.
        CookingStation = 550,
        ChangeSpecialistWings = 650,
        ChangeFaction = 570,
        ChangeGender = 651,
        PointInitialisation = 652,
        SealedTarotCard = 789,
        TarotCard = 790,
        // Blessing Amulet: a betting chance bonus on top of the protection.
        RedAmulet = 791,
        // Protection Amulet.
        BlueAmulet = 792,
        ReinforcementAmulet = 793,
        Heroic = 794,
        RandomHeroic = 795,
        // Carried while gambling, it raises the rarity by one instead of rolling.
        RarityIncreaseAmulet = 796,
        // The champion gear counterpart of RarityIncreaseAmulet.
        HeroicRarityIncreaseAmulet = 797,
        // The champion gear counterpart of BlueAmulet: protection without the chance bonus.
        HeroicProtectionAmulet = 798,
        // Rolls only among the sets the opener can wear; an opener no set fits rolls among them all.
        FamilyExperienceBooster = 825,
        ClassBox = 827,
        AttackAmulet = 932,
        DefenseAmulet = 933,
        // Fireworks and similar toys: show the item's EffectValue on the user.
        PlayEffect = 997,
        // A pack that opens into every item its roll rows list.
        ItemPack = 995,
        // EffectValue is the seconds of heat resistance one use adds.
        HeatResistance = 996,
        SpeedBooster = 998,
        BoxEffect = 999,
        Vehicle = 1000,
        // A special item: transforms into one of the monster looks its CostumeMorph rows list, and back on a second use.
        CostumeScroll = 1001,
        // A map item a raid collect quest counts when it is picked up; it never reaches the bag.
        RaidCollectQuestItem = 2001,
        // Releases one of the monsters its item's ItemSummon rows list, where the player stands.
        SealedVessel = 1002,
        GoldNosMerchantUpgrade = 1003,
        SilverNosMerchantUpgrade = 1004,
        PetSpaceUpgrade = 1006,
        PetBackpackUpgrade = 1008,
        CuarryBankVipMedal = 1010,
        CuarryBankSavingsBook = 1011,
        ShellIdentification = 1012,
        ShellOptionRemoval = 1013,
        SoulRevival = 1014,

        // The effect value of both is the upgrade window, as the client numbers it, that the scroll
        // opens and the card is upgraded in.
        EventSpUpgradeScroll = 1015,
        EventSpecialist = 1017,

        CostumeFusion = 1016,

        // EffectValue is the points one coupon adds: minigame durability for the first, miniland
        // production points for the second.
        MinigameDurabilityCoupon = 1018,
        ProductionCoupon = 1019,

        // EffectValue is how many cards one game deals.
        TarotCardGame = 1020,

        // Takes over an item bound to another player; the item's level and rarity set how many it takes.
        Perfume = 1021,

        // EffectValue is the reward level of the minigame box, 0 for the lowest.
        MinigameChest = 1022,

        // Lifts the fixed level a failed upgrade left on a piece of equipment.
        ReleaseScroll = 1023,

        // Gives one more entry to the Arena of Masters.
        ArenaOfMastersTicket = 1024,

        // Starts one of the flower quests, picked at random.
        SoundFlower = 1025,

        // Opened by an archaeologist rather than from the bag; EffectValue is the gold the examination costs.
        Relict = 1026,

        // Applies the item's own effects to its user, such as a bandage stopping bleedings.
        Bandage = 1027,

        // Builds the side's base camp where its user stands; EffectValue is the camp's monster.
        AngelBaseFlag = 1028,
        DemonBaseFlag = 1029,

        // Used on a skill in the partner window: puts that skill back to unlearned, or every skill for the
        // second, and refills the card's agility.
        PartnerSkillTicket = 1030,
        PartnerSkillTicketAll = 1031,

        BuffPotions = 6600,
        MarriageSeparation = 6969
    }
}
