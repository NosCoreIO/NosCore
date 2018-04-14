<p align="center">
  <img src="https://cdn.discordapp.com/attachments/319565884454731795/426892646288457728/N2.png"/>
</p>

# NosCore #
[![Travis build status](https://travis-ci.org/NosCoreIO/NosCore.svg?branch=master)](https://travis-ci.org/NosCoreIO/NosCore)

## Achtung! ##
We are not responsible of any damages caused by bad usage of our source. Please before asking questions or installing this source read this readme and also do a research, google is your friend. If you mess up when installing our source because you didnt follow it, we will laugh at you. A lot.

## Instructions to contribute ##

### Disclaimer ###
This project not for commercial use. The emulator itself is proof of concept.

### Legal ###
This Website and Project is in no way affiliated with, authorized, maintained, sponsored or endorsed by Gameforge or any of its affiliates or subsidiaries. This is an independent project for educational use ONLY. Using the Project might be against the TOS.
# Instructions to contribute #

## Disclaimer ##
This project is a community project not for commercial use. The emulator itself is proof of concept of our idea to try out anything what's not possible on original servers. The result is to learn and program together for prove the study. 

## Legal ##
This Website and Project is in no way affiliated with, authorized, maintained, sponsored or endorsed by Gameforge or any of its affiliates or subsidiaries. This is an independent and unofficial server for educational use ONLY. Using the Project might be against the TOS.

### Contribution is only possible with Visual Studio 2017 and Microsoft SQL Server 2016 ###
We recommend usage of [Roslynator extension](https://github.com/JosefPihrt/Roslynator).

# Building the code #
## 1. Install SSDT For Visual Studio ##
http://go.microsoft.com/fwlink/?LinkID=393520&clcid=0x409

## 2. Install or Configure Microsoft SQL Server 2016 (at least Developer Edition) ##
- Microsoft SQL Server 2016 developer edition: https://www.microsoft.com/en-us/sql-server/sql-server-editions-developers
- Microsoft SQL Server Management Studio (SSMS): https://msdn.microsoft.com/en-us/library/mt238290.aspx
- Installation Tutorial: http://pastebin.com/gRVENLFm

## 3. Use the NuGet Package Manager to Update the Database ##
- Go to Tools -> NuGet Package Manager -> Package Manager Console
- Choose Project NosCore.Database
- Type 'update-database' and update the Database
