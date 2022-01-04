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

namespace NosCore.Parser
{
    public class Parser : BackgroundService
    {
        private readonly ImportFactory _factory;
        private readonly ILogger _logger;
        public Parser(ImportFactory factory, ILogger logger)
        {
            _factory = factory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ENTER_PATH));
                var key = default(ConsoleKeyInfo);

                var folder = Console.ReadLine();
                _logger.Information(
                    $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ALL)} [Y/n]");
                key = Console.ReadKey(true);

                _factory.SetFolder(folder!);
                await _factory.ImportPacketsAsync().ConfigureAwait(false);

                if (key.KeyChar != 'n')
                {
                    await _factory.ImportAccountsAsync().ConfigureAwait(false);
                    await _factory.ImportMapsAsync().ConfigureAwait(false);
                    await _factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                    await _factory.ImportMapTypeAsync().ConfigureAwait(false);
                    await _factory.ImportMapTypeMapAsync().ConfigureAwait(false);
                    await _factory.ImportPortalsAsync().ConfigureAwait(false);
                    await _factory.ImportI18NAsync().ConfigureAwait(false);
                    //factory.ImportScriptedInstances();
                    await _factory.ImportItemsAsync().ConfigureAwait(false);
                    await _factory.ImportSkillsAsync().ConfigureAwait(false);
                    await _factory.ImportCardsAsync().ConfigureAwait(false);
                    await _factory.ImportNpcMonstersAsync().ConfigureAwait(false);
                    await _factory.ImportDropsAsync().ConfigureAwait(false);
                    //factory.ImportNpcMonsterData();
                    await _factory.ImportMapNpcsAsync().ConfigureAwait(false);
                    await _factory.ImportMapMonstersAsync().ConfigureAwait(false);
                    await _factory.ImportShopsAsync().ConfigureAwait(false);
                    //factory.ImportTeleporters();
                    await _factory.ImportShopItemsAsync().ConfigureAwait(false);
                    //factory.ImportShopSkills();
                    //factory.ImportRecipe();
                    await _factory.ImportScriptsAsync().ConfigureAwait(false);
                    await _factory.ImportQuestsAsync().ConfigureAwait(false);
                }
                else
                {
                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportMapsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPTYPES)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                        await _factory.ImportMapTypeAsync().ConfigureAwait(false);
                        await _factory.ImportMapTypeMapAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ACCOUNTS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportAccountsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_PORTALS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportPortalsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_I18N)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportI18NAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TIMESPACES)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportScriptedInstances();
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ITEMS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportItemsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportNpcMonstersAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_DROPS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportDropsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportNpcMonsterData();
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_CARDS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportCardsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SKILLS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportSkillsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPNPCS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportMapNpcsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MONSTERS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportMapMonstersAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportShopsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TELEPORTERS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportTeleporters();
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPITEMS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportShopItemsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportShopSkills();
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_RECIPES)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        //factory.ImportRecipe();
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SCRIPTS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportScriptsAsync().ConfigureAwait(false);
                    }

                    _logger.Information(
                        $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_QUESTS)} [Y/n]");
                    key = Console.ReadKey(true);
                    if (key.KeyChar != 'n')
                    {
                        await _factory.ImportQuestsAsync().ConfigureAwait(false);
                    }
                }

                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DONE));
                await Task.Delay(5000, stoppingToken);
            }
            catch (FileNotFoundException)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AT_LEAST_ONE_FILE_MISSING));
                throw;
            }
        }
    }
}