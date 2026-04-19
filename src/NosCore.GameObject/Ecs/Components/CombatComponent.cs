using Arch.Core;
using System.Collections.Concurrent;
using System.Threading;

namespace NosCore.GameObject.Ecs.Components;

public record struct CombatComponent(
    int HitRate,
    int CriticalChance,
    int CriticalRate,
    int MinHit,
    int MaxHit,
    int MinDistance,
    int MaxDistance,
    int DistanceCriticalChance,
    int DistanceCriticalRate,
    int DistanceRate,
    int FireResistance,
    int WaterResistance,
    int LightResistance,
    int DarkResistance,
    int Defence,
    int DefenceRate,
    int DistanceDefence,
    int DistanceDefenceRate,
    int MagicDefence,
    int Element,
    int ElementRate,
    SemaphoreSlim HitSemaphore,
    ConcurrentDictionary<Entity, int> HitList);
