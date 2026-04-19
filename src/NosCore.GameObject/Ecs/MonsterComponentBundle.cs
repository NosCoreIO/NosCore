using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Entities.Interfaces;

namespace NosCore.GameObject.Ecs;

[ComponentBundle(
    typeof(EntityIdentityComponent),
    typeof(HealthComponent),
    typeof(ManaComponent),
    typeof(PositionComponent),
    typeof(VisualComponent),
    typeof(NpcDataComponent),
    typeof(SpawnComponent),
    typeof(EffectComponent),
    typeof(TimingComponent),
    typeof(NpcStateComponent)
)]
public readonly partial struct MonsterComponentBundle : INonPlayableEntity
{
    public short MapX => FirstX;
    public short MapY => FirstY;
}
