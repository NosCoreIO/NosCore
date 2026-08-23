using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Ecs;

// A mate on the map is a monster that belongs to somebody: it stands, takes hits, carries buffs
// and cooldowns, and dies. Giving it the monster's component set rather than a set of its own
// is what lets the battle service treat it as a combatant without a second notion of one.
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

    // A monster answers with the square it spawned on; a mate follows its owner, so the live
    // position is the only meaningful one.
    public short MapX => PositionX;
    public short MapY => PositionY;
}
