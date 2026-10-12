//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHubClient(IMessageBus bus) : IAuthHub
    {
        public Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId) =>
            bus.RequestAsync<string?>(new GetAwaitingConnectionRequest(name, packetPassword, clientSessionSessionId));

        public Task SetAwaitingConnectionAsync(long sessionId, string accountName) =>
            bus.SendToMasterAsync(new SetAwaitingConnectionRequest(sessionId, accountName));

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
