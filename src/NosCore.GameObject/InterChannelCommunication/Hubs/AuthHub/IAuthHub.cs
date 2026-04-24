//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;

public interface IAuthHub
{
    Task<string?> GetAwaitingConnectionAsync(string? name, string? packetPassword, int clientSessionSessionId);

    Task SetAwaitingConnectionAsync(long sessionId, string accountName);

    /// <summary>
    /// Push an auth code issued by the WebApi into the shared auth-code
    /// store on the MasterServer so every other process (LoginServer
    /// first) can validate it via <see cref="GetAwaitingConnectionAsync"/>.
    /// </summary>
    Task StoreAuthCodeAsync(string authCode, string accountName);

    Task RegisterSessionIpAsync(string accountName, string ipAddress);
    Task UnregisterSessionIpAsync(string accountName);
    Task<string?> GetSessionIpAsync(string accountName);
}
