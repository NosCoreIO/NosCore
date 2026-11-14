# Getting started

From a fresh clone to a character walking around on your own machine. The steps below were run
on Ubuntu 24.04 with PostgreSQL 16; the commands are the same on Windows and macOS apart from
how PostgreSQL is installed.

Nothing here needs configuration: every default in `configuration/` points at `localhost`.

## What you need

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0).
- PostgreSQL, reachable on `localhost:5432` with the user `postgres` and the password `password`.
  To use other values, set `DB_HOST`, `DB_USER` and `DB_PASSWORD` in the environment of every
  process below.
- A NosTale client installed, to play with.

## 1. Build

From the root of the NosCore clone:

```bash
dotnet build NosCore.sln
```

Everything lands in `build/net11.0`. The servers find `configuration/` relative to that folder,
so start them from there.

## 2. Start the servers

Each in its own terminal, from `build/net11.0`, in any order:

```bash
dotnet NosCore.MasterServer.dll
```

```bash
dotnet NosCore.LoginServer.dll
```

```bash
dotnet NosCore.WorldServer.dll
```

The first time, the world server finds an empty database and fills it before it opens: it
downloads the client's data files into `build/net11.0/client-data`, creates the `noscore`
database, applies the migrations and imports items, monsters, maps, skills and quests. That
takes two to three minutes, most of it the download. The master and login servers wait for it
and print `Waiting for a world server to do it` meanwhile.
It is ready when the world prints `Successfully Loaded!`.

After a `git pull` there is nothing to run either. The world server imports again, from the
same folder, whenever the build or the client files changed since the last import, or a
migration is pending.

The first import also creates two accounts, both with the password `test`:

| Account | Role |
|---|---|
| `admin` | game master: can use the `$` commands (`$Help` lists them) |
| `test` | ordinary player |

| Server | Listens on | What it does |
|---|---|---|
| Master | 5000 (HTTP) | the one authority for everything shared between channels |
| Login | 4000 | account check and channel list |
| World | 1337 | one game channel |
| WebApi | 8080 (`API_PORT`) | optional: account login for the launcher |

`dotnet NosCore.WebApi.dll` is only needed for clients that log in through
[NosCore.Launcher](https://github.com/NosCoreIO/NosCore.Launcher).

Stop the world server with Ctrl+C rather than killing it: it saves its characters on the way
out.

## 3. Add a packet capture

The client's files do not say where monsters, NPCs, portals, shops and teleporters are: those
exist only in what a server sends. Without a capture the server runs, but its maps are empty,
and the import says so with `packet.txt is missing`.

The capture is a file of server packets, one per line. Record it with the Packet Logger in
[NosCore.DeveloperTools](https://github.com/NosCoreIO/NosCore.DeveloperTools) while playing on a
server that already has them: walk through the maps you want filled and open the shops you want
stocked. Put it at `build/net11.0/client-data/parser/packet.txt` and restart the world server;
it notices the new file and imports again.

## The parser on its own

The world server runs the parser for you, but it can be run by hand, for instance to import
from client files of your own:

```bash
dotnet NosCore.Parser.dll --folder /path/to/parser/input
```

Without `--folder` it downloads the client files as above. `--interactive` asks for the folder
and for each import in turn. The world server remembers the folder of the last import and uses
it from then on.

## 4. Connect a client

The client has the official login server's address built in, so it needs a copy that points
at yours. The Client Creator in
[NosCore.DeveloperTools](https://github.com/NosCoreIO/NosCore.DeveloperTools) makes one from
`NostaleClientX.exe`: give it the address `127.0.0.1` and an output name, and put the result
in the game folder. Started without arguments, that copy opens the account and password login.

Log in as `admin` / `test`, pick the server and the channel, create a character and press
Start.

## Running the tests

```bash
dotnet test NosCore.sln
```

The unit tests use an in-memory database and need none of the above. The end-to-end scenarios
(`test/NosCore.EndToEnd.Tests`) start the three servers against a database of their own; they
need the PostgreSQL from above and report themselves as not run without it.

## When something does not work

- **A server keeps printing "Waiting for a world server to do it".** No world server has
  imported the game data yet. Start one, or run the parser.
- **The maps have no monsters or NPCs.** There is no `packet.txt` in the folder of the last
  import; see step 3.
- **"Already connected" at login.** The world server still holds the account's last session.
  It lets go when that connection closes; restarting the world server always clears it.
- **`MSB3026` or `MSB3027` during a build.** A running server holds the files being replaced.
  Stop the servers and build again.
