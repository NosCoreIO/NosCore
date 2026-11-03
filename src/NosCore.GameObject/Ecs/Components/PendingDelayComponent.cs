using NodaTime;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

public record struct PendingDelayComponent(DelayPacketType? PendingDelayType, Instant PendingDelayReadyAt);
