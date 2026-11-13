using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Ecs.Components;

public record struct NpcStateComponent(
    NpcMonsterDto NpcMonster,
    MapInstance MapInstance,
    short? Dialog,
    bool IsDisabled
);
