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
using System.ComponentModel.DataAnnotations;
using System.IO;
using Autofac;
using AutofacSerilogIntegration;
using DotNetty.Buffers;
using DotNetty.Codecs;
using FastExpressionCompiler;
using Mapster;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities;
using NosCore.Database.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using NosCore.GameObject;
using NosCore.PacketHandlers;
using NosCore.PacketHandlers.Login;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private static LoginConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var loginConfiguration = new LoginConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("login.json", false);
            builder.Build().Bind(loginConfiguration);
            Validator.ValidateObject(loginConfiguration, new ValidationContext(loginConfiguration),
                validateAllProperties: true);
            LogLanguage.Language = loginConfiguration.Language;
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            return loginConfiguration;
        }

        public static void Main()
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            var container = InitializeContainer();
            var loginServer = container.Resolve<LoginServer>();
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
            try
            {
                loginServer.Run();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterType<GenericDao<Account, AccountDto>>().As<IGenericDao<AccountDto>>().SingleInstance();
            containerBuilder.RegisterInstance(InitializeConfiguration()).As<LoginConfiguration>()
                .As<ServerConfiguration>();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();

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
                .Where(p => (p.Namespace == "ChickenAPI.Packets.ServerPackets.Login" || p.Name == "NoS0575Packet") 
                    && p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            return containerBuilder.Build();
        }
    }
}