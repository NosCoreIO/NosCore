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
using System.Reactive.Linq;
using System.Threading;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.StaticEntities;
using NosCore.GameObject;
using NosCore.GameObject.Event;
using NosCore.GameObject.Map;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ItemBuilderService.Item;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.Packets.ClientPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        [UsedImplicitly] private readonly List<Item> _items;
        [UsedImplicitly] private readonly IEnumerable<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> _handlers;
        [UsedImplicitly] private readonly IMapInstanceAccessService _mapInstanceAccessService;
        [UsedImplicitly] private readonly List<Map> _maps;
        private readonly NetworkManager _networkManager;
        [UsedImplicitly] private readonly List<NpcMonsterDto> _npcmonsters;
        private readonly WorldConfiguration _worldConfiguration;
        private readonly List<IGlobalEvent> _events;
        [UsedImplicitly] private readonly Mapper _mapper;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        public WorldServer(WorldConfiguration worldConfiguration, NetworkManager networkManager, List<Item> items,
            List<NpcMonsterDto> npcmonsters, List<Map> maps, IMapInstanceAccessService mapInstanceAccessService,
            IEnumerable<IGlobalEvent> events, IEnumerable<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>> handlers, Mapper mapper)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _items = items;
            _handlers = handlers;
            _npcmonsters = npcmonsters;
            _maps = maps;
            _mapInstanceAccessService = mapInstanceAccessService;
            _events = events.ToList();
            _mapper = mapper;
        }

        public void Run()
        {
            if (_worldConfiguration == null)
            {
                return;
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            _events.ForEach(e =>
            {
                Observable.Interval(e.Delay).Subscribe(_ => e.Execution());
            });
            ConnectMaster();
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll();
                eventSaveAll.Execution();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_WILL_EXIT));
                Thread.Sleep(30000);
            };
            try
            {
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT),
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
            WebApiAccess.RegisterBaseAdress(new Channel
            {
                MasterCommunication = _worldConfiguration.MasterCommunication,
                ClientName = _worldConfiguration.ServerName,
                ClientType = ServerType.WorldServer,
                ConnectedAccountLimit = _worldConfiguration.ConnectedAccountLimit,
                Port = _worldConfiguration.Port,
                ServerGroup = _worldConfiguration.ServerGroup,
                Host = _worldConfiguration.Host,
                WebApi = _worldConfiguration.WebApi
            });
        }
    }
}