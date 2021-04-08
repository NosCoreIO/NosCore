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
using DotNetty.Buffers;
using DotNetty.Codecs;
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
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NosCore.Core.MessageQueue;
using ILogger = Serilog.ILogger;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";
        private static ILogger _logger = null!;

        private static void InitializeConfiguration(string[] args)
        {
            _logger = Shared.I18N.Logger.GetLoggerConfiguration().CreateLogger();
            Shared.I18N.Logger.PrintHeader(ConsoleText);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            var loginConfiguration = new LoginConfiguration();
            ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "login.yml" }).Bind(loginConfiguration);
            optionsBuilder.UseNpgsql(loginConfiguration.Database!.ConnectionString);
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<NosCoreContext>().As<DbContext>();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<Dao<Database.Entities.Character, CharacterDto, long>>().As<IDao<CharacterDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<ClientSession>();

            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<PubSubHubClient>().AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterType<AuthHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BlacklistHttpClient).Assembly)
                .Where(t => t.Name.EndsWith("HttpClient"))
                .AsImplementedInterfaces();

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
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(InitializeContainer)
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.Title = Title;
                    }
                    var loginConfiguration = new LoginConfiguration();
                    var configuration =
                        ConfiguratorBuilder.InitializeConfiguration(args, new[] { "logger.yml", "login.yml" });
                    configuration.Bind(loginConfiguration);
                    services.AddOptions<LoginConfiguration>().Bind(configuration).ValidateDataAnnotations();
                    services.AddOptions<ServerConfiguration>().Bind(configuration).ValidateDataAnnotations();
                    services.AddOptions<WebApiConfiguration>().Bind(configuration.GetSection(nameof(LoginConfiguration.MasterCommunication))).ValidateDataAnnotations();
                    InitializeConfiguration(args);

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    services.AddDbContext<NosCoreContext>(
                        conf => conf.UseNpgsql(loginConfiguration.Database!.ConnectionString));
                    services.AddHostedService<LoginServer>();
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }
}