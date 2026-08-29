using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;

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
    typeof(NpcStateComponent),
    typeof(BuffStateComponent),
    typeof(AggroComponent),
    typeof(SkillCooldownComponent),
    typeof(MateStateComponent)
)]
public readonly partial struct MateComponentBundle : INonPlayableEntity
{
    public Arch.Core.Entity Handle => Entity;

    public short MapX => PositionX;
    public short MapY => PositionY;
}
