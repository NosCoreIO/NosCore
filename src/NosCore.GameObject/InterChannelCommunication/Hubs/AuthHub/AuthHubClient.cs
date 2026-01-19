//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub
{
    public class AuthHubClient(HubConnectionFactory hubConnectionFactory) : IAuthHub
    {
        private readonly HubConnection _hubConnection = hubConnectionFactory.Create(nameof(AuthHub));

        public async Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId)
        {
            await _hubConnection.StartAsync();
            var result = await _hubConnection.InvokeAsync<string?>(nameof(GetAwaitingConnectionAsync), name, packetPassword, clientSessionSessionId);
            await _hubConnection.StopAsync();
            return result;
        }

        public async Task SetAwaitingConnectionAsync(long sessionId, string accountName)
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync(nameof(SetAwaitingConnectionAsync), sessionId, accountName);
            await _hubConnection.StopAsync();
        }
    }
}
