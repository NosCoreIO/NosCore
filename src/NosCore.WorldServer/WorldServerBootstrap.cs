//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Autofac;
using Autofac.Extensions.DependencyInjection;
using FastExpressionCompiler;
using FastMember;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Algorithm.ExperienceService;
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Observability;
using NosCore.Core.Services.IdService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data;
using NosCore.Data.CommandPackets;
using NosCore.Data.DataAttributes;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Resource;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.Database.Entities.Base;
using NosCore.Database.Hosting;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Hosting.Modules;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.ScheduledJobs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ChannelCommunicationService.Handlers;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.PathfindingService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.Encoding.Filter;
using NosCore.Networking.SessionGroup;
using NosCore.Networking.SessionRef;
using NosCore.PacketHandlers.Login;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.PathFinder.Heuristic;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using Wolverine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace NosCore.WorldServer
{
    public static class WorldServerBootstrap
    {
        private const string Title = "NosCore - WorldServer";
        private const string ConsoleText = "WORLD SERVER - NosCoreIO";

        private static void InitializeConfiguration(string[] args, IServiceCollection services)
        {
            var worldConfiguration = new WorldConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "world.yml" });
            conf.Bind(worldConfiguration);
            services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(worldConfiguration.Database.ConnectionString, options => { options.UseNodaTime(); }));
            services.AddOptions<WorldConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(LoginConfiguration.MasterCommunication))).ValidateDataAnnotations();

            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(worldConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterModule(new PersistenceModule((b, t) =>
            {
                foreach (var tgo in typeof(MapWorld).Assembly.GetTypes().Where(t.IsAssignableFrom))
                {
                    b.RegisterType(tgo);
                    b.RegisterType(typeof(GameObjectMapper<,>).MakeGenericType(t, tgo))
                        .As(typeof(IGameObjectMapper<>).MakeGenericType(t))
                        .AutoActivate();
                }
            }));
            containerBuilder.RegisterType<MapsterMapper.Mapper>().AsImplementedInterfaces();
            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && (p.GetCustomAttribute<PacketHeaderAttribute>() == null
                    || (p.GetCustomAttribute<PacketHeaderAttribute>()!.Scopes & Scope.OnLoginScreen) == 0) && p.IsClass && !p.IsAbstract).ToList();
            listofpacket.AddRange(typeof(HelpPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList());
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();

            //NosCore.Configuration
            containerBuilder.RegisterAssemblyTypes(typeof(ChannelHubClient).Assembly)
                .Where(t => t.Name.EndsWith("HubClient") && t.Name != nameof(ChannelHubClient))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.RegisterType<ChannelHubClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<HubConnectionFactory>();

            containerBuilder.Register(c =>
            {
                var configuration = c.Resolve<IOptions<WorldConfiguration>>();
                return new Channel
                {
                    MasterCommunication = configuration.Value.MasterCommunication,
                    ClientName = configuration.Value.ServerName!,
                    ClientType = ServerType.WorldServer,
                    ConnectedAccountLimit = configuration.Value.ConnectedAccountLimit,
                    Port = configuration.Value.Port,
                    DisplayPort = configuration.Value.DisplayPort,
                    DisplayHost = configuration.Value.DisplayHost,
                    ServerId = configuration.Value.ServerId,
                    StartInMaintenance = configuration.Value.StartInMaintenance,
                    Host = configuration.Value.Host!,
                };
            });
            containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<WebApiConfiguration>>().Value.HashingType switch
            {
                HashingType.BCrypt => new BcryptHasher(),
                HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                _ => new Sha512Hasher()
            });

            //NosCore.Controllers
            containerBuilder.RegisterTypes(typeof(NoS0575PacketHandler).Assembly.GetTypes()
                    .Where(type => typeof(IPacketHandler).IsAssignableFrom(type) && typeof(IWorldPacketHandler).IsAssignableFrom(type)).ToArray())
                .AsImplementedInterfaces();

            //NosCore.Core
            containerBuilder.RegisterModule<NetworkingModule>();
            containerBuilder.RegisterType<WorldDecoder>().AsImplementedInterfaces();
            containerBuilder.RegisterType<WorldEncoder>().AsImplementedInterfaces();
            containerBuilder.Register(x => new List<IRequestFilter>()).As<IEnumerable<IRequestFilter>>();
            containerBuilder.RegisterType<WorldPacketHandlingStrategy>().As<IPacketHandlingStrategy>().SingleInstance();
            containerBuilder.RegisterAssemblyTypes(typeof(ISessionDisconnectHandler).Assembly)
                .Where(t => typeof(ISessionDisconnectHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .AsImplementedInterfaces();
            containerBuilder.Register(c =>
            {
                var conf = c.Resolve<IOptions<WorldConfiguration>>();
                return new PipelineConfiguration { UseDelimiter = true, Language = conf.Value.Language };
            }).As<IPipelineConfiguration>().SingleInstance();


            // IIdService / pathfinder heuristic / session + map registries / hub clients /
            // combat pipeline are all registered in IServiceCollection by
            // WolverineDependencyRegistrar.RegisterDependencies so Wolverine can see them
            // at codegen time. AutofacServiceProviderFactory.Populate() copies those
            // registrations into the Autofac container at host-build, which is why they
            // aren't duplicated here — registering them on both sides would produce the
            // "An item with the same key..." runtime errors (see PersistenceModule.MirrorTo
            // comment for the same pattern on the DAO side).

            containerBuilder.RegisterAssemblyTypes(typeof(MapWorld).Assembly)
                .Where(t => typeof(IDto).IsAssignableFrom(t))
                .AsSelf();

            containerBuilder
                .RegisterAssemblyTypes(typeof(ChannelCommunicationMessageHandler<>).Assembly)
                .Where(t => typeof(IChannelCommunicationMessageHandler<NosCore.GameObject.InterChannelCommunication.Messages.IMessage>).IsAssignableFrom(t))
                .SingleInstance()
                .AsImplementedInterfaces();
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
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseNosCoreWolverine("NosCore.WorldServer", typeof(NoS0575PacketHandler).Assembly)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.Title = Title;
                    }

                    InitializeConfiguration(args, services);

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddNosCoreTelemetry("NosCore.WorldServer");

                    services.AddI18NLogs();
                    services.AddTransient(typeof(IGameLanguageLocalizer), typeof(GameLanguageLocalizer));
                    services.AddTransient(typeof(ILogLanguageLocalizer<LanguageKey>),
                        x => new LogLanguageLocalizer<LanguageKey, LocalizedResources>(
                            x.GetRequiredService<IStringLocalizer<LocalizedResources>>()));

                    // Cross-cutting singletons that don't belong to any module. Logger is
                    // wired here because it needs a captured Log.Logger reference; IClock
                    // because SystemClock is an external NodaTime type with no container-
                    // friendly constructor.
                    services.AddSingleton<Serilog.ILogger>(_ => Log.Logger);
                    services.AddSingleton<IClock>(_ => SystemClock.Instance);

                    // ID services, session/map registries, hub clients, pathfinder and the
                    // combat pipeline live behind a single registrar. Registering them once
                    // in IServiceCollection — Autofac populates from it — keeps Wolverine
                    // codegen and Autofac runtime resolution on the same view of the world.

                    // Scan the Algorithm package for *Service classes. GameObject-side
                    // *Service/*Queue/*Ai/etc. are handled by the registrar below
                    // with lifetime driven off the ISingletonService marker.
                    foreach (var implType in typeof(IExperienceService).Assembly.GetTypes()
                                 .Where(t => t.Name.EndsWith("Service") && t.IsClass && !t.IsAbstract))
                    {
                        foreach (var iface in implType.GetInterfaces())
                        {
                            services.AddTransient(iface, implType);
                        }
                        services.AddTransient(implType);
                    }

                    NosCore.Database.Hosting.PersistenceModule.MirrorTo(services);
                    NosCore.GameObject.Messaging.WolverineDependencyRegistrar.RegisterDependencies(services);

                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.AddHostedService<WorldServer>();
                    services.AddHostedService(sp => new RecurringMessagePublisher<SaveAllSessionsMessage>(
                        sp.GetRequiredService<IMessageBus>(),
                        sp.GetRequiredService<ILogger<RecurringMessagePublisher<SaveAllSessionsMessage>>>(),
                        TimeSpan.FromMinutes(5)));
                    services.AddHostedService(sp => new RecurringMessagePublisher<RemoveTimeoutStaticBonusesMessage>(
                        sp.GetRequiredService<IMessageBus>(),
                        sp.GetRequiredService<ILogger<RecurringMessagePublisher<RemoveTimeoutStaticBonusesMessage>>>(),
                        TimeSpan.FromMinutes(5)));

                    TypeAdapterConfig.GlobalSettings.AllowImplicitSourceInheritance = false;
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<IPacket>().Ignore(s => s.ValidationResult);
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<I18NString>().BeforeMapping(s => s.Clear());
                    TypeAdapterConfig.GlobalSettings.Default.IgnoreMember((member, side)
                        => ((side == MemberSide.Destination) && member.Type.GetInterfaces().Contains(typeof(IEntity))) || (member.Type.GetGenericArguments().Any() && member.Type.GetGenericArguments()[0].GetInterfaces().Contains(typeof(IEntity))));
                    TypeAdapterConfig.GlobalSettings.When(s => !s.SourceType.IsAssignableFrom(s.DestinationType))
                        .IgnoreMember((member, side) => typeof(I18NString).IsAssignableFrom(member.Type));
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }
}
