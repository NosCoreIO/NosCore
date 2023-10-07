//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Autofac;
using AutofacSerilogIntegration;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Core.I18N;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Database.Entities.Base;
using NosCore.Parser.Parsers;
using NosCore.Shared.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NosCore.Shared.I18N;

namespace NosCore.Parser
{
    public class Parser(ImportFactory factory, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.Warning(logLanguage[LogLanguageKey.ENTER_PATH]);
                var key = default(ConsoleKeyInfo);

                var folder = Console.ReadLine();
                logger.Information(
                    $"{logLanguage[LogLanguageKey.PARSE_ALL]} [Y/n]");
                key = Console.ReadKey(true);

                factory.SetFolder(folder!);
                await factory.ImportPacketsAsync().ConfigureAwait(false);

                if (key.KeyChar != 'n')
                {
                    await factory.ImportAccountsAsync().ConfigureAwait(false);
                    await factory.ImportMapsAsync().ConfigureAwait(false);
                    await factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                    await factory.ImportMapTypeAsync().ConfigureAwait(false);
                    await factory.ImportMapTypeMapAsync().ConfigureAwait(false);
                    await factory.ImportPortalsAsync().ConfigureAwait(false);
                    await factory.ImportI18NAsync().ConfigureAwait(false);
                    //factory.ImportScriptedInstances();
                    await factory.ImportItemsAsync().ConfigureAwait(false);
                    await factory.ImportSkillsAsync().ConfigureAwait(false);
                    await factory.ImportCardsAsync().ConfigureAwait(false);
                    await factory.ImportNpcMonstersAsync().ConfigureAwait(false);
                    await factory.ImportDropsAsync().ConfigureAwait(false);
                    //factory.ImportNpcMonsterData();
                    await factory.ImportMapNpcsAsync().ConfigureAwait(false);
                    await factory.ImportMapMonstersAsync().ConfigureAwait(false);
                    await factory.ImportShopsAsync().ConfigureAwait(false);
                    //factory.ImportTeleporters();
                    await factory.ImportShopItemsAsync().ConfigureAwait(false);
                    //factory.ImportShopSkills();
                    //factory.ImportRecipe();
                    await factory.ImportScriptsAsync().ConfigureAwait(false);
                    await factory.ImportQuestsAsync().ConfigureAwait(false);
                }
                else
                {
                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPTYPES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                        await factory.ImportMapTypeAsync().ConfigureAwait(false);
                        await factory.ImportMapTypeMapAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_ACCOUNTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportAccountsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_PORTALS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportPortalsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_I18N]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportI18NAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_TIMESPACES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportScriptedInstances();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_ITEMS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportItemsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportNpcMonstersAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_DROPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportDropsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_NPCMONSTERDATA]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportNpcMonsterData();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_CARDS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportCardsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SKILLS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportSkillsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MAPNPCS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapNpcsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_MONSTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportMapMonstersAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportShopsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_TELEPORTERS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportTeleporters();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPITEMS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportShopItemsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SHOPSKILLS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportShopSkills();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_RECIPES]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportRecipe();
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_SCRIPTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportScriptsAsync().ConfigureAwait(false);
                    }

                    logger.Information(
                        $"{logLanguage[LogLanguageKey.PARSE_QUESTS]} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await factory.ImportQuestsAsync().ConfigureAwait(false);
                    }
                }

                logger.Information(logLanguage[LogLanguageKey.DONE]);
                await Task.Delay(5000, stoppingToken);
            }
            catch (FileNotFoundException)
            {
                logger.Error(logLanguage[LogLanguageKey.AT_LEAST_ONE_FILE_MISSING]);
                throw;
            }
        }
    }
}