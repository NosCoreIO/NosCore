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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using ChickenAPI.Packets;
using ChickenAPI.Packets.Interfaces;
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
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClient;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using Serilog;
using ILogger = Serilog.ILogger;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private static LoginConfiguration _loginConfiguration = new LoginConfiguration();

        private static void InitializeConfiguration()
        {
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                .AddYamlFile("login.yml", false)
                .Build()
                .Bind(_loginConfiguration);

            Validator.ValidateObject(_loginConfiguration, new ValidationContext(_loginConfiguration),
                true);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_loginConfiguration.Database.ConnectionString);
            DataAccessHelper.Instance.Initialize(optionsBuilder.Options);

            LogLanguage.Language = _loginConfiguration.Language;
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterInstance(_loginConfiguration!).As<LoginConfiguration>().As<ServerConfiguration>();
            containerBuilder.RegisterType<GenericDao<Account, AccountDto, long>>().As<IGenericDao<AccountDto>>()
                .SingleInstance();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BlacklistHttpClient).Assembly)
                .Where(t => t.Name.EndsWith("HttpClient"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            containerBuilder.Register(c => new Channel
            {
                MasterCommunication = _loginConfiguration!.MasterCommunication,
                ClientType = ServerType.LoginServer,
                ClientName = $"{ServerType.LoginServer}({_loginConfiguration.UserLanguage})",
                Port = _loginConfiguration.Port,
                Host = _loginConfiguration.Host
            });
            foreach (var type in typeof(NoS0575PacketHandler).Assembly.GetTypes())
            {
                if (typeof(IPacketHandler).IsAssignableFrom(type) && typeof(ILoginPacketHandler).IsAssignableFrom(type))
                {
                    containerBuilder.RegisterType(type)
                        .AsImplementedInterfaces()
                        .PropertiesAutowired();
                }
            }

            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => ((p.Namespace == "ChickenAPI.Packets.ServerPackets.Login") ||
                        (p.Namespace == "ChickenAPI.Packets.ClientPackets.Login"))
                    && p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
        }

        public static void Main()
        {
            try
            {
                BuildHost(new string[0]).Run();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IHost BuildHost(string[] _)
        {
            return new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    try { Console.Title = Title; } catch (PlatformNotSupportedException) { }
                    Logger.PrintHeader(ConsoleText);
                    InitializeConfiguration();

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    var containerBuilder = new ContainerBuilder();
                    InitializeContainer(containerBuilder);
                    containerBuilder.Populate(services);
                    var container = containerBuilder.Build();

                    TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                       .AfterMapping(dest => Task.Run(dest.Initialize));
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();


                    Task.Run(() => container.Resolve<LoginServer>().Run());
                })
                .Build();
        }
    }
}