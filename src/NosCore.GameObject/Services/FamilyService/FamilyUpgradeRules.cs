//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using System;

namespace NosCore.GameObject.Services.FamilyService
{
    // A family stores what an upgrade line gives it (WarehouseSize, MaxSize), so how far it went along a
    // line is read back from that capacity.
    public static class FamilyUpgradeRules
    {
        public static bool IsOwned(this FamilyDto family, FamilyUpgradeDto upgrade, IFamilyUpgradeService capacities) =>
            family.Capacity(upgrade.UpgradeType) >= capacities.Capacity(upgrade.UpgradeType, (byte)(upgrade.UpgradeLevel + 1));

        public static bool HasPrevious(this FamilyDto family, FamilyUpgradeDto upgrade, IFamilyUpgradeService capacities) =>
            upgrade.UpgradeLevel == 0
            || family.Capacity(upgrade.UpgradeType) >= capacities.Capacity(upgrade.UpgradeType, upgrade.UpgradeLevel);

        public static FamilyUpgradeState StateOf(this FamilyDto family, FamilyUpgradeDto upgrade, IFamilyUpgradeService capacities)
        {
            if (family.IsOwned(upgrade, capacities))
            {
                return FamilyUpgradeState.Purchased;
            }

            return family.FamilyLevel >= upgrade.FamilyLevelMinimum && family.HasPrevious(upgrade, capacities)
                ? FamilyUpgradeState.Purchasable
                : FamilyUpgradeState.Unavailable;
        }

        public static void Apply(this FamilyDto family, FamilyUpgradeDto upgrade, IFamilyUpgradeService capacities)
        {
            var capacity = capacities.Capacity(upgrade.UpgradeType, (byte)(upgrade.UpgradeLevel + 1));
            switch (upgrade.UpgradeType)
            {
                case FamilyUpgradeType.Warehouse:
                    family.WarehouseSize = capacity;
                    break;
                case FamilyUpgradeType.MemberLimit:
                    family.MaxSize = capacity;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade.UpgradeType, null);
            }
        }

        private static byte Capacity(this FamilyDto family, FamilyUpgradeType type) => type switch
        {
            FamilyUpgradeType.Warehouse => family.WarehouseSize,
            FamilyUpgradeType.MemberLimit => family.MaxSize,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private static byte Capacity(this IFamilyUpgradeService capacities, FamilyUpgradeType type, byte level) => type switch
        {
            FamilyUpgradeType.Warehouse => capacities.GetWarehouseSize(level),
            FamilyUpgradeType.MemberLimit => capacities.GetMemberLimit(level),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
