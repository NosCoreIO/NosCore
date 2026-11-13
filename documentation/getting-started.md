# Getting started

From a fresh clone to a character walking around on your own machine. The steps below were run
on Ubuntu 24.04 with PostgreSQL 16; the commands are the same on Windows and macOS apart from
how PostgreSQL is installed.

Nothing here needs configuration: every default in `configuration/` points at `localhost`.

## What you need

- The [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0), and the .NET 10 SDK for
  the tool that prepares the game data.
- PostgreSQL, reachable on `localhost:5432` with the user `postgres` and the password `password`.
  To use other values, set `DB_HOST`, `DB_USER` and `DB_PASSWORD` in the environment of every
  process below.
- A NosTale client installed, to play with.

## 1. Get the game data

The server reads its items, monsters, maps, skills and quests from the client's own data files.
[NosCore.ParserInputGenerator](https://github.com/NosCoreIO/NosCore.ParserInputGenerator)
downloads those archives and unpacks them into the layout the parser reads:

```bash
git clone https://github.com/NosCoreIO/NosCore.ParserInputGenerator
```

```bash
cd NosCore.ParserInputGenerator
```

```bash
dotnet build
```

```bash
cd build/net10.0
```

```bash
dotnet NosCore.ParserInputGenerator.Launcher.dll
```

The result is `NosCore.ParserInputGenerator/build/net10.0/output/parser`: the `.dat` files, one `_code_<language>_*.txt` per
language, and a `map` folder.

The parser also needs a file the client does not contain: **`packet.txt`**, a capture of what a
server sends, one packet per line. Portals, monster and NPC positions, shops and teleporters
exist only in those packets, so the file has to be recorded while playing on a server that
already has them: walk through the maps you want filled and open the shops you want stocked.
The Packet Logger in
[NosCore.DeveloperTools](https://github.com/NosCoreIO/NosCore.DeveloperTools) attaches to the
running client and writes the lines. Put the file in the same `parser` folder. Without it the
parser stops; with a short one the server runs, and the maps that were not visited are empty.

## 2. Build

From the root of the NosCore clone:

```bash
dotnet build NosCore.sln
```

Everything lands in `build/net11.0`. The servers find `configuration/` relative to that folder,
so start them from there.

## 3. Fill the database

```bash
cd build/net11.0
```

```bash
dotnet NosCore.Parser.dll --folder /path/to/output/parser
```

The parser creates the `noscore` database if it is missing, applies every migration, and
imports the data. It takes a minute or two. Run it again after pulling: the servers do not
migrate, and each of them stops with "The database is N migrations behind this build" when it
meets an older schema. A change to what the parser imports needs the same run and has no
such message.

It also creates two accounts, both with the password `test`:

| Account | Role |
|---|---|
| `admin` | game master: can use the `$` commands (`$Help` lists them) |
| `test` | ordinary player |

Without `--folder` the parser asks for the folder and for each import in turn.

## 4. Start the servers

Each in its own terminal, from `build/net11.0`, in this order:

```bash
dotnet NosCore.MasterServer.dll
```

```bash
dotnet NosCore.LoginServer.dll
```

```bash
dotnet NosCore.WorldServer.dll
```

Wait for `Successfully Loaded!` from the master and the world, and `Listening Port 4000` from
the login server, before starting the next one.

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

## 5. Connect a client

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

- **A server stops with "The database is N migrations behind this build".** The schema is
  older than the code. Run the parser again.
- **The parser stops on `packet.txt`.** The file is missing from the folder given to
  `--folder`.
- **"Already connected" at login.** The world server still holds the account's last session.
  It lets go when that connection closes; restarting the world server always clears it.
- **`MSB3026` or `MSB3027` during a build.** A running server holds the files being replaced.
  Stop the servers and build again.
