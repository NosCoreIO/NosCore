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
using NosCore.Core;
using NosCore.Core.Configuration;
using NosCore.Core.Encryption;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Login;
using NosCore.Packets;
using NosCore.Packets.Attributes;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.GameObject.InterChannelCommunication;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.Services.LoginService;
using NosCore.Networking;
using NosCore.Networking.Encoding;
using NosCore.Networking.Encoding.Filter;
using NosCore.Networking.SessionRef;
using NosCore.Shared.I18N;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";

        private static void InitializeConfiguration(string[] args, IServiceCollection services)
        {
            var loginConfiguration = new LoginConfiguration();
            var conf = ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "login.yml" });
            conf.Bind(loginConfiguration);
            services.AddDbContext<NosCoreContext>(
                conf => conf.UseNpgsql(loginConfiguration.Database!.ConnectionString, options => { options.UseNodaTime(); }));
            services.AddOptions<LoginConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<ServerConfiguration>().Bind(conf).ValidateDataAnnotations();
            services.AddOptions<WebApiConfiguration>().Bind(conf.GetSection(nameof(LoginConfiguration.MasterCommunication))).ValidateDataAnnotations();

            Logger.PrintHeader(ConsoleText);
            CultureInfo.DefaultThreadCurrentCulture = new(loginConfiguration.Language.ToString());
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<Dao<Database.Entities.Character, CharacterDto, long>>().As<IDao<CharacterDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<LoginDecoder>().AsImplementedInterfaces();
            containerBuilder.RegisterType<LoginEncoder>().AsImplementedInterfaces();

            containerBuilder.RegisterType<ClientSession>().AsImplementedInterfaces();
            containerBuilder.RegisterType<SessionRefHolder>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>().AsImplementedInterfaces();
            containerBuilder.RegisterType<SpamRequestFilter>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.Register(_ => SystemClock.Instance).As<IClock>().SingleInstance();
            containerBuilder.RegisterType<HubConnectionFactory>();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHubClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.Register<IIdService<ChannelInfo>>(_ => new IdService<ChannelInfo>(1)).SingleInstance();

            containerBuilder.Register<IHasher>(o => o.Resolve<IOptions<LoginConfiguration>>().Value.MasterCommunication.HashingType switch
            {
                HashingType.BCrypt => new BcryptHasher(),
                HashingType.Pbkdf2 => new Pbkdf2Hasher(),
                _ => new Sha512Hasher()
            });
            containerBuilder.Register(c =>
            {
                var conf = c.Resolve<IOptions<LoginConfiguration>>();
                return new Channel
                {
                    MasterCommunication = conf.Value!.MasterCommunication,
                    ClientType = ServerType.LoginServer,
                    ClientName = $"{ServerType.LoginServer}",
                    Port = conf.Value.Port,
                    Host = conf.Value.Host!
                };
            });

            containerBuilder.RegisterTypes(typeof(NoS0575PacketHandler).Assembly.GetTypes().Where(type => typeof(IPacketHandler).IsAssignableFrom(type) && typeof(ILoginPacketHandler).IsAssignableFrom(type)).ToArray())
                .Where(t => typeof(IPacketHandler).IsAssignableFrom(t) && typeof(ILoginPacketHandler).IsAssignableFrom(t))
                .AsImplementedInterfaces();

            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => p.GetInterfaces().Contains(typeof(IPacket)) && (p.GetCustomAttribute<PacketHeaderAttribute>() == null || (p.GetCustomAttribute<PacketHeaderAttribute>()!.Scopes & Scope.OnLoginScreen) != 0) && p.IsClass && !p.IsAbstract).ToList();
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
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