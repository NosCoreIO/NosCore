//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.EquipmentService;

/// <summary>
/// What the worn equipment adds to the combat stats.
///
/// Before this service the equipment counted for nothing: <c>CombatComponent</c> is meant to hold
/// these values and nobody ever wrote into it, so weapon and armour were decoration - the damage
/// came from the level and class tables alone, in full gear exactly as naked.
/// </summary>
public interface IEquipmentStatsService
{
    /// <summary>
    /// The sum of the worn equipment, or zeros if there is no inventory and nothing worn.
    /// </summary>
    EquipmentStats Resolve(IAliveEntity entity);
}

/// <summary>
/// The equipment's contribution, split by profile the way the game splits it: the main
/// weapon feeds melee, the secondary feeds range, the armour feeds the defences.
/// </summary>
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
    /// <summary>
    /// Maximum HP and MP added by the worn pieces.
    ///
    /// They live here and not among the combat stats because they do not end up in
    /// <c>CombatStats</c>: they raise the entity's maximum instead. Nothing consumes them yet -
    /// max HP is still computed from class and level alone, at login - and they are collected
    /// here because the parser has always read <c>Item.Hp</c> and <c>Item.Mp</c> and nobody
    /// ever looked at them.
    /// </summary>
    int Hp,
    int Mp,
    /// <summary>
    /// The effects the worn pieces declare - a different thing from the flat stats above: an
    /// armour carries its defence in a field, but it can also carry "chance of causing poisoning".
    /// The sibling codebase calls them <c>StaticBcards</c>.
    /// </summary>
    IReadOnlyList<BCardDto> BCards)
{
    public static EquipmentStats None => new() { BCards = System.Array.Empty<BCardDto>() };
}
