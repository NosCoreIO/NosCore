using Arch.Core;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.ShopService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;

namespace NosCore.GameObject.Ecs.Components;

public record struct NpcStateComponent(
    NpcMonsterDto NpcMonster,
    MapInstance MapInstance,
    SemaphoreSlim HitSemaphore,
    ConcurrentDictionary<Entity, int> HitList,
    Shop? Shop,
    IDisposable? Life,
    Dictionary<Type, Subject<RequestData>> Requests,
    short? Dialog,
    bool IsDisabled
);
