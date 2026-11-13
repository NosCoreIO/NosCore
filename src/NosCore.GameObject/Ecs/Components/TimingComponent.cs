using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public record struct TimingComponent(Instant LastMove, Instant LastAttack, Instant LastPotion, Instant LastRegen,
    Instant LastDamaged);
