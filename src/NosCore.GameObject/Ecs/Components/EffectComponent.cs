using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public record struct EffectComponent(short Effect, short EffectDelay, Instant LastPlayed = default);
