using NosCore.GameObject.Ecs.Attributes;
using NosCore.GameObject.Ecs.Components;

namespace NosCore.GameObject.Ecs;

[ComponentBundle(
    typeof(EntityIdentityComponent),
    typeof(HealthComponent),
    typeof(ManaComponent),
    typeof(PositionComponent),
    typeof(VisualComponent),
    typeof(AppearanceComponent),
    typeof(ExperienceComponent),
    typeof(GoldComponent),
    typeof(ReputationComponent),
    typeof(SpComponent),
    typeof(NameComponent),
    typeof(CombatComponent),
    typeof(PlayerComponent),
    typeof(PlayerFlagsComponent),
    typeof(TimingComponent),
    typeof(SpeedComponent),
    typeof(PlayerStateComponent)
)]
public ref partial struct PlayerComponentBundle
{
    public long CharacterId => PlayerCharacterId;
    public bool InExchangeOrShop => InShop || InExchange;
}
