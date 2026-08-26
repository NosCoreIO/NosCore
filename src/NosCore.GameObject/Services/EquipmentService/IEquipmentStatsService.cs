//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.EquipmentService;

public interface IEquipmentStatsService
{
    /// <summary>The sum of the worn equipment, or zeros if nothing is worn.</summary>
    EquipmentStats Resolve(IAliveEntity entity);
}

public readonly record struct EquipmentStats(
    int MinHit,
    int MaxHit,
    int HitRate,
    int CriticalChance,
    int CriticalRate,
    int MainWeaponUpgrade,
    int MinDistance,
    int MaxDistance,
    int DistanceRate,
    int DistanceCriticalChance,
    int DistanceCriticalRate,
    int SecondaryWeaponUpgrade,
    int CloseDefence,
    int DistanceDefence,
    int MagicDefence,
    int DefenceDodge,
    int DistanceDefenceDodge,
    int ArmourUpgrade,
    int ElementRate,
    int FireResistance,
    int WaterResistance,
    int LightResistance,
    int DarkResistance,
    // Raise the entity's maximum rather than feeding CombatStats, and nothing consumes them
    // yet — max HP is still class and level alone.
    int Hp,
    int Mp,
    IReadOnlyList<BCardDto> BCards)
{
    public static EquipmentStats None => new() { BCards = System.Array.Empty<BCardDto>() };
}
