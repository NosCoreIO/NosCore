<p align="center">
  <img src="https://cdn.discordapp.com/attachments/319565884454731795/426892646288457728/N2.png"/>
</p>

# NosCore #
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/78d6f89d028f4b9eb0349e37eb10fbac)](https://www.codacy.com/app/NosCoreIO/NosCore?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=NosCoreIO/NosCore&amp;utm_campaign=Badge_Grade)
[![Travis build status](https://travis-ci.org/NosCoreIO/NosCore.svg?branch=master)](https://travis-ci.org/NosCoreIO/NosCore)

## You like our work ? ##
[![ko-fi](https://www.ko-fi.com/img/donate_sm.png)](https://ko-fi.com/A3562BQV)
or
<a href="https://www.patreon.com/bePatron?u=6503887" data-patreon-widget-type="become-patron-button">Become a Patron!</a>

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

### Contribution is only possible with Visual Studio 2017 ###
We recommend usage of : 
* [Roslynator extension](https://github.com/JosefPihrt/Roslynator).
* [Resharper](https://www.jetbrains.com/resharper/)
* [SwitchStartupProject extension](https://marketplace.visualstudio.com/items?itemName=vs-publisher-141975.SwitchStartupProjectforVS2017)


# Building the code #

## 1. Install or Configure PostgreSQL ##
- PostgreSQL: https://www.postgresql.org/
- Use update-database
- Parse all

## 2. Use the NuGet Package Manager to Update the Database ##
- Go to Tools -> NuGet Package Manager -> Package Manager Console
- Choose Project NosCore.Database
- Type 'update-database' and update the Database
