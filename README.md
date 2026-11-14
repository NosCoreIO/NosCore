<p align="center">
  <img src="https://raw.githubusercontent.com/NosCoreIO/NosCore.Packets/master/icon.png"/>
</p>

# NosCore #
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/643fd3df0ce742ec9b2ac3dab95bdc44)](https://www.codacy.com/gh/NosCoreIO/NosCore/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NosCoreIO/NosCore&amp;utm_campaign=Badge_Grade)
[![.NET](https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml)

# Special Thanks for Contributions #
<p align="left">
<a href="https://www.navicat.com"><img height="100px" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>
</p>

## You want to contribute ? ##
[![Discord](https://i.gyazo.com/2115a3ecb258220f5b1a8ebd8c50eb8f.png)](https://discord.gg/Eu3ETSw)

## You like our work ? ##
NosCore is a spare-time open source project. Sponsoring does not buy a hosted server, a
license or a support contract — it buys time spent on the emulator and on the packages
around it (NosCore.Packets, NosCore.Dao, NosCore.Algorithm, the parser tooling).

Sponsors get the **Supporter** role on [our Discord](https://discord.gg/Eu3ETSw).

<a href='https://github.com/sponsors/0Lucifer0' target='_blank'><img height='48' style='border:0px;height:46px;' src='https://i.gyazo.com/47b2ca2eb6e1ce38d02b04c410e1c82a.png' border='0' alt='Sponsor me!' /></a>
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/A3562BQV) 
<a href='https://www.patreon.com/bePatron?u=6503887' target='_blank'><img height='46' style='border:0px;height:46px;' src='https://c5.patreon.com/external/logo/become_a_patron_button@2x.png' border='0' alt='Become a Patron!' /></a>

## Warning! ##
We are not responsible of any damages caused by bad usage of our source. Please before asking questions or installing this source read this readme and also do a research, google is your friend. If you mess up when installing our source because you didnt follow it, we will laugh at you. A lot.

## Instructions to contribute ##

### Disclaimer ###
This project is a community project not for commercial use. The emulator itself is proof of concept of our idea to try out anything what's not possible on original servers. The result is to learn and program together for prove the study.

### Legal ###
This Website and Project is in no way affiliated with, authorized, maintained, sponsored or endorsed by Gameforge or any of its affiliates or subsidiaries. This is an independent and unofficial server for educational use ONLY. Using the Project might be against the TOS.

### Editors ###
Any editor that handles .NET 11 works: the build, the tests and every tool here run from the command line on Windows, Linux and macOS. In Visual Studio we recommend:
* [Roslynator extension](https://github.com/JosefPihrt/Roslynator).
* [Resharper](https://www.jetbrains.com/resharper/)
* [SwitchStartupProject extension](https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectForVS2022)


# Building and running #

[documentation/getting-started.md](documentation/getting-started.md) goes from a fresh clone to a character in the world: the game data, the accounts that exist by default and how to point a client at your server.

[documentation/architecture.md](documentation/architecture.md) explains how the servers, the projects and the sibling packages fit together.

In short, from the root of the clone:

```
dotnet run --project src/NosCore.DevHost
```

It builds everything and starts the master, login and world servers together; Ctrl+C stops them. Each can also be started on its own from `build/net11.0`, in any order.

To play Glacernon on a channel of its own, also start `ACT4_CHANNEL=true WORLD_PORT=1338 dotnet NosCore.WorldServer.dll` from `build/net11.0`.

It needs the .NET 11 SDK and a PostgreSQL on `localhost:5432` (user `postgres`, password `password`). On first start the world server downloads the client's data files, creates the database and imports the game data, which takes two to three minutes; it imports again by itself whenever the build or the client files change. Monster, NPC and shop positions come from a packet capture you add yourself, as the guide explains.

On Windows, `scripts\NosCore.cmd` does the same, `scripts\` has a `.cmd` for each server, and `docker-compose.yml` runs the same servers in containers.

### Inter-server messaging ###
Master, world, login and web API servers talk to each other through [Wolverine](https://wolverinefx.net). By default the queues live in the same PostgreSQL database as the game data, so there is nothing extra to install.

The PostgreSQL transport polls its queues (`PollingIntervalMs`, 20 ms by default), so every master round trip costs one to two polling intervals. For production, switch every server to RabbitMQ in its `Messaging` section (or with the `MESSAGING_TRANSPORT` / `RABBITMQ_URI` environment variables):
```yaml
Messaging:
  Transport: RabbitMq
  RabbitMqUri: amqp://user:password@rabbitmq:5672
```
All servers must use the same transport. Queues are created on startup.

## (Optional) Local NuGet packages ##
Every NosCore repo ships a `Directory.Build.props` that adds a NuGet source when the `NOSCORE_LOCAL_PACKAGES` environment variable is set — useful for testing an un-released `NosCore.Packets` / `NosCore.Dao` / `NosCore.Shared` build without publishing it.

One-time setup (run once per machine):
```
setx NOSCORE_LOCAL_PACKAGES C:\LocalPackages
mkdir C:\LocalPackages
```
Restart Visual Studio / your shell so the new variable is picked up.

Drop any `.nupkg` file into that folder, then restore as usual. If the variable is unset the property group is skipped and only nuget.org is used — no action needed for contributors who don't want local packages.

