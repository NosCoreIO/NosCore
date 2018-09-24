using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.EntityFrameworkCore;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Networking;
using NosCore.DAL;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Item;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Polly;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        private readonly WorldConfiguration _worldConfiguration;
        private readonly NetworkManager _networkManager;
        private readonly List<Item> _items;
        private readonly List<NpcMonsterDTO> _npcmonsters;
        private readonly List<Map> _maps;

        public WorldServer(WorldConfiguration worldConfiguration, NetworkManager networkManager, List<Item> items, List<NpcMonsterDTO> npcmonsters, List<Map> maps)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _items = items;
            _npcmonsters = npcmonsters;
            _maps = maps;
        }


        public void Run()
        {
            if (_worldConfiguration == null)
            {
                return;
            }

            Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));

            ConnectMaster();
            try
            { 
                Logger.Log.Info(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
                    _worldConfiguration.Port));
                Console.Title +=
                    $" - Port : {_worldConfiguration.Port} - WebApi : {_worldConfiguration.WebApi}";
                _networkManager.RunServerAsync().Wait();
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
                        pipeline.AddLast(new MasterClientSession(password, ConnectMaster));
                    }));
                var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port));

                await connection.WriteAndFlushAsync(new Channel
                {
                    Password = password,
                    ClientName = clientType.Name,
                    ClientType = (byte)clientType.Type,
                    connectedAccountLimit = connectedAccountLimit,
                    Port = clientPort,
                    ServerGroup = serverGroup,
                    Host = serverHost,
                    WebApi = webApi
                });
            }

            WebApiAccess.RegisterBaseAdress(_worldConfiguration.MasterCommunication.WebApi.ToString(), _worldConfiguration.MasterCommunication.Password);
            Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (_, __, timeSpan) =>
                    Logger.Log.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_RETRY), timeSpan.TotalSeconds))
                ).ExecuteAsync(() => RunMasterClient(_worldConfiguration.MasterCommunication.Host,
                    Convert.ToInt32(_worldConfiguration.MasterCommunication.Port),
                    _worldConfiguration.MasterCommunication.Password,
                    new MasterClient
                    {
                        Name = _worldConfiguration.ServerName,
                        Type = ServerType.WorldServer,
                        WebApi = _worldConfiguration.WebApi
                    }, _worldConfiguration.WebApi, _worldConfiguration.ConnectedAccountLimit,
                    _worldConfiguration.Port, _worldConfiguration.ServerGroup, _worldConfiguration.Host)
                ).Wait();
        }
    }
}