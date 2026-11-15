<p align="center">
  <img width="200" alt="NosCore" src="https://raw.githubusercontent.com/NosCoreIO/NosCore.Packets/master/icon.png"/>
</p>

<h1 align="center">NosCore</h1>

<p align="center">
  <a href="https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml"><img alt="Build" src="https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml/badge.svg?branch=master"/></a>
  <a href="https://app.codacy.com/gh/NosCoreIO/NosCore/dashboard"><img alt="Codacy" src="https://app.codacy.com/project/badge/Grade/643fd3df0ce742ec9b2ac3dab95bdc44"/></a>
  <a href="https://discord.gg/Eu3ETSw"><img alt="Discord" src="https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white"/></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"/></a>
</p>

A NosTale server written in C# on .NET 11: master, login and world servers on PostgreSQL, with the game data read
from the client's own files.

## Getting started

[documentation/getting-started.md](documentation/getting-started.md) goes from a fresh clone to a character in the
world: the game data the parser needs, the database, the order to start the servers in, the accounts that exist by
default and how to point a client at your server.
[documentation/architecture.md](documentation/architecture.md) explains how the servers, the projects and the
sibling packages fit together.

It needs the [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) and a PostgreSQL on `localhost:5432`
(user `postgres`, password `password`). Nothing else needs configuring. In short, with each server in its own
terminal:

```bash
dotnet build NosCore.sln
cd build/net11.0
dotnet NosCore.Parser.dll --folder <parser input folder>
dotnet NosCore.MasterServer.dll
dotnet NosCore.LoginServer.dll
dotnet NosCore.WorldServer.dll
```

The parser creates the database, applies the migrations and imports the game data. Run it again after pulling: it is
safe to run on a database it already filled. On Windows, `scripts\` has a `.cmd` for each step, and
`docker-compose.yml` runs the same servers in containers. If you would rather have the servers, the Act 4 channel included, in one terminal,
run `dotnet run --project src/NosCore.DevHost` from the root of the clone once the database is filled.

To play Glacernon on a channel of its own, also start `ACT4_CHANNEL=true WORLD_PORT=1338 dotnet NosCore.WorldServer.dll` from
`build/net11.0`.

Run the tests with:

```bash
dotnet test NosCore.sln
```

## Inter-server messaging

The master, world, login and web API servers talk to each other through [Wolverine](https://wolverinefx.net). By
default the queues live in the same PostgreSQL database as the game data, so there is nothing extra to install.

The PostgreSQL transport polls its queues (`PollingIntervalMs`, 20 ms by default), so every master round trip costs
one to two polling intervals. For production, switch every server to RabbitMQ in its `Messaging` section, or with the
`MESSAGING_TRANSPORT` and `RABBITMQ_URI` environment variables:

```yaml
Messaging:
  Transport: RabbitMq
  RabbitMqUri: amqp://user:password@rabbitmq:5672
```

All servers must use the same transport. Queues are created on startup.

## Testing an unreleased package

Every NosCore repository ships a `Directory.Build.props` that adds a NuGet source when the `NOSCORE_LOCAL_PACKAGES`
environment variable is set, so a local build of `NosCore.Packets`, `NosCore.Dao` or another sibling package can be
tried before it is published. Point the variable at a folder, drop the `.nupkg` files in it and restore as usual:

```bash
export NOSCORE_LOCAL_PACKAGES=~/LocalPackages      # Windows: setx NOSCORE_LOCAL_PACKAGES C:\LocalPackages
```

`export` only lasts for that shell, so restore from the same one, or add the line to your shell profile; after a
`setx`, open a new terminal or restart the IDE. When the variable is unset, only nuget.org is used.

## The NosCore repositories

| Repository | What it holds |
|---|---|
| [NosCore](https://github.com/NosCoreIO/NosCore) | The servers: master, login, world, web API, and the parser that fills the database |
| [NosCore.Packets](https://github.com/NosCoreIO/NosCore.Packets) | Every packet class and the serializer |
| [NosCore.Algorithm](https://github.com/NosCoreIO/NosCore.Algorithm) | Stat and progression formulas and tables |
| [NosCore.Shared](https://github.com/NosCoreIO/NosCore.Shared) | I18N, logging, configuration and cross-cutting enums |
| [NosCore.Dao](https://github.com/NosCoreIO/NosCore.Dao) | Data access over Entity Framework Core |
| [NosCore.Networking](https://github.com/NosCoreIO/NosCore.Networking) | Sessions, sockets and the client's packet encryption |
| [NosCore.PathFinder](https://github.com/NosCoreIO/NosCore.PathFinder) | Brushfire, flow field and jump point search |
| [NosCore.Analyzers](https://github.com/NosCoreIO/NosCore.Analyzers) | Roslyn analyzers and suppressors for the code base |
| [NosCore.Archive](https://github.com/NosCoreIO/NosCore.Archive) | Reading and writing the client's `.NOS` archives |
| [NosCore.ParserInputGenerator](https://github.com/NosCoreIO/NosCore.ParserInputGenerator) | Downloads the client's data files and unpacks them for the parser |
| [NosCore.DeveloperTools](https://github.com/NosCoreIO/NosCore.DeveloperTools) | Packet logger and client creator |
| [NosCore.ClientLab](https://github.com/NosCoreIO/NosCore.ClientLab) | The client recompiled to run in a browser, for end-to-end tests |
| [NosCore.ReverseProxy](https://github.com/NosCoreIO/NosCore.ReverseProxy) | TCP proxy in front of the login and world servers |

See [CONTRIBUTING.md](CONTRIBUTING.md) for how concurrent work is kept apart in the servers.

## Contributing

Questions, ideas and pull requests are welcome; most of the discussion happens on [Discord](https://discord.gg/Eu3ETSw).
Any editor that handles .NET 11 works: `dotnet build` and `dotnet test` run on Windows, Linux and macOS.
In Visual Studio, [Roslynator](https://github.com/dotnet/roslynator) and
[ReSharper](https://www.jetbrains.com/resharper/) help keep to the code style.

## Supporting NosCore

NosCore is a spare-time open source project. Sponsoring buys time spent on the emulator and the packages around it,
and gives the **Supporter** role on Discord.

<p>
  <a href="https://github.com/sponsors/0Lucifer0"><img alt="Sponsor on GitHub" src="https://img.shields.io/badge/GitHub-sponsor-EA4AAA?style=for-the-badge&logo=githubsponsors&logoColor=white"/></a>
  <a href="https://ko-fi.com/A3562BQV"><img alt="Support on Ko-fi" src="https://img.shields.io/badge/Ko--fi-support-FF5E5B?style=for-the-badge&logo=kofi&logoColor=white"/></a>
  <a href="https://www.patreon.com/bePatron?u=6503887"><img alt="Become a patron" src="https://img.shields.io/badge/Patreon-become_a_patron-F96854?style=for-the-badge&logo=patreon&logoColor=white"/></a>
</p>

Thanks to [Navicat](https://www.navicat.com) for supporting the project.

<a href="https://www.navicat.com"><img height="60" alt="Navicat" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>

## Legal

NosCore is an independent, unofficial project for educational use. It is not affiliated with, authorized, maintained,
sponsored or endorsed by Gameforge or any of its affiliates; NosTale is a trademark of Gameforge. Using it may be
against the terms of service of NosTale servers.

Released under the [MIT License](LICENSE).
