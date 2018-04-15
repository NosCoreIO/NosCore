using log4net;
using log4net.Config;
using log4net.Repository;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.Logger;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Database;
using NosCore.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NosCore.GameObject.Networking;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Codecs;
using DotNetty.Transport.Channels.Sockets;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using NosCore.Configuration;
using NosCore.GameObject;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using NosCore.DAL;
using AutoMapper;
using NosCore.Core.Client;
using NosCore.Core.Extensions;
using NosCore.Core.Networking;
using NosCore.Core.Serializing.HandlerSerialization;
using NosCore.Data.AliveEntities;
using NosCore.Domain;
using NosCore.Handler;

namespace NosCore.WorldServer
{
    public static class WorldServer
    {
        private static readonly WorldConfiguration _worldConfiguration = new WorldConfiguration();

        private const string _configurationPath = @"..\..\..\configuration";

        private static List<IPacketHandler> _clientPacketDefinitions;

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory() + _configurationPath);
            builder.AddJsonFile("world.json", false);
            builder.Build().Bind(_worldConfiguration);
            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
        }

        private static void InitializeLogger()
        {
            // LOGGER
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("../../configuration/log4net.config"));
            Logger.InitializeLogger(LogManager.GetLogger(typeof(WorldServer)));
        }

        public static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>()
               .UseUrls(_worldConfiguration.WebApi.ToString())
               .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
               .PreferHostingUrls(true)
               .Build();

        private static void InitializePackets()
        {
            PacketFactory.Initialize<NoS0575Packet>();
            _clientPacketDefinitions = typeof(DefaultPacketHandler).Assembly.GetInstancesOfImplementingTypes<IPacketHandler>().ToList();
        }

        private static void ConnectMaster()
        {
            while (true)
            {
                try
                {
                    RunMasterClient(_worldConfiguration.MasterCommunication.Host, Convert.ToInt32(_worldConfiguration.MasterCommunication.Port), _worldConfiguration.MasterCommunication.Password, new MasterClient() { Name = "WorldServer", Type = ServerType.WorldServer, WebApi = _worldConfiguration.WebApi }, WebApi: _worldConfiguration.WebApi, connectedAccountLimit: _worldConfiguration.ConnectedAccountLimit, clientPort: _worldConfiguration.Port, serverGroup: _worldConfiguration.ServerGroup, serverHost: _worldConfiguration.Host).Wait();
                    break;
                }
                catch
                {
                    Logger.Log.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.MASTER_SERVER_RETRY));
                    Thread.Sleep(5000);
                }
            }
        }

        private static void PrintHeader()
        {
            Console.Title = "NosCore - WorldServer";
            const string text = "WORLD SERVER - 0Lucifer0";
            int offset = (Console.WindowWidth / 2) + (text.Length / 2);
            string separator = new string('=', Console.WindowWidth);
            Console.WriteLine(separator + string.Format("{0," + offset + "}\n", text) + separator);
        }

        public static async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType, ServerConfiguration WebApi, int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0, string serverHost = "")
        {
            var group = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    pipeline.AddLast(new LengthFieldPrepender(2));
                    pipeline.AddLast(new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                    pipeline.AddLast(new StringEncoder(), new StringDecoder());
                    pipeline.AddLast(new MasterClientSession(password));
                }));
            var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port)).ConfigureAwait(false);

            await connection.WriteAndFlushAsync(new Channel()
            {
                Password = password,
                ClientName = clientType.Name,
                ClientType = (byte)clientType.Type,
                ConnectedAccountsLimit = connectedAccountLimit,
                Port = clientPort,
                ServerGroup = serverGroup,
                Host = serverHost,
                WebApi = WebApi
            }).ConfigureAwait(false);
        }

        public static void Main(string[] args)
        {
            PrintHeader();
            InitializeLogger();
            InitializeConfiguration();
            BuildWebHost(args).StartAsync();
            Mapping.Mapper.InitializeMapping();
            InitializePackets();
            ConnectMaster();
            if (DataAccessHelper.Instance.Initialize(_worldConfiguration.Database))
            {
                ServerManager.Instance.Initialize();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT), _worldConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_worldConfiguration.Port)} - WebApi : {_worldConfiguration.WebApi}";
                NetworkManager.RunServerAsync(Convert.ToInt32(_worldConfiguration.Port), new WorldEncoderFactory(), new WorldDecoderFactory(), _clientPacketDefinitions, true).Wait();
            }
            else
            {
                Console.ReadKey();
                return;
            }
        }
    }
}