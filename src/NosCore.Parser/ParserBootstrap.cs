//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Core;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Database.Entities.Base;
using NosCore.Parser.Parsers;
using NosCore.Shared.Helpers;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
                builder => builder.UseNpgsql(parserConfiguration.Database.ConnectionString, options => { options.UseNodaTime(); }));
            services.AddOptions<ParserConfiguration>().Bind(conf).ValidateDataAnnotations();
            Logger.GetLoggerConfiguration().CreateLogger();
            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(parserConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>()
                .OnActivated(c => c.Instance.Database.Migrate());
            containerBuilder.RegisterAssemblyTypes(typeof(CardParser).Assembly)
                .Where(t => t.Name.EndsWith("Parser") && !t.IsGenericType)
                .AsSelf();

            containerBuilder.RegisterType<ImportFactory>();
            var registerDatabaseObject = typeof(ParserBootstrap).GetMethod(nameof(RegisterDatabaseObject));

            foreach (var mapping in Database.Hosting.PersistenceModule.DiscoverDaoMappings())
            {
                registerDatabaseObject?.MakeGenericMethod(mapping.DtoType, mapping.DbType, mapping.PkType).Invoke(null,
                    new[] { containerBuilder, (object)mapping.IsStatic });
            }

            containerBuilder.RegisterType<Dao<ItemInstance, IItemInstanceDto?, Guid>>().As<IDao<IItemInstanceDto?, Guid>>()
                .SingleInstance();
        }

        public static async Task Main(string[] args)
        {
            var cli = ParserCliOptions.Parse(args);
            try
            {
                await BuildHost(args, cli).RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (!cli.HasFolder)
                {
                    Console.ReadLine();
                }
            }
        }

        private static IHost BuildHost(string[] args, ParserCliOptions cli)
        {
            return new HostBuilder()
                .UseSerilog()
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    ConsoleHelper.SetTitle(Title);

                    InitializeConfiguration(args, services);

                    services.AddSingleton(cli);
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
