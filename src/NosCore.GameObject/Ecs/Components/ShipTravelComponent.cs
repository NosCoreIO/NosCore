using System;
using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

public record struct ShipTravelComponent(Guid? BoardedShipId, Instant ShipDepartureAt);
