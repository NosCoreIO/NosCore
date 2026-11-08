namespace NosCore.GameObject.Ecs.Components;

// Walking time the character has used that the time since its last accepted move has not yet
// covered.
public record struct WalkAllowanceComponent(double WalkMillisecondsOwed);
