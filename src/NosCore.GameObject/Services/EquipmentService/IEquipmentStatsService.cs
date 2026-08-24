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
/// Before this service <b>the equipment counted for nothing</b>: NosCore has a
/// <c>CombatComponent</c> meant to hold these values, but nobody ever wrote
/// into it - it starts at zero and stays at zero. The comment in the code said "once populated by the
/// inventory/equipment system», e quel sistema non esisteva.
///
/// The effect in game was that weapon and armour were decoration: the damage came only from the
/// tabelle di classe e livello, identico a mani nude.
/// </summary>
public interface IEquipmentStatsService
{
    /// <summary>
    /// The sum of the worn equipment. Returns zeros if there is no inventory or no
    /// niente addosso.
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
    /// <c>CombatStats</c>: they change the entity's ceiling instead, and it is
    /// <c>VitalityService</c> that writes it. The field already existed on the item (<c>Item.Hp</c>,
    /// <c>Item.Mp</c>, read by the parser) and nobody read it.
    /// </summary>
    int Hp,
    int Mp,
    /// <summary>
    /// The effects the worn pieces declare.
    ///
    /// They are a different thing from the flat stats above: an armour carries its defence in
    /// a field, but it can also carry an effect - "chance of causing poisoning",
    /// "fire resistance increased". In the sibling codebase they are the `StaticBcards`; here nobody read
    /// nessuno.
    /// </summary>
    IReadOnlyList<BCardDto> BCards)
{
    public static EquipmentStats None => new() { BCards = System.Array.Empty<BCardDto>() };
}
