# Contributing to NosCore

## Concurrency & locking

Setting the project up is in [documentation/getting-started.md](documentation/getting-started.md).

Three things keep concurrent work apart. Use the one that already owns the state you are touching
rather than adding a lock next to it.

| What it serializes | How | Where it lives |
|---|---|---|
| The packets of one session | `AsyncLock _handlingPacketLock` | `ClientSession` |
| Everything that changes one map: the life-loop tick, hits, vitals, entering and leaving | `MapExecutor`, reached with `entity.OnMapAsync(...)` | `MapInstance` |
| Rows several channels share: bazaar listings, warehouses, family rows | the master server is their only writer, and takes a lock around each change | the master-side service (`BazaarService`, `WarehouseService`, `FamilyRecordService`) |

### Rules

1. **Work on a map goes through its executor.** Code reached from a packet handler that changes what
   other players on the map see or fight with runs inside `OnMapAsync`. Work running on an executor
   must not wait on another executor or on a session's packet lock; a map change takes two executors
   through `RunExclusiveAsync`, in a fixed order. Start a map change outside
   `OnMapAsync`: from inside the later-created of the two maps it throws instead of waiting.
2. **State shared between channels is written by the master.** A channel asks through a hub
   (`IBazaarHub`, `IWarehouseHub`, `IFamilyHub`) and the master re-reads the row, checks and writes
   under its lock. A lock in a channel does not stop another channel.
3. **One purpose per lock.** A lock guards exactly one mutable resource. Don't reuse an existing one
   for unrelated state.
4. **Use `AsyncLock` (`NosCore.Core.Concurrency`) with `using var _ = await x.AcquireAsync();`** so it
   is released on every path, and `ConcurrentDictionary` for shared maps. Don't reach for
   `lock(obj)`: async code can't `await` inside it.
5. **Don't hold a channel-side lock across a call to the master or to another server.** The
   master-side locks in the table are the exception: holding them across the read and the write is
   their purpose.
6. **Don't acquire two locks at once** unless the order is documented at both call sites.

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
