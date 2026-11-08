//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SubscriberCacheService;

// For packets a player can repeat at will; anything that must see a login or logout the
// moment it happens still asks the hub directly.
public sealed class SubscriberCacheService(IPubSubHub pubSubHub, IClock clock)
    : ISubscriberCacheService, ISingletonService
{
    public static readonly Duration CacheDuration = Duration.FromSeconds(3);

    private CachedSubscribers? _cached;

    public async Task<IReadOnlyList<Subscriber>> GetSubscribersAsync()
    {
        var now = clock.GetCurrentInstant();
        var cached = _cached;
        if (cached != null && cached.ExpiresAt > now)
        {
            return cached.Subscribers;
        }

        var subscribers = await pubSubHub.GetSubscribersAsync();
        _cached = new CachedSubscribers(subscribers, now + CacheDuration);
        return subscribers;
    }

    private sealed record CachedSubscribers(IReadOnlyList<Subscriber> Subscribers, Instant ExpiresAt);
}
