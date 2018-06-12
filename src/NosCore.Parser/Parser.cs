using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.Parser
{
	public static class Parser
	{
		private const string _configurationPath = @"..\..\..\configuration";
		private static readonly ParserConfiguration _parserConfiguration = new ParserConfiguration();

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
			builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
			builder.AddJsonFile("parser.json", false);
			builder.Build().Bind(_parserConfiguration);
			LogLanguage.Language = _parserConfiguration.Language;
			Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
		}

		private static void PrintHeader()
		{
			Console.Title = "NosCore - Parser";
			const string text = "PARSER SERVER - 0Lucifer0";
			var offset = Console.WindowWidth / 2 + text.Length / 2;
			var separator = new string('=', Console.WindowWidth);
			Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
		}

		public static void Main(string[] args)
		{
			PrintHeader();
			InitializeLogger();
			InitializeConfiguration();

			try
			{
				DataAccessHelper.Instance.Initialize(_parserConfiguration.Database);

				try
				{
					Logger.Log.Warn(LogLanguage.Instance.GetMessageFromKey(LanguageKey.ENTER_PATH));
					var folder = string.Empty;
					var key = default(ConsoleKeyInfo);
					if (args.Length == 0)
					{
						folder = Console.ReadLine();
						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ALL)} [Y/n]");
						key = Console.ReadKey(true);
					}
					else
					{
						folder = args.Aggregate(folder, (current, str) => current + str + " ");
					}

					var factory = new ImportFactory(folder, _parserConfiguration);
					factory.ImportPackets();

					if (key.KeyChar != 'n')
					{
						factory.ImportMaps();
						//factory.ImportRespawnMapType();
						//factory.ImportMapType();
						//factory.ImportMapTypeMap();
						factory.ImportAccounts();
						factory.ImportPortals();
						factory.ImportI18N();
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
						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportMaps();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPTYPES)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportMapType();
							factory.ImportMapTypeMap();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ACCOUNTS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportAccounts();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_PORTALS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportPortals();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_I18N)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportI18N();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_TIMESPACES)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportScriptedInstances();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_ITEMS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportItems();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportNpcMonsters();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportNpcMonsterData();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_CARDS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportCards();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SKILLS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportSkills();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MAPNPCS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportMapNpcs();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_MONSTERS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportMonsters();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportShops();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_TELEPORTERS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportTeleporters();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPITEMS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportShopItems();
						}

						Console.WriteLine(
							$"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportShopSkills();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_RECIPES)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportRecipe();
						}

						Console.WriteLine($"{LogLanguage.Instance.GetMessageFromKey(LanguageKey.PARSE_QUESTS)} [Y/n]");
						key = Console.ReadKey(true);
						if (key.KeyChar != 'n')
						{
							factory.ImportQuests();
						}
					}

					Console.WriteLine(LogLanguage.Instance.GetMessageFromKey(LanguageKey.DONE));
					Thread.Sleep(5000);
				}
				catch (FileNotFoundException)
				{
					Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AT_LEAST_ONE_FILE_MISSING));
					Thread.Sleep(5000);
				}
			}
			catch (Exception)
			{
				Console.ReadKey();
			}
		}
	}
}