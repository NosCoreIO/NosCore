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
        ApplyHairStyle = 11,
        RestoreDignity = 14,
        Speaker = 15,
        MarriageProposal = 34,
        // EffectValue is the passive skill the book teaches.
        PassiveSkillBook = 99,
        MateCapture = 100,
        OpenProduction = 100,
        Undefined = 69,
        // The map item that adds its amount to the gold counter when picked up.
        Gold = 70,
        MedalOfHonour = 69,
        Holder = 69,
        SpCharger = 71,
        DroppedSpRecharger = 150,
        PremiumSpRecharger = 151,
        CraftedSpRecharger = 152,
        SpecialistMedal = 204,
        AncelloansBlessing = 208,
        FairyBooster = 250,
        FriendshipWings = 199,
        ApplySkinPartner = 305,
        ChangeSpecialistWings = 650,
        ChangeFaction = 570,
        ChangeGender = 651,
        PointInitialisation = 652,
        SealedTarotCard = 789,
        TarotCard = 790,
        RedAmulet = 791,
        BlueAmulet = 792,
        ReinforcementAmulet = 793,
        Heroic = 794,
        RandomHeroic = 795,
        AttackAmulet = 932,
        DefenseAmulet = 933,
        // Fireworks and similar toys: show the item's EffectValue on the user.
        PlayEffect = 997,
        // EffectValue is the seconds of heat resistance one use adds.
        HeatResistance = 996,
        SpeedBooster = 998,
        BoxEffect = 999,
        Vehicle = 1000,
        GoldNosMerchantUpgrade = 1003,
        SilverNosMerchantUpgrade = 1004,
        InventoryUpgrade = 1005,
        PetSpaceUpgrade = 1006,
        PetBasketUpgrade = 1007,
        PetBackpackUpgrade = 1008,
        InventoryTicketUpgrade = 1009,
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

        BuffPotions = 6600,
        MarriageSeparation = 6969
    }
}
