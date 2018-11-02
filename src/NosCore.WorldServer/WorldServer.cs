//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Client;
using NosCore.Core.Networking;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Event;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.GameObject.Services.MapInstanceAccess;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Polly;
using Serilog;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        [UsedImplicitly] private readonly List<Item> _items;
        [UsedImplicitly] private readonly MapInstanceAccessService _mapInstanceAccessService;
        [UsedImplicitly] private readonly List<Map> _maps;
        private readonly NetworkManager _networkManager;
        [UsedImplicitly] private readonly List<NpcMonsterDto> _npcmonsters;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly List<IGlobalEvent> _events;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public WorldServer(WorldConfiguration worldConfiguration, NetworkManager networkManager, List<Item> items,
            List<NpcMonsterDto> npcmonsters, List<Map> maps, MapInstanceAccessService mapInstanceAccessService, IEnumerable<IGlobalEvent> events)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _items = items;
            _npcmonsters = npcmonsters;
            _maps = maps;
            _mapInstanceAccessService = mapInstanceAccessService;
            _events = events.ToList();
        }

        public void Run()
        {
            if (_worldConfiguration == null)
            {
                return;
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SUCCESSFULLY_LOADED));
            _events.ForEach(e =>
            {
                Observable.Interval(e.Delay).Subscribe(_ => e.Execution());
            });
            ConnectMaster();
            try
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LanguageKey.LISTENING_PORT),
                    _worldConfiguration.Port);
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
                var connection = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(targetHost), port))
                    .ConfigureAwait(false);

                await connection.WriteAndFlushAsync(new Channel
                {
                    Password = password,
                    ClientName = clientType.Name,
                    ClientType = (byte) clientType.Type,
                    ConnectedAccountLimit = connectedAccountLimit,
                    Port = clientPort,
                    ServerGroup = serverGroup,
                    Host = serverHost,
                    WebApi = webApi
                }).ConfigureAwait(false);
            }

            WebApiAccess.RegisterBaseAdress(_worldConfiguration.MasterCommunication.WebApi.ToString(),
                _worldConfiguration.MasterCommunication.Password);
            Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (_, __, timeSpan) =>
                        _logger.Error(string.Format(
                            LogLanguage.Instance.GetMessageFromKey(LanguageKey.MASTER_SERVER_RETRY),
                            timeSpan.TotalSeconds))
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