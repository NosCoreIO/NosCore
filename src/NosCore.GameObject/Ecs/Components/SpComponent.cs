namespace NosCore.GameObject.Ecs.Components;

// IsDrainingPoints is what the client was last told about the drain, so only a change is sent.
public record struct SpComponent(short SpCooldown, int SpPoint, int SpAdditionPoint, bool IsDrainingPoints = false);
