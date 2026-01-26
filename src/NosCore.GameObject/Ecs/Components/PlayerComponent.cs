namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerComponent(long AccountId, long CharacterId, bool IsGm);
