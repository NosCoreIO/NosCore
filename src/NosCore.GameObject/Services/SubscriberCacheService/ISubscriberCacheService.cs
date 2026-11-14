//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SubscriberCacheService;

public interface ISubscriberCacheService
{
    Task<Subscriber?> FindAsync(Func<Subscriber, bool> matches);

    // The master lists a session for a moment after it has left this channel; it is skipped from now on.
    void Forget(long subscriberId);
}
