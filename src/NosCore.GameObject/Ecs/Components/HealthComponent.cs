namespace NosCore.GameObject.Ecs.Components;

public record struct HealthComponent(int Hp, int MaxHp, bool IsAlive);
