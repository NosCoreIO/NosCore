using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using NosCore.Core.Encryption;
using log4net;
using log4net.Repository;
using log4net.Config;
using NosCore.Core.Logger;
using NosCore.Data;
using NosCore.Database;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.DAL;

namespace NosCore.Parser
{
    public static class Parser
    {
        private static readonly ParserConfiguration _parserConfiguration = new ParserConfiguration();

        private const string _configurationPath = @"..\..\..\configuration";

        private static void InitializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(Parser)));
        }

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("parser.json", false);
            builder.Build().Bind(_parserConfiguration);
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
        }

        private static void PrintHeader()
        {
            Console.Title = "NosCore - Parser";
            const string text = "PARSER SERVER - 0Lucifer0";
            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        public static void Main(string[] args)
        {
            PrintHeader();
            InitializeLogger();
            InitializeConfiguration();
            if (DataAccessHelper.Instance.Initialize(_parserConfiguration.Database))
            {
                try
                {
                    Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ENTER_PATH));
                    string folder = string.Empty;
                    ConsoleKeyInfo key;
                    if (args.Length == 0)
                    {
                        folder = Console.ReadLine();
                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ALL)} [Y/n]");
                        key = Console.ReadKey(true);
                    }
                    else
                    {
                        folder = args.Aggregate(folder, (current, str) => current + str + " ");
                    }

                    ImportFactory factory = new ImportFactory(folder, _parserConfiguration);
                    factory.ImportPackets();

                    if (key.KeyChar != 'n')
                    {
                        factory.ImportMaps();
                        //factory.ImportRespawnMapType();
                        //factory.ImportMapType();
                        //factory.ImportMapTypeMap();
                        factory.ImportAccounts();
                        //factory.ImportPortals();
                        //factory.ImportScriptedInstances();
                        //factory.ImportItems();
                        //factory.ImportSkills();
                        //factory.ImportCards();
                        //factory.ImportNpcMonsters();
                        //factory.ImportNpcMonsterData();
                        //factory.ImportMapNpcs();
                        //factory.ImportMonsters();
                        //factory.ImportShops();
                        //factory.ImportTeleporters();
                        //factory.ImportShopItems();
                        //factory.ImportShopSkills();
                        //factory.ImportRecipe();
                        //factory.ImportQuests();
                    }
                    else
                    {
                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMaps();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPTYPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapType();
                            factory.ImportMapTypeMap();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ACCOUNTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportAccounts();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_PORTALS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportPortals();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TIMESPACES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportScriptedInstances();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportItems();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportNpcMonsters();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportNpcMonsterData();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_CARDS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportCards();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportSkills();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPNPCS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMapNpcs();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportMonsters();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportShops();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TELEPORTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportTeleporters();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportShopItems();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportShopSkills();
                        }

                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_RECIPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportRecipe();
                        }
                        Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_QUESTS)} [Y/n]");
                        key = System.Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            factory.ImportQuests();
                        }
                    }
                    Console.WriteLine(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DONE));
                    Thread.Sleep(5000);
                }
                catch (FileNotFoundException)
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AT_LEAST_ONE_FILE_MISSING));
                    Thread.Sleep(5000);
                }
            }
            else
            {
                Console.ReadKey();
                return;
            }
        }
    }
}