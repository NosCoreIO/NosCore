//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.SubscriberCacheService;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;

public class SubscriberCacheDisconnectHandler(ISubscriberCacheService subscriberCache) : ISessionDisconnectHandler
{
    public Task HandleDisconnectAsync(ClientSession session)
    {
        subscriberCache.Forget(session.SessionId);
        return Task.CompletedTask;
    }
}
