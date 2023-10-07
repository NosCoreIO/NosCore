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
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Shared.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NosCore.Database.Entities.Base;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;

namespace NosCore.Parser
{
    public static class ParserBootstrap
    {
        private const string Title = "NosCore - Parser";
        private const string ConsoleText = "PARSER - NosCoreIO";

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

        private static void InitializeConfiguration(string[] args, IServiceCollection services)
        {
            var parserConfiguration = new ParserConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "parser.yml" });
            conf.Bind(parserConfiguration);
            services.AddDbContext<NosCoreContext>(
                builder => builder.UseNpgsql(parserConfiguration.Database!.ConnectionString, options => { options.UseNodaTime(); }));
            services.AddOptions<ParserConfiguration>().Bind(conf).ValidateDataAnnotations();
            Logger.GetLoggerConfiguration().CreateLogger();
            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(parserConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>()
                .OnActivated(c => c.Instance.Database.Migrate());
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterAssemblyTypes(typeof(CardParser).Assembly)
                .Where(t => t.Name.EndsWith("Parser") && !t.IsGenericType)
                .AsSelf();

            containerBuilder.RegisterType<ImportFactory>();
            var registerDatabaseObject = typeof(ParserBootstrap).GetMethod(nameof(RegisterDatabaseObject));
            var assemblyDto = typeof(IStaticDto).Assembly.GetTypes();
            var assemblyDb = typeof(Account).Assembly.GetTypes();

            assemblyDto.Where(p =>
                    typeof(IDto).IsAssignableFrom(p) && !p.Name.Contains("InstanceDto") && p.IsClass)
                .ToList()
                .ForEach(t =>
                {
                    var type = assemblyDb.First(tgo =>
                        string.Compare(t.Name, $"{tgo.Name}Dto", StringComparison.OrdinalIgnoreCase) == 0);
                    var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                        Guid.NewGuid().ToString());
                    var typepk = type.GetProperties()
                        .Where(s => new NosCoreContext(optionsBuilder.Options).Model.FindEntityType(type)?
                            .FindPrimaryKey()?.Properties.Select(x => x.Name)
                            .Contains(s.Name) ?? false
                        ).ToArray()[0];
                    registerDatabaseObject?.MakeGenericMethod(t, type, typepk!.PropertyType).Invoke(null,
                        new[] { containerBuilder, (object)typeof(IStaticDto).IsAssignableFrom(t) });
                });

            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>()
                .SingleInstance();
        }

        public static async Task Main(string[] args)
        {
            try
            {
                await BuildHost(args).RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                .UseSerilog()
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.Title = Title;
                    }

                    InitializeConfiguration(args, services);

                    services.AddI18NLogs();
                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    services.AddHostedService<Parser>();

                    TypeAdapterConfig.GlobalSettings.Default.IgnoreAttribute(typeof(I18NFromAttribute));
                    TypeAdapterConfig.GlobalSettings.Default
                        .IgnoreMember((member, side) => side == MemberSide.Destination && member.Type.GetInterfaces().Contains(typeof(IEntity))
                            || (member.Type.GetGenericArguments().Any() && member.Type.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IEntity))));
                })
                .Build();
        }
    }
}