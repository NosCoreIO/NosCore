//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
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
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Event;
using NosCore.GameObject.Networking;
using Serilog;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        private readonly List<IGlobalEvent> _events;
        private readonly ILogger _logger;
        private readonly NetworkManager _networkManager;
        private readonly WorldConfiguration _worldConfiguration;

        public WorldServer(WorldConfiguration worldConfiguration, NetworkManager networkManager,
            IEnumerable<IGlobalEvent> events, ILogger logger)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _events = events.ToList();
            _logger = logger;
        }

        public void Run()
        {
            if (_worldConfiguration == null)
            {
                return;
            }

            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            _events.ForEach(e => { Observable.Interval(e.Delay).Subscribe(_ => e.Execution()); });
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