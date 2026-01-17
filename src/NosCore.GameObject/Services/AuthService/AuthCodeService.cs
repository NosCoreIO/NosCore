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

using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.AuthService
{
    public class AuthCodeService : IAuthCodeService
    {
        private readonly ConcurrentDictionary<string, string> _authCodes = new();
        private readonly ConcurrentDictionary<string, long> _readyForAuth = new();

        public void StoreAuthCode(string authCode, string accountName)
        {
            _authCodes[authCode] = accountName;
        }

        public string? GetAccountByAuthCode(string authCode)
        {
            return _authCodes.TryGetValue(authCode, out var accountName) ? accountName : null;
        }

        public bool TryRemoveAuthCode(string authCode, out string? accountName)
        {
            return _authCodes.TryRemove(authCode, out accountName);
        }

        public void MarkReadyForAuth(string accountName, long sessionId)
        {
            _readyForAuth.AddOrUpdate(accountName, sessionId, (_, _) => sessionId);
        }

        public bool IsReadyForAuth(string accountName, long sessionId)
        {
            return _readyForAuth.TryGetValue(accountName, out var storedSessionId) && storedSessionId == sessionId;
        }

        public void ClearReadyForAuth(string accountName)
        {
            _readyForAuth.TryRemove(accountName, out _);
        }
    }
}
