# NosCore

A NosTale server reimplementation. .NET 10, EF Core + Npgsql + NodaTime, Autofac,
Arch.Core ECS, Wolverine for messaging, source generators, `TreatWarningsAsErrors`.

---

## Code conventions

**Comments are the exception.** Write one only when the *why* cannot be inferred from names
and types, and keep it to a line or two. Do not restate what the code says, narrate how a
bug was found, or record where a value came from. Do not describe what changed — that is
the commit message's job.

**English only** — code, comments, commit messages, PR bodies, test names.

**No UTF-8 BOM on `.cs` files.** Some editors add one on save. Check before pushing:

```bash
for f in $(git diff --name-only origin/master...HEAD | grep '\.cs$'); do
  [ "$(head -c 3 "$f" | xxd -p)" = "efbbbf" ] && echo "BOM: $f"
done
```

**No `ConfigureAwait`.** Every host here is a generic-host console app, so there is no
synchronization context to come back to and `ConfigureAwait(false)` does nothing but add
noise to the line. Do not add it; the calls still in the tree are not a precedent.

**Keep semantic types.** Do not flatten a `bool` or an enum to a number because the wire
value happens to be `0` or `1`; the serializer handles the conversion.

**The serializer owns the wire format.** Never massage a value to fit it: no
`Replace(' ', '^')` before assigning a field, no manual padding, no hand-built separator. A
field that needs escaping says so on the packet property - `EscapeSpaces` for the one that
ends the line - so the fix belongs in `NosCore.Packets` and ships as a version bump. A
`Replace` here to make a packet come out right is a defect, including as a stopgap while
the package catches up.

**Ship only what is wired.** No placeholder enums, handlers or UI for behaviour that does
not exist yet. A data file listing a field is not a reason to expose it.

---

## Where things belong

NosCore is a main repo plus independently versioned NuGet packages, each in its own repo
under `NosCoreIO`. **A fact belongs in exactly one place.** Choosing the convenient place
over the correct one is the most common reason a change is sent back.

### Sibling packages

| Package | Owns |
|---|---|
| `NosCore.Packets` | Every packet class and the serializer — all wire-format knowledge |
| `NosCore.Algorithm` | Every stat and progression formula or table |
| `NosCore.Shared` | I18N, logging, cross-cutting enums, config primitives |
| `NosCore.Dao` | Data-access abstraction (`IDao<,>`) |
| `NosCore.Networking` | Session and socket plumbing |
| `NosCore.PathFinder` | Brushfire, flow field, jump point search |
| `NosCore.Analyzers` | Roslyn analyzers |

`NosCore.Algorithm` holds one service per curve — experience, HP/MP, damage, defence,
dodge, hit rate, reputation and so on. **Any number that scales with a level or a stat
belongs there**, as `I<Thing>Service` + `<Thing>Service`, with the ceiling in `Constants.cs`
and an approval table in its `DocumentationTest`. Do not hand-roll a lookup table inside
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

`NosCore.Data` being data-only and `NosCore.GameObject` holding the logic is deliberate.

### Deciding, in order

1. A **wire shape** — field order, separator, sentinel? → `NosCore.Packets`, evidenced by a
   packet trace.
2. A **number that scales with a level or stat**? → `NosCore.Algorithm`.
3. The **meaning of a `.dat` column or a BCard subtype**? → an enum in `NosCore.Data`,
   wired by `NosCore.Parser`. Game data belongs in the database, not in a generated C#
   table — a table cannot be regenerated when the client updates.
4. **Behaviour** — what happens when an effect fires? → a service in `NosCore.GameObject`.
5. A **player-facing string**? → a `LanguageKey` plus every `.resx` under
   `NosCore.Data/Resource/`, never a literal.

### Schema changes

Additive where possible. When a value's source changes, rewire the parser rather than
dropping or renaming columns. Expand a bitmask field into one boolean column per flag;
never store the raw mask as a single aggregate column.

---

## Research sources

The game is closed-source, so formulas and enum meanings have to be looked up. In
descending order of trust:

1. **The client data files** and their matching language files — authoritative. The data
   file has the numbers, the language file has the sentence describing what a row does.
   Pair them.
