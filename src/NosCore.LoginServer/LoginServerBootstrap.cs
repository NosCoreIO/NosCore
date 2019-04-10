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
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database.Entities;
using NosCore.Database.DAL;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;
using ChickenAPI.Packets.ClientPackets.Login;

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

        private static void InitializePackets()
        {
            PacketFactory.Initialize<NoS0575Packet>();
        }

        public static void Main()
        {
            Console.Title = Title;
            Logger.PrintHeader(ConsoleText);
            InitializePackets();
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
            containerBuilder.RegisterType<GenericDao<Account, AccountDto>>().As<IGenericDao<AccountDto>>().SingleInstance();
            containerBuilder.RegisterInstance(InitializeConfiguration()).As<LoginConfiguration>()
                .As<ServerConfiguration>();
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();

            return containerBuilder.Build();
        }
    }
}