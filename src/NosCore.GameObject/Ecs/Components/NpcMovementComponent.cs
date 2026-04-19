namespace NosCore.GameObject.Ecs.Components;

public record struct NpcMovementComponent(short FirstX, short FirstY, bool IsMoving, bool IsHostile);
