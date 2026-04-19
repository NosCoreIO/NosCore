using System;
using NodaTime;
using NosCore.GameObject.Services.ItemGenerationService.Item;

namespace NosCore.GameObject.Ecs.Components;

public record struct MapItemDataComponent(short VNum, short Amount, long? OwnerId, Instant DroppedAt, Guid ItemInstanceId, IItemInstance? ItemInstance);
