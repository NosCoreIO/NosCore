//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService.Model;

// Snapshot of combat-relevant stats resolved for a single hit. Pulling these into a
// flat struct lets the damage calculator stay pure and easy to test.
//
// Matches OpenNos Character/Monster stat shape so the damage formula port can use these
// fields directly without introspecting the underlying entity.
public readonly record struct CombatStats(
    int Level,
    int HeroLevel,
    CharacterClassType Class,
    int Morale,
    // Melee profile
    int MinHit,
    int MaxHit,
    int HitRate,
    int CriticalChance,
    int CriticalRate,
    int MeleeUpgrade,
    // Ranged profile
    int MinDistance,
    int MaxDistance,
    int DistanceRate,
    int DistanceCriticalChance,
    int DistanceCriticalRate,
    int RangedUpgrade,
    // Element
    byte Element,
    int ElementRate,
    int ElementRateSp,
    // Defence
    int Defence,
    int DefenceRate,
    int DistanceDefence,
    int DistanceDefenceRate,
    int MagicDefence,
    int DefenceDodge,
    int DistanceDefenceDodge,
    int DefenceUpgrade,
    // Resistances (per-element % reduction of incoming element damage)
    int FireResistance,
    int WaterResistance,
    int LightResistance,
    int DarkResistance,
    // Type 14: what the ATTACKER does to the defender's resistances. Carried by the attacker and
    // read against the defender's own, so a "reduces the enemy's fire resistance by 20" arrives
    // here as -20 and the damage step adds the two together.
    int EnemyFireResistance = 0,
    int EnemyWaterResistance = 0,
    int EnemyLightResistance = 0,
    int EnemyDarkResistance = 0,
    // Type 16: overrides of the ordinary dodge roll, both as a percentage.
    int GuaranteedHitChance = 0,
    int GuaranteedDodgeChance = 0);
