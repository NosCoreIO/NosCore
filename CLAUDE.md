# NosCore

A NosTale server reimplementation. .NET 10, EF Core 10 + Npgsql + NodaTime, Autofac,
Arch.Core ECS, Wolverine for messaging, source generators, `TreatWarningsAsErrors`.

---

## Code conventions

### Comments are the exception, not the default

Write **no comment** unless the *why* cannot be inferred from names and types. When one is
genuinely needed, a line or two. These all get removed in review:

- restating what the code already says
- narrative essays about how a bug was found, what it cost, or how many declarations exist
- provenance — where a number came from, which other project does it differently
- `ONE ASSUMPTION IS OURS`-style emphasis, block capitals, rhetorical structure

A comment earns its place by answering a question the reader would otherwise have to go
digging for. `// Array.Empty so "nothing worn" compares equal to None.` is worth a line.
Three paragraphs on why a table might be wrong are not.

Do not describe *what changed* — that is the commit message's job, and it goes stale the
moment someone else edits the file.

### English only

Code, comments, commit messages, PR bodies, test names. Non-English text has reached
review more than once and always comes back.

### No BOM

`.cs` files have no UTF-8 BOM. Editors add them silently on save; check before pushing:

```bash
for f in $(git diff --name-only origin/master...HEAD | grep '\.cs$'); do
  [ "$(head -c 3 "$f" | xxd -p)" = "efbbbf" ] && echo "BOM: $f"
done
```

`.resx` files in this repo are inconsistent already — leave those alone.

### Keep semantic types

Do not flatten a `bool` or an enum to a number because the wire value happens to be `0`
or `1`. The serializer handles the conversion; the type is what makes the call site
readable.

### Ship only what is wired

No placeholder enums, tabs, buttons or handlers for behaviour that does not exist. A
reference screenshot or a `.dat` column is not a specification — wire-status is. If
something is deliberately empty, that has to be the point (see
`NosCore.PacketHandlers/NoAction/`, which is no-op by design).

### Versions are real

A new project starts at `0.0.1`. Do not label unshipped work `v2.1`.

---

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

`NosCore.Algorithm` already owns `CloseDefence`, `Damage`, `Dignity`, `DistanceDefence`,
`DistanceDodge`, `Dodge`, `Experience`, `FairyExperience`, `FamilyExperience`,
`HeroExperience`, `HitRate`, `Hp`, `JobExperience`, `MagicDefence`, `MateExperience`, `Mp`,
`Reputation`, `SecondaryDamage`, `SecondaryHitRate`, `SpExperience`, `Speed` and `Sum`.
**If a number is a curve or a stat formula it belongs there**, as `I<Thing>Service` +
`<Thing>Service`, with the level ceiling in `Constants.cs` and an approval table in
`test/NosCore.Algorithm.Tests/DocumentationTest.cs`. Do not hand-roll a lookup table in
`NosCore.GameObject` — that has been reviewed out twice.

### Projects in this repo

| Project | Owns | Must not contain |
|---|---|---|
| `NosCore.Data` | DTOs, enumerations, `.resx` language resources | game logic |
| `NosCore.Database` | EF entities and migrations | game logic |
| `NosCore.GameObject` | ECS bundles/components, game services, event handlers | wire formats, stat curves |
| `NosCore.PacketHandlers` | One handler per client packet | rules that belong in a service |
| `NosCore.Parser` | `.dat` ingestion and documentation generation | — |
| `NosCore.Core` | Shared infrastructure | — |

`NosCore.Data` being data-only and `NosCore.GameObject` holding the logic is deliberate,
not an accident to be tidied up.

### Deciding, in order

1. A **wire shape** — field order, separator, sentinel? → `NosCore.Packets`, and a packet
   trace is the only acceptable evidence.
2. A **number that scales with level or a stat**? → `NosCore.Algorithm`.
3. The **meaning of a `.dat` column or a BCard subtype**? → an enum in `NosCore.Data`,
   wired by `NosCore.Parser`. `.dat` data belongs in the database, **not** in a generated
   C# table checked into the repo — a table cannot be regenerated when the client updates.
4. **Behaviour** — what happens when an effect fires? → a service in `NosCore.GameObject`.
5. A **player-facing string**? → a `LanguageKey` plus every `.resx` under
   `NosCore.Data/Resource/`, never a literal.

### Schema changes

Additive where possible. When a value's source changes, rewire the parser rather than
dropping or renaming existing columns. Expand a `.dat` bitmask into one boolean column per
flag — never store the raw mask as a single aggregate column.

