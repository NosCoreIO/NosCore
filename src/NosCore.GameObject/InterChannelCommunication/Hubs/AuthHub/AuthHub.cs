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
using NodaTime;
using NosCore.Core;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHub : Hub, IAuthHub
    {
        public Task<string?> GetAwaitingConnectionAsync(string? id, string token, int sessionId)
        {
            if (token != "thisisgfmode")
            {
                if (token == null || token == "NONE_SESSION_TICKET")
                {
                    return Task.FromResult<string?>(null);
                }

                var sessionGuid = HexStringToString(token);
                if (!SessionFactory.Instance.AuthCodes.ContainsKey(sessionGuid))
                {
                    return Task.FromResult<string?>(null);
                }

                var username = SessionFactory.Instance.AuthCodes[sessionGuid];
                SessionFactory.Instance.ReadyForAuth.AddOrUpdate(username, sessionId, (key, oldValue) => sessionId);
                return Task.FromResult<string?>(username);
            }

            if (id != null && (SessionFactory.Instance.ReadyForAuth.ContainsKey(id) &&
                    (sessionId == SessionFactory.Instance.ReadyForAuth[id])))
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
            SessionFactory.Instance.ReadyForAuth.AddOrUpdate(accountName, sessionId, (key, oldValue) => sessionId);
            return Task.CompletedTask;
        }

    }
}