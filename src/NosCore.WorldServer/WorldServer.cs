using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using log4net;
using log4net.Config;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Encryption;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer
{
	public class WorldServer
	{
		private readonly WorldConfiguration _worldConfiguration;

		public WorldServer(WorldConfiguration worldConfiguration)
		{
			_worldConfiguration = worldConfiguration;
		}

		private void InitializeLogger()
		{
			// LOGGER
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
			Logger.InitializeLogger(LogManager.GetLogger(typeof(WorldServer)));
		}

		public void Run()
		{
			InitializeLogger();
			if (_worldConfiguration != null)
			{
				Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
			}

			DAOFactory.RegisterMapping(typeof(Character).Assembly);
			ConnectMaster();
			try
			{
				DataAccessHelper.Instance.Initialize(_worldConfiguration.Database);

				ServerManager.Instance.Initialize();
				Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
					_worldConfiguration.Port));
				Console.Title +=
					$" - Port : {_worldConfiguration.Port} - WebApi : {_worldConfiguration.WebApi}";
				NetworkManager.RunServerAsync(Convert.ToInt32(_worldConfiguration.Port), new WorldEncoderFactory(),
					new WorldDecoderFactory(), true).Wait();
			}
			catch
			{
				Console.ReadKey();
			}
		}

		private void ConnectMaster()
		{
			async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType,
				ServerConfiguration webApi, int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0,
				string serverHost = "")
			{
				var group = new MultithreadEventLoopGroup();

				var bootstrap = new Bootstrap();
				bootstrap
					.Group(group)
					.Channel<TcpSocketChannel>()
					.Option(ChannelOption.TcpNodelay, true)
					.Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
					{
						var pipeline = channel.Pipeline;

						pipeline.AddLast(new LengthFieldPrepender(2));
						pipeline.AddLast(new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

						pipeline.AddLast(new StringEncoder(), new StringDecoder());
						pipeline.AddLast(new MasterClientSession(password));
					}));
				var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port))
					.ConfigureAwait(false);

				await connection.WriteAndFlushAsync(new Channel
				{
					Password = password,
					ClientName = clientType.Name,
					ClientType = (byte) clientType.Type,
					ConnectedAccountsLimit = connectedAccountLimit,
					Port = clientPort,
					ServerGroup = serverGroup,
					Host = serverHost,
					WebApi = webApi
				}).ConfigureAwait(false);
			}

			while (true)
			{
				try
				{
					WebApiAccess.RegisterBaseAdress(_worldConfiguration.MasterCommunication.WebApi.ToString(), _worldConfiguration.MasterCommunication.Password);
                    RunMasterClient(_worldConfiguration.MasterCommunication.Host,
						Convert.ToInt32(_worldConfiguration.MasterCommunication.Port),
						_worldConfiguration.MasterCommunication.Password,
						new MasterClient
						{
							Name = _worldConfiguration.ServerName,
							Type = ServerType.WorldServer,
							WebApi = _worldConfiguration.WebApi
						}, _worldConfiguration.WebApi, _worldConfiguration.ConnectedAccountLimit,
						_worldConfiguration.Port, _worldConfiguration.ServerGroup, _worldConfiguration.Host).Wait();
					break;
				}
				catch
				{
					Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_RETRY));
					Thread.Sleep(5000);
				}
			}
		}
	}
}