---

## Research sources, and the rule about them

The game is closed-source, so unknown formulas and enum meanings get looked up. In
descending order of trust:

1. **The client `.dat` files** (`C:\Users\erwan\Desktop\parser`) — authoritative. The
   matching `_code_<lang>_<File>.txt` language files carry the human-readable description
   of each row; pair them to learn what a subtype actually does.
2. **Packet traces** — authoritative for the wire. A trace outranks any other
   implementation's packet class.
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
back. Referencing the `.dat` files or a trace is fine; they are project input.

If a number is doubtful, record the doubt without its provenance:

```csharp
// A ginfo line puts a level 7 family's bar at 640 000 against the 1 900 000 here, so at
// least that row is suspect.
```

One contradicted row means re-check the whole table — it is rarely a single bad
transcription. Check the table's *length* too; inherited tables are often truncated at an
old level cap.

The `nostale-reference-mining` skill covers the full method.

---

## Git workflow

- **Never commit to `master`.** Branch, PR, CI green, then merge — including one-line
  chores.
- **Rebase, never merge.** Linear history. `git rebase origin/master`, `--force-with-lease`
  on push. Never `git merge origin/master` into a branch, and never `git pull` on a feature
  branch without `--rebase`.
- Base new work on `origin/master`. Keep iterating on the same feature branch rather than
  opening a new one per follow-up.
- Opening a PR is fine unprompted. **Wait for the go-ahead before merging**, unless the
  request already covers it.
- After merging anything that changes a shared signature, **re-check the other open PRs**.
  Git merges text, not meaning: adding a required constructor parameter compiles fine on
  its own branch and breaks every branch that constructs the type.

---

## Build, test, troubleshoot

```bash
dotnet build NosCore.sln
dotnet test NosCore.sln
```

- **Source generators** live in `tools/NosCore.DtoGenerator` (DTOs from entities) and
  `tools/NosCore.EcsGenerator` (ECS bundles). Both pin Roslyn with `VersionOverride`,
  because a generator must target the *oldest* compiler it runs on. Do not let them float
  to the central `Microsoft.CodeAnalysis` version — that produces `CS9057` and breaks every
  build using the generator.
- **Migrations**: `dotnet ef migrations add <Name> --project src/NosCore.Database`. Never
  hand-write the Designer files.
- **The parser** is interactive by default; `--folder <path>` for a non-interactive run. It
  applies pending migrations on startup.
- **`MSB3026` / `MSB3027`** during a build means a running server or an open Visual Studio
  is holding the DLL. The compile almost certainly succeeded — stop the process rather than
  chasing a code error.
- **Do not fake local infrastructure to force a green build.** No invented environment
  variables, no stubbed-out NuGet sources. Ship the change and hand back the setup steps.
  NuGet sources that depend on an environment variable go in `Directory.Build.props` behind
  a `Condition`, never as `%VAR%` in `NuGet.config` (that gives `NU1301` when unset).
- **C# only.** "In C#" is literal — no native sidecar, however small. Ask before
  introducing any non-C# component.
- For image work the house library is **NetVips**, not ImageSharp: ImageSharp 4+ fails a
  Release build without a purchased Six Labors licence.

### Releasing a sibling package

Editing a sibling checkout does **not** affect this build. A change spanning packages is a
chain, not a PR:

branch → PR → CI → merge → tag `X.Y.Z` (bare, no `v`) → wait for nuget.org indexing →
`dotnet nuget locals all --clear` → bump `Directory.Packages.props` here → PR.

`dotnet nuget push` succeeding is not the same as the package being restorable; poll
`https://api.nuget.org/v3-flatcontainer/<id>/index.json` before bumping the consumer. The
`noscore-packets-release` skill documents the cycle.

---

## Manual verification

Much of this project is only observable in a running client. Automated tests cover the
arithmetic; they do not tell you whether a bar moved or a monster walked.

**Read `documentation/manual-test-plan.md` whenever a change touches gameplay** — combat,
stats, movement, equipment, skills, monsters or the character — and quote the relevant
checks back when handing the work over, so there is a concrete list to run rather than a
vague "test it in game".

**Add to that file when you add a gameplay feature.** If a change genuinely cannot be
observed from the client — it guards an unreachable state, or it is data waiting on
behaviour that is not wired yet — say so in its "Not verifiable yet" section instead of
inventing steps. Never claim a gameplay change is verified on the strength of unit tests
alone; say what was tested and what still needs a client.
