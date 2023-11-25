<p align="center">
  <img src="https://cdn.discordapp.com/attachments/319565884454731795/426892646288457728/N2.png"/>
</p>

# NosCore #
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/643fd3df0ce742ec9b2ac3dab95bdc44)](https://www.codacy.com/gh/NosCoreIO/NosCore/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NosCoreIO/NosCore&amp;utm_campaign=Badge_Grade)
[![.NET](https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/NosCoreIO/NosCore/actions/workflows/dotnet.yml)
[![Total alerts](https://img.shields.io/lgtm/alerts/g/NosCoreIO/NosCore.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/NosCoreIO/NosCore/alerts/)

# Special Thanks for Contributions #
<p align="left">
<a href="https://aws.amazon.com"><img height="100px" src="https://chiefit.me/wp-content/uploads/2019/06/Amazon-Web-Services_logo835x396.png"/></a>
<a href="https://www.navicat.com"><img height="100px" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>
<a href="https://www.jetbrains.com"><img height="100px" src="https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/JetBrains_Logo_2016.svg/1200px-JetBrains_Logo_2016.svg.png"/></a>
</p>

## You want to contribute ? ##
[![Discord](https://i.gyazo.com/2115a3ecb258220f5b1a8ebd8c50eb8f.png)](https://discord.gg/Eu3ETSw)

## You like our work ? ##
<a href='https://github.com/sponsors/0Lucifer0' target='_blank'><img height='48' style='border:0px;height:46px;' src='https://i.gyazo.com/47b2ca2eb6e1ce38d02b04c410e1c82a.png' border='0' alt='Sponsor me!' /></a>
<a href='https://ko-fi.com/A3562BQV' target='_blank'><img height='46' style='border:0px;height:46px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a> 
<a href='https://www.patreon.com/bePatron?u=6503887' target='_blank'><img height='46' style='border:0px;height:46px;' src='https://c5.patreon.com/external/logo/become_a_patron_button@2x.png' border='0' alt='Become a Patron!' /></a>

## Achtung! ##
We are not responsible of any damages caused by bad usage of our source. Please before asking questions or installing this source read this readme and also do a research, google is your friend. If you mess up when installing our source because you didnt follow it, we will laugh at you. A lot.

## Instructions to contribute ##

### Disclaimer ###
This project not for commercial use. The emulator itself is proof of concept.

### Legal ###
This Website and Project is in no way affiliated with, authorized, maintained, sponsored or endorsed by Gameforge or any of its affiliates or subsidiaries. This is an independent and unofficial server for educational use ONLY. Using the Project might be against the TOS.
# Instructions to contribute #

## Disclaimer ##
This project is a community project not for commercial use. The emulator itself is proof of concept of our idea to try out anything what's not possible on original servers. The result is to learn and program together for prove the study. 

### Contribution is only possible with Visual Studio 2022 ###
We recommend usage of : 
* [Roslynator extension](https://github.com/JosefPihrt/Roslynator).
* [Resharper](https://www.jetbrains.com/resharper/)
* [SwitchStartupProject extension](https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectForVS2022)


# Building the code #

## 1. Install .Net 8 ##
- https://dotnet.microsoft.com/download/dotnet/8.0
- Visual Studio > Tools > Options > Preview Features > Use previews of the .NET CORE SDK
- Restart

## 2. Install or Configure PostgreSQL ##
- PostgreSQL: https://www.postgresql.org/
- Use update-database
- Parse all

## 3. Use the NuGet Package Manager to Update the Database ##
- Go to Tools -> NuGet Package Manager -> Package Manager Console
- Choose Project NosCore.Database
- Type 'update-database' and update the Database

## 4. Start services ##
- script to start services are in .\scripts 

