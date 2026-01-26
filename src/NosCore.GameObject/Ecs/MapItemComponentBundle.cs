using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs;

[ComponentBundle(
    typeof(EntityIdentityComponent),
    typeof(PositionComponent),
    typeof(MapItemDataComponent)
)]
public ref partial struct MapItemComponentBundle
{
}
