namespace NosCore.GameObject.Ecs.Components;

public record struct ExperienceComponent(byte Level, long LevelXp, byte JobLevel, long JobLevelXp, byte HeroLevel, long HeroLevelXp);
