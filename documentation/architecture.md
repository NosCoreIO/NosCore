# Architecture

How NosCore is put together, for someone about to change it. Setting it up is in
[getting-started.md](getting-started.md); the rules for locks, jobs and events are in
[CONTRIBUTING.md](../CONTRIBUTING.md).

## The servers

| Process | Role |
|---|---|
| `NosCore.MasterServer` | The one authority for whatever more than one channel can touch: who is connected, the bazaar, warehouses, mail, friends, family rows. |
| `NosCore.LoginServer` | Checks an account and hands the client the list of channels. |
| `NosCore.WorldServer` | One game channel: maps, monsters, players, combat. Several can run against one master. |
| `NosCore.WebApi` | Account login for the launcher. Optional. |
| `NosCore.Parser` | Not a server: reads the client's data files into the database and applies the migrations. |

They talk through [Wolverine](https://wolverinefx.net). A channel never reaches another channel
directly: it sends the master a request through a hub (`InterChannelCommunication/Hubs`), and the
master publishes what the other channels need to hear. By default the queues live in the same
PostgreSQL database as the game data; RabbitMQ is a configuration switch.

## Where things belong

NosCore is this repository plus NuGet packages, each in its own repository under `NosCoreIO`.
A fact belongs in exactly one place.

| Package | Owns |
|---|---|
| `NosCore.Packets` | Every packet class and the serializer: all knowledge of the wire format |
| `NosCore.Algorithm` | Every formula or table that scales with a level or a stat |
| `NosCore.Shared` | Localisation, logging, cross-cutting enums, configuration primitives |
| `NosCore.Dao` | The data-access abstraction (`IDao<,>`) |
| `NosCore.Networking` | Session and socket plumbing |
| `NosCore.PathFinder` | Path finding |
| `NosCore.Analyzers` | Roslyn analyzers |

| Project here | Owns | Must not contain |
|---|---|---|
| `NosCore.Data` | DTOs, enumerations, language resources | game logic |
| `NosCore.Database` | EF entities and migrations | game logic |
| `NosCore.GameObject` | ECS components and bundles, game services, event handlers | wire formats, stat curves |
| `NosCore.PacketHandlers` | One handler per client packet | rules that belong in a service |
| `NosCore.Parser` | Reading the `.dat` files, generating their documentation | |
| `NosCore.Core` | Shared infrastructure | |

Deciding where a change goes, in order:

1. The shape of a packet: `NosCore.Packets`, backed by a packet trace.
2. A number that scales with a level or a stat: `NosCore.Algorithm`.
3. The meaning of a `.dat` column: an enum in `NosCore.Data`, filled by `NosCore.Parser`. Game
   data lives in the database, not in generated C#.
4. What happens when something fires: a service in `NosCore.GameObject`.
5. Text a player reads: a `LanguageKey` and every `.resx`, never a literal.

## Entities

Players, monsters, NPCs and mates are [Arch](https://github.com/genaray/Arch) entities.

- **Components** (`Ecs/Components`) hold all state. New per-entity state goes into a component,
  never into a service dictionary keyed by entity or a static.
- **Bundles** (`[ComponentBundle]` partial structs) are the generated facade the rest of the code
  reads and writes. Hand-written members are computed views with no backing field.
- **Extension methods** (`Ecs/Extensions`) are behaviour on one entity, through its bundle.
- **Systems** (`Ecs/Systems`) are for queries over many entities, such as the map life loop.

## A packet's way through a channel

1. `NosCore.Networking` decodes the bytes into a packet from `NosCore.Packets`.
2. The session takes its packet lock, so one player's packets are handled one at a time. The
   packet's own attributes (scope, minimum interval, refusals while dead or in a shop) are checked
   before any handler runs.
3. The handler in `NosCore.PacketHandlers` reads the packet and calls a service in
   `NosCore.GameObject`.
4. Work that changes what others on the map see runs on that map's `MapExecutor`
   (`entity.OnMapAsync`), one item at a time with the life-loop tick.
5. A change to state that other channels share goes to the master, which re-reads the row and
   writes it under its own lock.
6. Reactions that cross features (a kill gives experience, moves a quest, writes a family log) are
   Wolverine events handled in `Messaging/Handlers`.

## Tests

Every test gets its own `TestWorld` with an in-memory database, a fake clock and three maps, and
builds the class under test with `World.Create<T>()`. [test/README.md](../test/README.md) has the
pattern. What only a running client can show is listed in
[manual-test-plan.md](manual-test-plan.md).
