using Autofac;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Logger;
using NosCore.Core.Serializing;
using NosCore.Packets.ClientPackets;
using System;
using System.IO;
using System.Reflection;

namespace NosCore.LoginServer
{
    public static class WorldServerBootstrap
    {
        private const string _configurationPath = @"..\..\..\configuration";

        private static void PrintHeader()
        {
            Console.Title = "NosCore - LoginServer";
            const string text = "LOGIN SERVER - 0Lucifer0";
            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        private static LoginConfiguration InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            LoginConfiguration loginConfiguration = new LoginConfiguration();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("login.json", false);
            builder.Build().Bind(loginConfiguration);
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            return loginConfiguration;
        }

        private static void InitializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
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
            var container = InitializeContainer();
            var loginServer = container.Resolve<LoginServer>();
            loginServer.Run();
        }

        private static IContainer InitializeContainer()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(InitializeConfiguration()).As<LoginConfiguration>();
            containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly)
              .Where(t => t.IsAssignableFrom(typeof(PacketController)))
              .AsImplementedInterfaces().InstancePerRequest();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            return containerBuilder.Build();
        }
    }
}