using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Encryption;
using NosCore.Core.Logger;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.Domain;
using NosCore.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        private readonly WorldConfiguration _worldConfiguration;

        public WorldServer(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public void Run()
        {
            BuildWebHost(null).StartAsync();
            Mapping.Mapper.InitializeMapping();
            ConnectMaster();
            if (DataAccessHelper.Instance.Initialize(_worldConfiguration.Database))
            {
                ServerManager.Instance.Initialize();
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT), _worldConfiguration.Port));
                Console.Title += $" - Port : {Convert.ToInt32(_worldConfiguration.Port)} - WebApi : {_worldConfiguration.WebApi}";
                NetworkManager.RunServerAsync(Convert.ToInt32(_worldConfiguration.Port), new WorldEncoderFactory(), new WorldDecoderFactory(), true).Wait();
            }
            else
            {
                Console.ReadKey();
                return;
            }
        }

        private IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>()
               .UseUrls(_worldConfiguration.WebApi.ToString())
               .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
               .PreferHostingUrls(true)
               .Build();

        private  void ConnectMaster()
        {
            async Task RunMasterClient(string targetHost, int port, string password, MasterClient clientType, ServerConfiguration WebApi, int connectedAccountLimit = 0, int clientPort = 0, byte serverGroup = 0, string serverHost = "")
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
        
    }
}