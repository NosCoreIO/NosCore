//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastExpressionCompiler;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.Observability;
using NosCore.Core.Services.IdService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Hosting.Modules;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.LoginService;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.Encoding.Filter;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.Generated;
using NosCore.PacketHandlers.Login;
using NosCore.Packets;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Helpers;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";

        private static void InitializeConfiguration(string[] args, IServiceCollection services)
        {
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "login.yml" });
            var loginConfiguration = conf.Get<LoginConfiguration>()
                ?? throw new InvalidOperationException("The LoginConfiguration section could not be read.");
            DatabaseReadiness.WaitUntilUpToDate(loginConfiguration.Database.ConnectionString);
            services.AddNosCoreContext(loginConfiguration.Database.ConnectionString);
            services.AddOptions<LoginConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(LoginConfiguration.MasterCommunication))).ValidateDataAnnotations();

            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(loginConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterModule<NetworkingModule>();

            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<Dao<Database.Entities.Character, CharacterDto, long>>().As<IDao<CharacterDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<Dao<PenaltyLog, PenaltyLogDto, int>>().As<IDao<PenaltyLogDto, int>>()
                .SingleInstance();
            containerBuilder.RegisterType<PenaltyService>().As<IPenaltyService>();
            containerBuilder.RegisterType<LoginDecoder>().AsImplementedInterfaces();
            containerBuilder.RegisterType<LoginEncoder>().AsImplementedInterfaces();

            containerBuilder.RegisterType<LoginPacketHandlingStrategy>().As<IPacketHandlingStrategy>().SingleInstance();
            containerBuilder.Register(_ => Enumerable.Empty<ISessionDisconnectHandler>()).As<IEnumerable<ISessionDisconnectHandler>>();
            containerBuilder.RegisterType<NosCore.GameObject.Services.AuthService.AuthCodeService>().As<NosCore.GameObject.Services.AuthService.IAuthCodeService>().SingleInstance();
            containerBuilder.Register(c =>
            {
                var conf = c.Resolve<IOptions<LoginConfiguration>>();
                return new PipelineConfiguration { UseDelimiter = false, Language = conf.Value.Language };
            }).As<IPipelineConfiguration>().SingleInstance();

            containerBuilder.RegisterType<SpamRequestFilter>().SingleInstance().AsImplementedInterfaces();

            containerBuilder.Register(_ => SystemClock.Instance).As<IClock>().SingleInstance();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHubClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<PubSubHubClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthHubClient>().AsImplementedInterfaces();
            containerBuilder.Register<IIdService<ChannelInfo>>(_ => new IdService<ChannelInfo>(1)).SingleInstance();

            containerBuilder.RegisterHasher();
            containerBuilder.Register(c =>
            {
                var conf = c.Resolve<IOptions<LoginConfiguration>>();
                return new Channel
                {
                    MasterCommunication = conf.Value.MasterCommunication,
                    ClientType = ServerType.LoginServer,
                    ClientName = $"{ServerType.LoginServer}",
                    Port = conf.Value.Port,
                    Host = conf.Value.Host ?? throw new InvalidOperationException("Host is not configured.")
                };
            });

            containerBuilder.RegisterTypes(GeneratedPacketHandlers.LoginHandlerTypes)
                .AsImplementedInterfaces();

            containerBuilder.RegisterPacketSerializers(PacketTypes.LoginScreen());
        }

        public static async Task Main(string[] args)
        {
            try
            {
                await BuildHost(args).RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static IHost BuildHost(string[] args)
        {
            var configuration = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "login.yml" });
            var server = configuration.Get<ServerConfiguration>() ?? new ServerConfiguration();
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseNosCoreWolverine(configuration, InterChannelNode.For("NosCore.LoginServer", "login", server.Host, server.Port),
                    opts => opts.Discovery.DisableConventionalDiscovery())
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    ConsoleHelper.SetTitle(Title);

                    InitializeConfiguration(args, services);
                    services.AddI18NLogs();
                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddNosCoreTelemetry("NosCore.LoginServer");

                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    services.AddHostedService<LoginServer>();
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }
}
