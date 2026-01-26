using System;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public record struct MapItemDataComponent(short VNum, short Amount, long? OwnerId, Instant DroppedAt, Guid ItemInstanceId);
