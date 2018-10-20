//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.DAL;
using NosCore.Database;
using NosCore.Shared.I18N;

// ReSharper disable LocalizableElement

namespace NosCore.Parser
{
    public static class Parser
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - Parser";
        private const string ConsoleText = "PARSER SERVER - NosCoreIO";
        private static readonly ParserConfiguration ParserConfiguration = new ParserConfiguration();

        private static void InitializeLogger()
        {
            // LOGGER
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(Parser)));
        }

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("parser.json", false);
            builder.Build().Bind(ParserConfiguration);
            LogLanguage.Language = ParserConfiguration.Language;
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
        }

        public static void Main(string[] args)
        {
            Console.Title = Title;
            InitializeLogger();
            Logger.PrintHeader(ConsoleText);
            InitializeConfiguration();
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(ParserConfiguration.Database.ConnectionString);
                DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

                try
                {
                    Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ENTER_PATH));
                    var folder = string.Empty;
                    var key = default(ConsoleKeyInfo);
                    if (args.Length == 0)
                    {
                        folder = Console.ReadLine();
                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ALL)} [Y/n]");
                        key = Console.ReadKey(true);
                    }
                    else
                    {
                        folder = args.Aggregate(folder, (current, str) => current + str + " ");
                    }

                    var factory = new ImportFactory(folder);
                    factory.ImportPackets();

                    if (key.KeyChar != 'n')
                    {
                        factory.ImportMaps();
                        factory.ImportRespawnMapType();
                        factory.ImportMapType();
                        factory.ImportMapTypeMap();
                        factory.ImportAccounts();
                        factory.ImportPortals();
                        factory.ImportI18N();
                        //factory.ImportScriptedInstances();
                        factory.ImportItems();
                        factory.ImportSkills();
                        factory.ImportCards();
                        factory.ImportNpcMonsters();
                        factory.ImportDrops();
                        //factory.ImportNpcMonsterData();
                        factory.ImportMapNpcs();
                        factory.ImportMapMonsters();
                        //factory.ImportShops();
                        //factory.ImportTeleporters();
                        //factory.ImportShopItems();
                        //factory.ImportShopSkills();
                        //factory.ImportRecipe();
                        //factory.ImportQuests();
                    }
                    else
                    {
                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMaps();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPTYPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportRespawnMapType();
                            factory.ImportMapType();
                            factory.ImportMapTypeMap();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ACCOUNTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportAccounts();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_PORTALS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportPortals();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_I18N)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportI18N();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_TIMESPACES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportScriptedInstances();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportItems();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportNpcMonsters();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_DROPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportDrops();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportNpcMonsterData();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_CARDS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportCards();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportSkills();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPNPCS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapNpcs();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapMonsters();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportShops();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_TELEPORTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportTeleporters();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportShopItems();
                        }

                        Logger.Log.Info(
                            $"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportShopSkills();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_RECIPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportRecipe();
                        }

                        Logger.Log.Info($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_QUESTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportQuests();
                        }
                    }

                    Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.DONE));
                    Thread.Sleep(5000);
                }
                catch (FileNotFoundException)
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AT_LEAST_ONE_FILE_MISSING));
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex);
                Console.ReadKey();
            }
        }
    }
}