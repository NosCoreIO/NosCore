# Observability

Every server publishes metrics on a meter named `NosCore` and traces on an activity source
of the same name (`src/NosCore.Core/Observability`). Nothing has to be configured for the
servers to produce them; reading them takes one of the two tools below.

## Metrics

| Instrument | Kind | Tags | What it measures |
|---|---|---|---|
| `noscore.sessions.online` | up-down counter | | Sessions connected to the server |
| `noscore.packets.handled` | counter | `packet.header` | Client packets that reached their handler |
| `noscore.packets.duration` | histogram, ms | `packet.header` | Time a handler takes, wait for the session's packet lock included |
| `noscore.packets.refused` | counter | `reason` | Client packets refused before their handler (see below) |
| `noscore.map.tick.duration` | histogram, ms | `map.id` | One life tick of an awake map |
| `noscore.map.players` | gauge | `map.id` | Players on an awake map; 0 once it falls asleep |
| `noscore.map.monsters` | gauge | `map.id` | Monsters on an awake map; 0 once it falls asleep |
| `noscore.map.executor.wait` | histogram, ms | | Time work waits for its map's executor |
| `noscore.save.duration` | histogram, ms | | One character save |
| `noscore.save.failures` | counter | | Character saves that were rolled back |
| `noscore.master.request.duration` | histogram, ms | `message.type` | Round trip of a channel's request to the master |
| `noscore.master.request.timeouts` | counter | `message.type` | Requests to the master that got no answer in time |
| `noscore.pathfinding.searches` | counter | `found` | Monster path searches, and whether they found a path |
| `noscore.pathfinding.duration` | histogram, ms | | One monster path search |

The refusal reasons are `keepalive`, `corrupt`, `no_handler`, `invalid`, `in_shop`,
`before_mfa`, `without_character`, `in_game`, `authority`, `dead` and `sender_state`. Each
matches a log line of the world server, so a rise in one points to the lines to read.

Instances of one map (time-spaces, raids) share its `map.id`, so their gauges overwrite each
other; the tick histogram adds them up.

The runtime, HTTP client and ASP.NET Core instruments are published as well.

## Watching a running server

`dotnet-counters` reads the meter straight from the process, with no collector:

```bash
dotnet tool install -g dotnet-counters
dotnet-counters ps                                   # find the server's process id
dotnet-counters monitor -p <id> --counters NosCore
```

`dotnet-counters collect ... --format csv -o world.csv` writes the same values to a file.

## Exporting

The servers export metrics and traces over OTLP. The standard variables choose where:
`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME` and
`OTEL_RESOURCE_ATTRIBUTES`. Without them the exporter tries `http://localhost:4317` and drops
what it cannot send.

Any OTLP backend works. The .NET Aspire dashboard is one container and needs no setup:

```bash
docker run --rm -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard
```

Start the servers, then open the address the container prints (`http://localhost:18888/login?t=...`);
the Metrics page lists each server under its service name.
