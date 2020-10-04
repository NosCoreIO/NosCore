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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NosCore.Core.Configuration;
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
using NosCore.Shared.I18N;

// ReSharper disable LocalizableElement

namespace NosCore.Parser
{
    public static class Parser
    {
        private const string Title = "NosCore - Parser";
        private const string ConsoleText = "PARSER - NosCoreIO";
        private static readonly ILogger Logger = Shared.I18N.Logger.GetLoggerConfiguration().CreateLogger();

        public static void RegisterDatabaseObject<TDto, TDb, TPk>(ContainerBuilder containerBuilder, bool isStatic)
        where TDb : class where TPk : struct
        {
            containerBuilder.RegisterType<Dao<TDb, TDto, TPk>>().As<IDao<TDto, TPk>>().SingleInstance();
            if (isStatic)
            {
                containerBuilder.Register(c => c.Resolve<IDao<TDto, TPk>>().LoadAll().ToList())
                    .As<List<TDto>>()
                    .SingleInstance()
                    .AutoActivate();
            }
        }

        public static async Task Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Title = Title;
            }
            var parserConfiguration = new ParserConfiguration();
            ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "parser.yml" }).Bind(parserConfiguration);
            Shared.I18N.Logger.PrintHeader(ConsoleText);
            LogLanguage.Language = parserConfiguration.Language;
            Logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            TypeAdapterConfig.GlobalSettings.Default.IgnoreAttribute(typeof(I18NFromAttribute));
            TypeAdapterConfig.GlobalSettings.Default
                .IgnoreMember((member, side) => side == MemberSide.Destination && member.Type.GetInterfaces().Contains(typeof(IEntity))
                    || (member.Type.GetGenericArguments().Any() && member.Type.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IEntity))));
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
                optionsBuilder.UseNpgsql(parserConfiguration.Database!.ConnectionString);
                var dataAccess = new DataAccessHelper(() => new NosCoreContext(optionsBuilder.Options));
                try
                {
                    Logger.Warning(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.ENTER_PATH));
                    var folder = string.Empty;
                    var key = default(ConsoleKeyInfo);
                    if (args.Length == 0)
                    {
                        folder = Console.ReadLine();
                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ALL)} [Y/n]");
                        key = Console.ReadKey(true);
                    }
                    else
                    {
                        folder = args.Aggregate(folder, (current, str) => current + str + " ");
                    }

                    var containerBuilder = new ContainerBuilder();
                    containerBuilder.RegisterLogger();
                    containerBuilder.Register<IDbContextBuilder>(c => dataAccess).AsImplementedInterfaces().SingleInstance();
                    containerBuilder.RegisterAssemblyTypes(typeof(CardParser).Assembly)
                        .Where(t => t.Name.EndsWith("Parser") && !t.IsGenericType)
                        .AsSelf()
                        .PropertiesAutowired();
                    containerBuilder.RegisterType<ImportFactory>().PropertiesAutowired();
                    var registerDatabaseObject = typeof(Parser).GetMethod(nameof(RegisterDatabaseObject));
                    var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
                    var assemblyDb = typeof(Account).Assembly.GetTypes();

                    assemblyDto.Where(p =>
                            typeof(IDto).IsAssignableFrom(p) && !p.Name.Contains("InstanceDto") && p.IsClass)
                        .ToList()
                        .ForEach(t =>
                        {
                            var type = assemblyDb.First(tgo =>
                                string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                            var typepk = type.GetProperties()
                                .Where(s => dataAccess.CreateContext().Model.FindEntityType(type)
                                    .FindPrimaryKey().Properties.Select(x => x.Name)
                                    .Contains(s.Name)
                                ).ToArray()[0];
                            registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType).Invoke(null,
                                new[] { containerBuilder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
                        });

                    containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>()
                        .SingleInstance();
                    var container = containerBuilder.Build();
                    var factory = container.Resolve<ImportFactory>();
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
                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportMapsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPTYPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportRespawnMapTypeAsync().ConfigureAwait(false);
                            await factory.ImportMapTypeAsync().ConfigureAwait(false);
                            await factory.ImportMapTypeMapAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ACCOUNTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportAccountsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_PORTALS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportPortalsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_I18N)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportI18NAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TIMESPACES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportScriptedInstances();
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_ITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportItemsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportNpcMonstersAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_DROPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportDropsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_NPCMONSTERDATA)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportNpcMonsterData();
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_CARDS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportCardsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportSkillsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MAPNPCS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportMapNpcsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_MONSTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportMapMonstersAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportShopsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_TELEPORTERS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportTeleporters();
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPITEMS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportShopItemsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SHOPSKILLS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportShopSkills();
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_RECIPES)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            //factory.ImportRecipe();
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_SCRIPTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportScriptsAsync().ConfigureAwait(false);
                        }

                        Logger.Information(
                            $"{LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.PARSE_QUESTS)} [Y/n]");
                        key = Console.ReadKey(true);
                        if (key.KeyChar != 'n')
                        {
                            await factory.ImportQuestsAsync().ConfigureAwait(false);
                        }
                    }

                    Logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.DONE));
                    Thread.Sleep(5000);
                }
                catch (FileNotFoundException)
                {
                    Logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AT_LEAST_ONE_FILE_MISSING));
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
                Console.ReadKey();
            }
        }
    }
}