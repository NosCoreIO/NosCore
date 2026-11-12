//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Items
{
    // What an upgrade or another paid action consumes. Item.dat does not say which item is which, so the parser sets it.
    public enum MaterialType : byte
    {
        None = 0,
        CellaPowder = 1,
        SoulGem = 2,
        CompleteSoulGem = 3,
        DimensionalForce = 4,
        DimensiolithShard = 5,
        EquipmentProtectionScroll = 6,
        GoldenEquipmentProtectionScroll = 7,
        AngelsFeather = 8,
        FullMoonCrystal = 9,
        GreenSoul = 10,
        RedSoul = 11,
        BlueSoul = 12,
        DragonSkin = 13,
        DragonBlood = 14,
        DragonHeart = 15,
        DragonGem = 16,
        LowerSpProtectionScroll = 17,
        HigherSpProtectionScroll = 18,
        RiverSand = 19,
        SmallRubyOfCompletion = 20,
        SmallSapphireOfCompletion = 21,
        SmallObsidianOfCompletion = 22,
        SmallTopazOfCompletion = 23,
        RubyOfCompletion = 24,
        SapphireOfCompletion = 25,
        ObsidianOfCompletion = 26,
        TopazOfCompletion = 27,
        SeedOfPower = 28,
    }
}
