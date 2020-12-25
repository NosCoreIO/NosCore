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

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Configuration;
using Serilog;

namespace NosCore.Rpc
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MasterHub : Hub<IMasterHub>, IMasterHub
    {
        private readonly ILogger _logger;
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;

        public MasterHub(ILogger logger, IOptions<WebApiConfiguration> apiConfiguration)
        {
            _apiConfiguration = apiConfiguration;
            _logger = logger;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var data = MasterClientListSingleton.Instance.Channels.FirstOrDefault(o => o.ConnectionId == Context.ConnectionId);
            if (data != null)
            {
                _logger.Debug(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CHANNEL_CONNECTION_LOST),
                    data.Id.ToString(CultureInfo.CurrentCulture),
                    data.Name);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Channels");
            }

            await base.OnDisconnectedAsync(exception);
        }

        //[HttpPost]
        //public IActionResult SetExpectingConnection([FromBody] AuthIntent intent)
        //{
        //    if (intent == null!)
        //    {
        //        return BadRequest(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.AUTH_INCORRECT));
        //    }

        //    SessionFactory.Instance.ReadyForAuth.AddOrUpdate(intent.AccountName, intent.SessionId, (key, oldValue) => intent.SessionId);
        //    return Ok();
        //}

        //[HttpGet]
        //public IActionResult GetExpectingConnection(string? id, string? token, long sessionId)
        //{
        //    if (token != "thisisgfmode")
        //    {
        //        if (token == null || token == "NONE_SESSION_TICKET")
        //        {
        //            return Ok(null);
        //        }
        //        var sessionGuid = HexStringToString(token);
        //        if (!SessionFactory.Instance.AuthCodes.ContainsKey(sessionGuid))
        //        {
        //            return Ok(null);
        //        }
        //        var username = SessionFactory.Instance.AuthCodes[sessionGuid];
        //        SessionFactory.Instance.ReadyForAuth.AddOrUpdate(username, sessionId, (key, oldValue) => sessionId);
        //        return Ok(username);
        //    }

        //    if (id != null && (SessionFactory.Instance.ReadyForAuth.ContainsKey(id) &&
        //        (sessionId == SessionFactory.Instance.ReadyForAuth[id])))
        //    {
        //        return Ok(true);
        //    }

        //    return Ok(false);
        //}
        //private static string HexStringToString(string hexString)
        //{
        //    var bb = Enumerable.Range(0, hexString.Length)
        //        .Where(x => x % 2 == 0)
        //        .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
        //        .ToArray();
        //    return Encoding.UTF8.GetString(bb);
        //}
        public List<ChannelInfo> GetChannels()
        {
            return MasterClientListSingleton.Instance.Channels;
        }

        public List<ChannelInfo> GetChannel(long id)
        {
            return MasterClientListSingleton.Instance.Channels.Where(s => s.Id == id).ToList();
        }

        //public List<Tuple<ServerConfiguration?, ConnectedAccount?>> GetCharacter(long id)
        //{
        //    return new List<Tuple<ServerConfiguration?, ConnectedAccount?>>();
        //}

        //public List<List<ConnectedAccount>> GetCharacters(ChannelInfo? channel)
        //{
        //    return new List<List<ConnectedAccount>>();
        //}
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
                ConnectionId = Context.ConnectionId
            };

            MasterClientListSingleton.Instance.Channels.Add(serv);
        }
    }

    public interface IMasterHub
    {
        public void RegisterChannel(Channel data);
        List<ChannelInfo> GetChannel(long id);
        List<ChannelInfo> GetChannels();
    }
}