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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NosCore.GameObject.Services.AuthService;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHub : Hub, IAuthHub
    {
        private readonly IAuthCodeService _authCodeService;

        public AuthHub(IAuthCodeService authCodeService)
        {
            _authCodeService = authCodeService;
        }

        public Task<string?> GetAwaitingConnectionAsync(string? id, string? token, int sessionId)
        {
            if (token != "thisisgfmode")
            {
                if (token == null || token == "NONE_SESSION_TICKET")
                {
                    return Task.FromResult<string?>(null);
                }

                var sessionGuid = HexStringToString(token);
                var username = _authCodeService.GetAccountByAuthCode(sessionGuid);
                if (username == null)
                {
                    return Task.FromResult<string?>(null);
                }

                _authCodeService.MarkReadyForAuth(username, sessionId);
                return Task.FromResult<string?>(username);
            }

            if (id != null && _authCodeService.IsReadyForAuth(id, sessionId))
            {
                return Task.FromResult<string?>("true");
            }

            return Task.FromResult<string?>("false");
        }

        private static string HexStringToString(string hexString)
        {
            var bb = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            return Encoding.UTF8.GetString(bb);
        }

        public Task SetAwaitingConnectionAsync(long sessionId, string accountName)
        {
            _authCodeService.MarkReadyForAuth(accountName, sessionId);
            return Task.CompletedTask;
        }

    }
}