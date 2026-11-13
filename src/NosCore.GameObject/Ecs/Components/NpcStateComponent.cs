using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ProductionService;
using NosCore.GameObject.Services.ShopService;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace NosCore.GameObject.Ecs.Components;

public record struct NpcStateComponent(
    NpcMonsterDto NpcMonster,
    MapInstance MapInstance,
    Shop? Shop,
    Dictionary<int, Shop>? Shops,
    IReadOnlyList<Recipe>? Recipes,
    Dictionary<Type, Subject<RequestData>> Requests,
    short? Dialog,
    bool IsDisabled
);
