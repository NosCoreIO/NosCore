using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

public record struct EntityIdentityComponent(long VisualId, VisualType VisualType, long CharacterId);
