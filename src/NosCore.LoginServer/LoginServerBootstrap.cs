using System;
using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string ConfigurationPath = @"../../../configuration";
        private const string Title = "NosCore - LoginServer";

        private static void PrintHeader()
        {
            Console.Title = Title;
            const string text = "LOGIN SERVER - 0Lucifer0";
            var offset = Console.WindowWidth / 2 + text.Length / 2;
            var separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private static LoginConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var loginConfiguration = new LoginConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath);
            builder.AddJsonFile("login.json", false);
            builder.Build().Bind(loginConfiguration);
            LogLanguage.Language = loginConfiguration.Language;
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
            return loginConfiguration;
        }

        private static void InitializeLogger()
        {
            // LOGGER
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(LoginServer)));
        }

        private static void InitializePackets()
        {
            PacketFactory.Initialize<NoS0575Packet>();
        }

        public static void Main()
        {
            PrintHeader();
            InitializeLogger();
            InitializePackets();
            var container = InitializeContainer();
            DependancyResolver.Init(new AutofacServiceProvider(container));
            var loginServer = container.Resolve<LoginServer>();
            loginServer.Run();
        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(InitializeConfiguration()).As<LoginConfiguration>().As<GameServerConfiguration>();
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<string>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();
            containerBuilder.RegisterType<NetworkManager>();

            return containerBuilder.Build();
        }
    }
}