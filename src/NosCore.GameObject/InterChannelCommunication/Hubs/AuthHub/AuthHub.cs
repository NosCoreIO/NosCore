//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.GameObject.Services.AuthService;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHub : IAuthHub
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
            // Older clients hex-encoded each ASCII char of the auth code (36
            // GUID chars → 72 hex chars); modern builds hand the raw GUID
            // string straight through. Detect the GUID form via the dashes
            // so Convert.ToByte doesn't choke on them.
            if (Guid.TryParse(hexString, out _))
            {
                return hexString;
            }

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

        public Task StoreAuthCodeAsync(string authCode, string accountName)
        {
            _authCodeService.StoreAuthCode(authCode, accountName);
            return Task.CompletedTask;
        }

        public Task RegisterSessionIpAsync(string accountName, string ipAddress)
        {
            _authCodeService.RegisterSessionIp(accountName, ipAddress);
            return Task.CompletedTask;
        }

        public Task UnregisterSessionIpAsync(string accountName)
        {
            _authCodeService.UnregisterSessionIp(accountName);
            return Task.CompletedTask;
        }

        public Task<string?> GetSessionIpAsync(string accountName)
        {
            return Task.FromResult(_authCodeService.GetSessionIp(accountName));
        }

        public static async Task<Reply<string?>> Handle(GetAwaitingConnectionRequest request, IServiceProvider services) =>
            new(await services.GetRequiredService<AuthHub>().GetAwaitingConnectionAsync(request.Name, request.Token, request.SessionId));

        public static Task Handle(SetAwaitingConnectionRequest request, IServiceProvider services) =>
            services.GetRequiredService<AuthHub>().SetAwaitingConnectionAsync(request.SessionId, request.AccountName);

        public static Task Handle(StoreAuthCodeRequest request, IServiceProvider services) =>
            services.GetRequiredService<AuthHub>().StoreAuthCodeAsync(request.AuthCode, request.AccountName);

        public static Task Handle(RegisterSessionIpRequest request, IServiceProvider services) =>
            services.GetRequiredService<AuthHub>().RegisterSessionIpAsync(request.AccountName, request.IpAddress);

        public static Task Handle(UnregisterSessionIpRequest request, IServiceProvider services) =>
            services.GetRequiredService<AuthHub>().UnregisterSessionIpAsync(request.AccountName);

        public static async Task<Reply<string?>> Handle(GetSessionIpRequest request, IServiceProvider services) =>
            new(await services.GetRequiredService<AuthHub>().GetSessionIpAsync(request.AccountName));
    }
}