2. **Packet traces** — authoritative for the wire, and they outrank any other
   implementation's packet class.
3. **Community game databases** — reliable for player-facing values, and the right way to
   check a table before trusting it. Confirm whether a row means *at* level N or *to leave*
   level N, and whether the table runs further than the one you have.
4. **Other server implementations** — a hypothesis, never an answer. They are forks of
   older clients and carry known-wrong constants.

**Never name a source in code, comments, commit messages or PR bodies.** Describe NosCore's
behaviour in its own terms. Euphemisms count — "the sibling codebase", "the older
emulators", "the reference implementation" are the same violation. Referencing the game's
own data files or a packet trace is fine; those are project input.

If a value is doubtful, record the doubt without its provenance:

```csharp
// A ginfo line puts a level 7 family's bar at 640 000 against the 1 900 000 here, so at
// least that row is suspect.
```

One contradicted row means re-checking the whole table rather than patching that row — and
check the table's length too, since inherited tables are often truncated at an old cap.

---

## Git workflow

- **Never commit to `master`.** Branch, PR, CI green, then merge — including one-line
  chores.
- **Rebase, never merge.** Linear history: `git rebase origin/master`, `--force-with-lease`
  on push. Never merge `master` into a branch — not to pick up a fix, not to resolve a
  conflict, not to make the PR mergeable again. A `Merge remote-tracking branch` or
  `merge: origin/master` commit in a PR is a defect, and the fix is to rebase it out rather
  than leave it. Check before pushing:

  ```bash
  git log --oneline --merges origin/master..HEAD   # must print nothing
  ```

  Rebasing a long branch across a renamed API repeats the same conflict on every commit;
  `git config rerere.enabled true` resolves it once and replays the rest.
- Base new work on `origin/master`, and keep iterating on the same branch rather than
  opening a new one per follow-up.
- Opening a PR unprompted is fine; **wait for approval before merging** unless the request
  already covers it.
- After merging anything that changes a shared signature, **re-check the other open PRs**.
  Git merges text, not meaning: a new required constructor parameter compiles on its own
  branch and breaks every branch that constructs the type.

---

## Build and test

```bash
dotnet build NosCore.sln
dotnet test NosCore.sln
```

- **Source generators** live under `tools/`. They pin their Roslyn version deliberately: a
  generator must target the *oldest* compiler it will run on. Letting them float to the
  central `Microsoft.CodeAnalysis` version produces `CS9057` and breaks every build that
  uses them.
- **Migrations**: `dotnet ef migrations add <Name> --project src/NosCore.Database`. Never
  hand-write the Designer files.
- **The parser** is interactive by default; pass `--folder <path>` for a non-interactive
  run. It applies pending migrations on startup.
- **`MSB3026` / `MSB3027`** means a running server or an open IDE is holding the DLL. The
  compile almost certainly succeeded — stop the process rather than hunting for a code
  error.
- **Do not fake local infrastructure to force a green build.** No invented environment
  variables, no stubbed-out package sources. Make the change and hand back the setup steps.
- **C# only.** Ask before introducing any non-C# component.

### Changing a sibling package

Editing a sibling repo does not affect this build. It is a chain, not a single PR:

branch → PR → CI → merge → tag `X.Y.Z` (bare, no `v` prefix) → wait for nuget.org indexing
→ clear the local NuGet cache → bump `Directory.Packages.props` here → PR.

A successful `dotnet nuget push` is not the same as the package being restorable; poll the
flat-container index before bumping the consumer.

---

## Manual verification

Much of this project is only observable in a running client. Automated tests cover the
arithmetic; they do not tell you whether a bar moved or a monster walked.

**Read `documentation/manual-test-plan.md` whenever a change touches gameplay** — combat,
stats, movement, equipment, skills, monsters or the character — and quote the relevant
checks when handing the work over, so there is a concrete list to run.

**Add to that file when you add a gameplay feature.** If something genuinely cannot be
observed from the client, say so in its "Not verifiable yet" section instead of inventing
steps. Never call a gameplay change verified on unit tests alone; state what was tested and
what still needs a client.
