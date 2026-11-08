using NodaTime;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

// Target is whatever the bar's follow-up acts on - an item, a friend, a mate - compared by value.
public record struct PendingDelayComponent(DelayPacketType? PendingDelayType, Instant PendingDelayReadyAt, object? PendingDelayTarget);
