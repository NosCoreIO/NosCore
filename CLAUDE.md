# NosCore

A NosTale server reimplementation. .NET 10, EF Core 10 + Npgsql + NodaTime, Autofac,
Arch.Core ECS, Wolverine for messaging, source generators, warnings-as-errors.

## Where things belong

NosCore is a main repo plus independently versioned NuGet packages, each in its own repo
under `NosCoreIO`. **A fact goes in exactly one place.** Putting it in the convenient place
rather than the right one is the most common reason a PR is sent back.

### Sibling packages — separate repos, separate release cycle

| Package | Local checkout | Owns |
|---|---|---|
| `NosCore.Packets` | `c:\dev\NosCore.Packets` | Every packet class and the serializer. All wire-format knowledge. |
| `NosCore.Algorithm` | `c:\dev\NosCore.Algorithm` | Every stat and progression formula or table |
| `NosCore.Shared` | `c:\dev\NosCore.Shared` | I18N, `Logger`, cross-cutting enums, config primitives |
| `NosCore.Dao` | `c:\dev\NosCore.Dao` | Data-access abstraction (`IDao<,>`) |
| `NosCore.Networking` | `c:\dev\NosCore.Networking` | Session and socket plumbing |
| `NosCore.PathFinder` | `c:\dev\NosCore.PathFinder` | Brushfire, flow field, jump point search |
| `NosCore.Analyzers` | — | Roslyn analyzers |

Editing a sibling checkout does **not** affect this build. It needs a release: branch → PR →
merge → tag `X.Y.Z` → wait for nuget.org indexing → bump `Directory.Packages.props` here.
A change spanning packages is a chain, not a PR.

`NosCore.Algorithm` already owns `CloseDefence`, `Damage`, `Dignity`, `DistanceDefence`,
`DistanceDodge`, `Dodge`, `Experience`, `FairyExperience`, `FamilyExperience`,
`HeroExperience`, `HitRate`, `Hp`, `JobExperience`, `MagicDefence`, `MateExperience`, `Mp`,
`Reputation`, `SecondaryDamage`, `SecondaryHitRate`, `SpExperience`, `Speed` and `Sum`.
**If a number is a curve or a stat formula it belongs there**, as `I<Thing>Service` +
`<Thing>Service`, with the level ceiling in `Constants.cs` and an approval table in
`test/NosCore.Algorithm.Tests/DocumentationTest.cs`. Do not hand-roll a lookup table in
`NosCore.GameObject`.

### Projects in this repo

| Project | Owns | Must not contain |
|---|---|---|
| `NosCore.Data` | DTOs, enumerations, `.resx` language resources | game logic |
| `NosCore.Database` | EF entities and migrations | game logic |
| `NosCore.GameObject` | ECS bundles/components, game services, event handlers | wire formats, stat curves |
| `NosCore.PacketHandlers` | One handler per client packet | rules that belong in a service |
| `NosCore.Parser` | `.dat` ingestion and documentation generation | — |
| `NosCore.Core` | Shared infrastructure | — |

### Deciding, in order

1. A **wire shape** — field order, separator, sentinel? → `NosCore.Packets`, and a packet
   trace is the only acceptable evidence.
2. A **number that scales with level or a stat**? → `NosCore.Algorithm`.
3. The **meaning of a `.dat` column or a BCard subtype**? → an enum in `NosCore.Data`,
   wired by `NosCore.Parser`. `.dat` data belongs in the database, not in a generated C#
   table.
4. **Behaviour** — what happens when an effect fires? → a service in `NosCore.GameObject`.
5. A **player-facing string**? → a `LanguageKey` plus every `.resx` under
   `NosCore.Data/Resource/`, never a literal.

## Research sources, and the rule about them

The game is closed-source, so unknown formulas and enum meanings get looked up. In
descending order of trust:

1. **The client `.dat` files** (`C:\Users\erwan\Desktop\parser`) — authoritative. The
   matching `_code_<lang>_<File>.txt` language files carry the human-readable description
   of each row; pair them to learn what a subtype actually does.
2. **Packet traces** — authoritative for the wire.
3. **nosapki.com** — a live game database, reliable for anything player-facing, and the
   right way to check a table before trusting it. Watch whether "Level N" means *at* N or
   *to leave* N, and whether its table runs further than yours.
4. **OpenNos** (`C:\dev\OpenNos`) — a pre-i18n fork of an old client. Every constant is a
   hypothesis, never an answer. Errors already found and fixed downstream: BCard types
   56/57 swapped, `NoPenatly` sharing a value with `DistanceDamageIncreasing`, subtypes
   marked "didn't exist" that do, enum members numbered `00`/`01` which are not valid
   subtypes, and the entire family experience table.

**Never name these sources in code, comments, commit messages or PR bodies.** Describe
NosCore's behaviour in its own terms. Euphemisms count — "the sibling codebase", "the older
emulators", "the reference implementation" are the same violation and have all been sent
back in review. Referencing the `.dat` files or a trace is fine; they are project input.

If a number is doubtful, record the doubt without its provenance:

```csharp
// A ginfo line puts a level 7 family's bar at 640 000 against the 1 900 000 here, so at
// least that row is suspect.
```

One contradicted row means re-check the whole table — it is rarely a single bad
transcription.

## Conventions

- **Comments are the exception.** Default to none. Write one only when the *why* cannot be
  inferred from names and types, and keep it to a line or two. Long narrative blocks,
  provenance essays and restating what the code says are all removed in review.
- **English only**, in code, comments and commit messages.
- **No UTF-8 BOM** on `.cs` files. Editors add them silently; strip before pushing.
- **Never commit to `master`.** Branch, PR, CI green, then merge — including one-line
  chores.
- **Rebase, never merge.** Linear history. `git rebase origin/master`, force-with-lease on
  push.
- Keep semantic types. Do not flatten a `bool` or an enum to a number because the wire
  value happens to be `0`/`1`; the serializer handles the conversion.
- Schema changes are additive where possible. Expand a `.dat` bitmask into one boolean
  column per flag rather than storing the raw mask.

## Build and test

```bash
dotnet build NosCore.sln
dotnet test NosCore.sln
```

Source generators live in `tools/NosCore.DtoGenerator` (DTOs from entities) and
`tools/NosCore.EcsGenerator` (ECS bundles). Both pin Roslyn via `VersionOverride` because a
generator must target the oldest compiler it runs on — do not let them float to the central
`Microsoft.CodeAnalysis` version.

Migrations: `dotnet ef migrations add <Name> --project src/NosCore.Database`. Never
hand-write the Designer files.

The parser is interactive by default; `--folder <path>` for a non-interactive run. It
applies pending migrations on startup.

## Manual verification

`documentation/manual-test-plan.md` lists what has to be checked in a running client,
because a good deal of this project is only observable there.
