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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Event;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Providers.MapInstanceProvider;
using Serilog;

namespace NosCore.WorldServer
{
    public class WorldServer
    {
        private readonly IChannelHttpClient _channelHttpClient;
        private readonly List<IGlobalEvent> _events;
        private readonly ILogger _logger;
        private readonly NetworkManager _networkManager;
        private readonly IOptions<WorldConfiguration> _worldConfiguration;
        private readonly IMapInstanceProvider _mapInstanceProvider;

        public WorldServer(IOptions<WorldConfiguration> worldConfiguration, NetworkManager networkManager,
            IEnumerable<IGlobalEvent> events, ILogger logger, IChannelHttpClient channelHttpClient, IMapInstanceProvider mapInstanceProvider)
        {
            _worldConfiguration = worldConfiguration;
            _networkManager = networkManager;
            _events = events.ToList();
            _logger = logger;
            _channelHttpClient = channelHttpClient;
            _mapInstanceProvider = mapInstanceProvider;
        }

        public async Task RunAsync()
        {
            await _mapInstanceProvider.InitializeAsync().ConfigureAwait(false);
            _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.SUCCESSFULLY_LOADED));
            _events.ForEach(e => { Observable.Interval(e.Delay).Subscribe(_ => e.ExecutionAsync()); });
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                var eventSaveAll = new SaveAll(_logger);
                _ = eventSaveAll.ExecutionAsync();
                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_WILL_EXIT));
                Thread.Sleep(30000);
            };

            try
            {

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.Title += $@" - Port : {_worldConfiguration.Value.Port} - WebApi : {_worldConfiguration.Value.WebApi}";
                }

                _logger.Information(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.LISTENING_PORT),
                    _worldConfiguration.Value.Port);
                await Task.WhenAny(_channelHttpClient.ConnectAsync(), _networkManager.RunServerAsync()).ConfigureAwait(false);
            }
            catch
            {
                Console.ReadKey();
            }
        }
    }
}