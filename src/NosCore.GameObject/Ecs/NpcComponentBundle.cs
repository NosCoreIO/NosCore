using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs;

[ComponentBundle(
    typeof(EntityIdentityComponent),
    typeof(HealthComponent),
    typeof(ManaComponent),
    typeof(PositionComponent),
    typeof(VisualComponent),
    typeof(NpcDataComponent),
    typeof(SpawnComponent),
    typeof(TimingComponent)
)]
public ref partial struct NpcComponentBundle
{
}
