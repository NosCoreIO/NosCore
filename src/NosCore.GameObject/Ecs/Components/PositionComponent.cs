using System;

namespace NosCore.GameObject.Ecs.Components;

public record struct PositionComponent(short PositionX, short PositionY, byte Direction, Guid MapInstanceId);
