using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using NosCore.Configuration;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.Shared.I18N;

namespace NosCore.MasterServer
{
	public class MasterServer
	{
		private readonly MasterConfiguration _masterConfiguration;

		public MasterServer(MasterConfiguration masterConfiguration)
		{
			_masterConfiguration = masterConfiguration;
		}

		private void InitializeLogger()
		{
			// LOGGER
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
			Logger.InitializeLogger(LogManager.GetLogger(typeof(MasterServer)));
		}

		public void Run()
		{
			InitializeLogger();
			if (_masterConfiguration != null)
			{
				Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
			}

			try
			{
				DataAccessHelper.Instance.Initialize(_masterConfiguration.Database);

				Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
					_masterConfiguration.Port));
				Console.Title +=
					$" - Port : {Convert.ToInt32(_masterConfiguration.Port)} - WebApi : {_masterConfiguration.WebApi}";
				RunMasterServerAsync(Convert.ToInt32(_masterConfiguration.Port), _masterConfiguration.Password).Wait();
			}
			catch
			{
				Console.ReadKey();
			}
		}

		public static async Task RunMasterServerAsync(int port, string password)
		{
			var bossGroup = new MultithreadEventLoopGroup(1);
			var workerGroup = new MultithreadEventLoopGroup();

			try
			{
				var bootstrap = new ServerBootstrap();
				bootstrap
					.Group(bossGroup, workerGroup)
					.Channel<TcpServerSocketChannel>()
					.Option(ChannelOption.SoBacklog, 100)
					.ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
					{
						var pipeline = channel.Pipeline;
						pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
						pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

						pipeline.AddLast(new StringEncoder(), new StringDecoder());
						pipeline.AddLast(new MasterServerSession(password));
					}));

				var bootstrapChannel = await bootstrap.BindAsync(port).ConfigureAwait(false);

				Logger.Log.Info(
					string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_LISTENING)));
				Console.ReadLine();

				await bootstrapChannel.CloseAsync().ConfigureAwait(false);
			}
			finally
			{
				Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
			}
		}
	}
}