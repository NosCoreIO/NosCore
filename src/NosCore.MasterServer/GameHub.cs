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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Configuration;
using Serilog;
// ReSharper disable UnusedMember.Global

namespace NosCore.MasterServer
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GameHub : Hub
    {
        private readonly ILogger _logger;
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;

        public GameHub(ILogger logger, IOptions<WebApiConfiguration> apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;
        }

        public List<ChannelInfo> GetChannels(long? id)
        {
            return id != null ? MasterClientListSingleton.Instance.Channels.Where(s => s.Id == id).ToList() : MasterClientListSingleton.Instance.Channels;
        }

        public void RegisterChannel(Channel data)
        {
            if (data.MasterCommunication!.Password != _apiConfiguration.Value.Password)
            {
                _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_ERROR));
                return;
            }

            var id = ++MasterClientListSingleton.Instance.ConnectionCounter;
            _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTHENTICATED_SUCCESS),
                id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = (ushort?)data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                Id = id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                LastPing = SystemTime.Now(),
                Type = data.ClientType,
            };

            MasterClientListSingleton.Instance.Channels.Add(serv);
        }
    }
}