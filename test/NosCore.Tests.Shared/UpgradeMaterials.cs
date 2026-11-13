//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Moq;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Services.ItemGenerationService;

namespace NosCore.Tests.Shared
{
    // The upgrade materials as the parser marks them in the item data.
    public static class UpgradeMaterials
    {
        private static readonly IReadOnlyDictionary<MaterialType, short> VNums = new Dictionary<MaterialType, short>
        {
            [MaterialType.CellaPowder] = 1014,
            [MaterialType.SoulGem] = 1015,
            [MaterialType.CompleteSoulGem] = 1016,
            [MaterialType.DimensionalForce] = 2774,
            [MaterialType.DimensiolithShard] = 2748,
            [MaterialType.EquipmentProtectionScroll] = 1218,
            [MaterialType.GoldenEquipmentProtectionScroll] = 5369,
            [MaterialType.AngelsFeather] = 2282,
            [MaterialType.FullMoonCrystal] = 1030,
            [MaterialType.GreenSoul] = 2283,
            [MaterialType.RedSoul] = 2284,
            [MaterialType.BlueSoul] = 2285,
            [MaterialType.DragonSkin] = 2511,
            [MaterialType.DragonBlood] = 2512,
            [MaterialType.DragonHeart] = 2513,
            [MaterialType.DragonGem] = 2630,
            [MaterialType.LowerSpProtectionScroll] = 1363,
            [MaterialType.HigherSpProtectionScroll] = 1364,
            [MaterialType.RiverSand] = 1027,
            [MaterialType.SmallRubyOfCompletion] = 2514,
            [MaterialType.SmallSapphireOfCompletion] = 2515,
            [MaterialType.SmallObsidianOfCompletion] = 2516,
            [MaterialType.SmallTopazOfCompletion] = 2517,
            [MaterialType.RubyOfCompletion] = 2518,
            [MaterialType.SapphireOfCompletion] = 2519,
            [MaterialType.ObsidianOfCompletion] = 2520,
            [MaterialType.TopazOfCompletion] = 2521,
            [MaterialType.SeedOfPower] = 1012,
            [MaterialType.Saver] = 1211,
        };

        public static short VNum(MaterialType material) => VNums[material];

        public static IItemCatalog Catalog()
        {
            var catalog = new Mock<IItemCatalog>();
            catalog.Setup(c => c.GetMaterialVNum(It.IsAny<MaterialType>()))
                .Returns((MaterialType material) => VNums.GetValueOrDefault(material));
            return catalog.Object;
        }
    }
}
