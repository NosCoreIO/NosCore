using System;
using System.IO;
using System.Reflection;
using Autofac;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core.Handling;
using NosCore.Core.Serializing;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.I18N;

namespace NosCore.LoginServer
{
	public static class WorldServerBootstrap
	{
		private const string ConfigurationPath = @"..\..\..\configuration";
		private const string Title = "NosCore - LoginServer";

        private static void PrintHeader()
		{
			Console.Title = Title;
			const string text = "LOGIN SERVER - 0Lucifer0";
			var offset = (Console.WindowWidth / 2) + (text.Length / 2);
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

		private static void InitializeControllers(IContainer container)
		{
			PacketControllerFactory.Initialize(container);
		}

		public static void Main()
		{
			PrintHeader();
			InitializeLogger();
			InitializePackets();
			var container = InitializeContainer();
			InitializeControllers(container);
			var loginServer = container.Resolve<LoginServer>();
			loginServer.Run();
		}

		private static IContainer InitializeContainer()
		{
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterInstance(InitializeConfiguration()).As<LoginConfiguration>();
			containerBuilder.RegisterAssemblyTypes(typeof(DefaultPacketController).Assembly).As<IPacketController>();
			containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
			return containerBuilder.Build();
		}
	}
}