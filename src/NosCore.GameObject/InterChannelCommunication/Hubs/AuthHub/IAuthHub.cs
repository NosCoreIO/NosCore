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
}
