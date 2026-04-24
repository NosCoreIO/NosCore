//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.AuthService
{
    public class AuthCodeService : IAuthCodeService
    {
        private readonly ConcurrentDictionary<string, string> _authCodes = new();
        private readonly ConcurrentDictionary<string, long> _readyForAuth = new();
        private readonly ConcurrentDictionary<string, string> _sessionIps = new();

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

        public void RegisterSessionIp(string accountName, string ipAddress)
        {
            _sessionIps[accountName] = ipAddress;
        }

        public void UnregisterSessionIp(string accountName)
        {
            _sessionIps.TryRemove(accountName, out _);
        }

        public string? GetSessionIp(string accountName)
        {
            return _sessionIps.TryGetValue(accountName, out var ip) ? ip : null;
        }
    }
}
