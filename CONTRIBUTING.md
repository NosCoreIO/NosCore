# Contributing to NosCore

## Concurrency & locking

NosCore intentionally uses **per-resource locks** rather than a global lock manager. Each lock has one job and lives on the resource it protects. When you add a new lock, follow the existing pattern below — do not introduce a centralised `LockManager`.

| Resource | Lock | Where it lives |
|---|---|---|
| Inbound packets for a session | `SemaphoreSlim _handlingPacketLock` | `ClientSession` |
| Damage application to a single entity | `SemaphoreSlim HitSemaphore` | `IAliveEntity` (player / monster / NPC) |
| SignalR hub connect/disconnect | `SemaphoreSlim _connectionLock` | `BaseHubClient` / `PubSubHubClient` |

### Rules

1. **One purpose per lock.** A lock guards exactly one mutable resource. Don't reuse an existing semaphore for unrelated state.
2. **Lock lives on the resource.** Put it as a field on the entity / session / client it protects, not in a static dictionary.
3. **Use `SemaphoreSlim(1, 1)` for serialization, `ConcurrentDictionary` for shared maps.** Don't reach for `lock(obj)` — async code can't `await` inside a `lock`.
4. **Always `try { ... } finally { _lock.Release(); }`** when acquiring a semaphore. Without `finally` an exception leaves the lock held.
5. **Never hold a lock across an external I/O call (DB, HTTP, SignalR).** If the call is slow, drop the lock first or use a more granular one.
6. **Don't acquire two locks at once** unless the order is documented at both call sites. Lock-ordering bugs surface at scale.

If a new feature needs cross-cutting coordination (e.g. "all map instances must agree on X"), prefer publishing a Wolverine message to a handler that owns the state — not adding a lock that spans subsystems.

## Background work & scheduled jobs

For "do X every N minutes" jobs, use the Wolverine pattern in `NosCore.GameObject.Messaging.ScheduledJobs/`:

1. Define a message record: `public sealed record FooJobMessage;`
2. Define a handler with a `Handle(FooJobMessage _)` method.
3. Register a `RecurringMessagePublisher<FooJobMessage>` as an `IHostedService` in the relevant `*Bootstrap.cs`.

For one-shot delayed work, publish via `IMessageBus.PublishAsync` with a `DeliveryOptions { ScheduledTime = ... }` — see Wolverine docs for details.

## Domain events

Cross-cutting reactions ("on monster killed → award XP, update quest progress, write family log") should be Wolverine messages, not direct calls between services. See `NosCore.GameObject.Messaging.Events/MonsterKilledEvent.cs` for the template.

### Conventions

- **Events** live in `NosCore.GameObject.Messaging.Events/` as `sealed record EventNameEvent(...)`. Use past tense — `XHappenedEvent` — to make it clear the event represents something that already occurred.
- **Handlers** live in `NosCore.GameObject.Messaging.Handlers/<Domain>/`, one folder per packet/feature area (e.g. `Guri/`, `Nrun/`, `UseItem/`, `MapItem/`, `Map/`). Class names always end with `Handler`, never `EventHandler`. Handlers are plain classes — Wolverine discovers them by convention via the `Handle`/`HandleAsync` method.
- **One handler per file.** Multiple handlers can subscribe to the same event; each filters internally with an early `return` if the event isn't relevant.
- **Publishing**: inject `Wolverine.IMessageBus` and call `messageBus.PublishAsync(new XEvent(...))`. Don't introduce per-domain "runner" services — the bus is the runner.

### Recurring & scheduled work

Recurring jobs (e.g. periodic save) are registered as `RecurringMessagePublisher<TMessage>` hosted services in the relevant `*Bootstrap.cs`. The publisher fires a fresh message every interval; the Wolverine handler does the work. See `Messaging/ScheduledJobs/` for examples.

For one-shot delayed work (e.g. "expire this buff in 30s"), publish via `IMessageBus.PublishAsync` with `DeliveryOptions { ScheduledTime = ... }`.
