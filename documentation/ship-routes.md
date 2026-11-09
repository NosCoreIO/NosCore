# Ship routes

Core loads ship routes from the `ShipRoute` table. Each row describes one boarding
menu, ship, destination and optional required faction. Separate rows give factions
separate ship instances, even when they use the same ship map. There are no built-in
map IDs, timetables or fares: import reviewed data appropriate to the client version.

Apply the `AddShipRoutes` migration before starting world. Back up an existing
database before applying migrations or importing routes. The migration creates an
empty table and does not modify character state or existing world content.

Import only ship routes, without reimporting the rest of the database:

```sh
dotnet run --project src/NosCore.Parser -- --ship-routes /path/to/ship-routes.json
```

The input is an array of objects with these properties (names are case sensitive):

| Properties | Meaning |
| --- | --- |
| `ShipRouteId` | Stable positive route ID; importing the same ID updates it |
| `Runner`, `CancelRunner` | Boarding and captain cancellation `NrunRunnerType` names |
| `SourceMapId`, `BoardingNpcVNum`, `BoardingDialog` | NPC menu offering this route |
| `ShipMapId`, `ShipX`, `ShipY` | Ship map template and boarding cell |
| `DestinationMapId`, `DestinationX`, `DestinationY` | Arrival map and cell |
| `ReturnX`, `ReturnY` | Cell on the source map used when cancelling |
| `CaptainVNum`, `CaptainDialog`, `CaptainX`, `CaptainY` | Captain placed aboard the ship |
| `RequiredFaction` | `Angel`, `Demon`, or `null` for unrestricted boarding |
| `DepartureIntervalSeconds` | Positive interval between scheduled departures |
| `Fare` | Nonnegative boarding fare; zero for a free crossing |

Use `GoToShipGlacernon` / `CancelShipTravelGlacernon` or
`GoToShipMortazDesert` / `CancelShipTravelMortazDesert`. Only option zero boards or
cancels. The normal NPC dialog checks still require an open dialog within reach.

The importer validates the complete input before writing. It rejects unknown
properties, duplicate IDs or menus, invalid runner pairs, missing NPCs/dialogs/maps,
negative fares and blocked landing cells. IDs not present in the file are preserved.
Restart world after importing because static data is cached at startup.

A departure is the next strictly later interval boundary measured from the Unix
epoch. All passengers who board a given sailing share its departure time. The
recurring Wolverine job checks departures every second. Each passenger's ticket is
an ECS component; cancellation, map changes and disconnects cannot leave a delayed
teleport attached to a character who has left the ship. A failed arrival retains the
ticket for a retry. If a passenger loses the required faction aboard ship, departure
returns them to port.

This feature covers outbound boarding, waiting, arrival and cancellation aboard
ship. It does not implement faction acquisition, Glacernon channel selection,
capacity limits, first-trip exemptions or departure announcements. Route data must
not assume those additional mechanics exist.
