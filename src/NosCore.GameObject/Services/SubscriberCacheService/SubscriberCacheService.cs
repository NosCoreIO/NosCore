//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.SubscriberCacheService;

// For packets a player can repeat at will. A subscriber found in the kept list is answered
// from it; one that is missing may have logged in since, so the hub is asked before it counts
// as absent. Anything that must see a logout the moment it happens asks the hub directly.
public sealed class SubscriberCacheService(IPubSubHub pubSubHub, IClock clock)
    : ISubscriberCacheService, ISingletonService
{
    public static readonly Duration CacheDuration = Duration.FromSeconds(3);

    private CachedSubscribers? _cached;

    private readonly ConcurrentDictionary<long, byte> _departed = new();

    public async Task<Subscriber?> FindAsync(Func<Subscriber, bool> matches)
    {
        var now = clock.GetCurrentInstant();
        var cached = _cached;
        if (cached != null && cached.ExpiresAt > now && cached.Subscribers.FirstOrDefault(s => IsPresent(s) && matches(s)) is { } known)
        {
            return known;
        }

        var subscribers = await pubSubHub.GetSubscribersAsync();
        foreach (var id in _departed.Keys.Where(id => subscribers.All(s => s.Id != id)))
        {
            _departed.TryRemove(id, out _);
        }

        _cached = new CachedSubscribers(subscribers, now + CacheDuration);
        return subscribers.FirstOrDefault(s => IsPresent(s) && matches(s));
    }

    public void Forget(long subscriberId) => _departed[subscriberId] = 0;

    private bool IsPresent(Subscriber subscriber) => !_departed.ContainsKey(subscriber.Id);

    private sealed record CachedSubscribers(IReadOnlyList<Subscriber> Subscribers, Instant ExpiresAt);
}
