//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.AuthService;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHubClient(IMessageBus bus, InterChannelNode node) : IAuthHub
    {
        public Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId) =>
            bus.RequestAsync<string?>(new GetAwaitingConnectionRequest(name, packetPassword, clientSessionSessionId));

        public Task<WorldEntryGrant?> RedeemWorldEntryAsync(string accountName, int sessionId) =>
            bus.RequestAsync<WorldEntryGrant?>(new RedeemWorldEntryRequest(accountName, sessionId));

        public Task SetAwaitingChannelChangeAsync(string accountName, long channelId, bool mfaValidated) =>
            bus.SendToMasterAsync(new SetAwaitingChannelChangeRequest(accountName, channelId, mfaValidated));

        public Task<WorldEntryGrant?> RedeemChannelChangeAsync(string accountName) =>
            bus.RequestAsync<WorldEntryGrant?>(new RedeemChannelChangeRequest(node.QueueName, accountName));

        public Task StoreAuthCodeAsync(string authCode, string accountName) =>
            bus.SendToMasterAsync(new StoreAuthCodeRequest(authCode, accountName));

        public Task RegisterSessionIpAsync(string accountName, string ipAddress) =>
            bus.SendToMasterAsync(new RegisterSessionIpRequest(accountName, ipAddress));

        public Task UnregisterSessionIpAsync(string accountName) =>
            bus.SendToMasterAsync(new UnregisterSessionIpRequest(accountName));

        public Task<string?> GetSessionIpAsync(string accountName) =>
            bus.RequestAsync<string?>(new GetSessionIpRequest(accountName));
    }
}
