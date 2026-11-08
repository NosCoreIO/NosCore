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
    public class AuthHub(IAuthCodeService authCodeService, MasterClientList masterClientList) : IAuthHub
    {
        public Task<string?> GetAwaitingConnectionAsync(string? id, string? token, int sessionId)
        {
            if (token == null || token == "NONE_SESSION_TICKET")
            {
                return Task.FromResult<string?>(null);
            }

            var authCode = HexStringToString(token);
            var username = authCode == null ? null : authCodeService.ConsumeAuthCode(authCode);
            if (username == null)
            {
                return Task.FromResult<string?>(null);
            }

            authCodeService.MarkReadyForAuth(username, sessionId);
            return Task.FromResult<string?>(username);
        }

        public Task<WorldEntryGrant?> RedeemWorldEntryAsync(string accountName, int sessionId)
        {
            return Task.FromResult(authCodeService.ConsumeReadyForAuth(accountName, sessionId));
        }

        public Task SetAwaitingChannelChangeAsync(string accountName, long channelId, bool mfaValidated)
        {
            authCodeService.MarkReadyForChannelChange(accountName, channelId, mfaValidated);
            return Task.CompletedTask;
        }

        public Task<WorldEntryGrant?> RedeemChannelChangeAsync(string accountName) =>
            throw new InvalidOperationException("The master server is not a channel");

        public WorldEntryGrant? RedeemChannelChange(string channelKey, string accountName)
        {
            return masterClientList.Channels.TryGetValue(channelKey, out var channel)
                ? authCodeService.ConsumeChannelChange(accountName, channel.Id)
                : null;
        }

        private static string? HexStringToString(string hexString)
        {
            // Older clients hex-encoded each ASCII char of the auth code (36
            // GUID chars → 72 hex chars); modern builds hand the raw GUID
            // string straight through. Detect the GUID form via the dashes
            // so Convert.ToByte doesn't choke on them.
            if (Guid.TryParse(hexString, out _))
            {
                return hexString;
            }

            if (hexString.Length % 2 != 0 || !hexString.All(Uri.IsHexDigit))
            {
                return null;
            }

            var bb = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            return Encoding.UTF8.GetString(bb);
        }

        public Task StoreAuthCodeAsync(string authCode, string accountName)
        {
            authCodeService.StoreAuthCode(authCode, accountName);
            return Task.CompletedTask;
        }

        public Task RegisterSessionIpAsync(string accountName, string ipAddress)
        {
            authCodeService.RegisterSessionIp(accountName, ipAddress);
            return Task.CompletedTask;
        }

        public Task UnregisterSessionIpAsync(string accountName)
        {
            authCodeService.UnregisterSessionIp(accountName);
            return Task.CompletedTask;
        }

        public Task<string?> GetSessionIpAsync(string accountName)
        {
            return Task.FromResult(authCodeService.GetSessionIp(accountName));
        }

        public static async Task<Reply<string?>> Handle(GetAwaitingConnectionRequest request, IServiceProvider services) =>
            new(await services.GetRequiredService<AuthHub>().GetAwaitingConnectionAsync(request.Name, request.Token, request.SessionId));

        public static async Task<Reply<WorldEntryGrant?>> Handle(RedeemWorldEntryRequest request, IServiceProvider services) =>
            new(await services.GetRequiredService<AuthHub>().RedeemWorldEntryAsync(request.AccountName, request.SessionId));

        public static Task Handle(SetAwaitingChannelChangeRequest request, IServiceProvider services) =>
            services.GetRequiredService<AuthHub>().SetAwaitingChannelChangeAsync(request.AccountName, request.ChannelId, request.MfaValidated);

        public static Reply<WorldEntryGrant?> Handle(RedeemChannelChangeRequest request, IServiceProvider services) =>
            new(services.GetRequiredService<AuthHub>().RedeemChannelChange(request.ChannelKey, request.AccountName));

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
