//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BlackListService;

public sealed class BlacklistCacheService(IBlacklistHub blacklistHub, IClock clock)
    : IBlacklistCacheService, ISingletonService
{
    public static readonly Duration CacheDuration = Duration.FromSeconds(3);

    private readonly ConcurrentDictionary<long, CachedBlacklist> _cached = new();

    public async Task<IReadOnlyList<CharacterRelationStatus>> GetBlacklistedAsync(long characterId)
    {
        var now = clock.GetCurrentInstant();
        if (_cached.TryGetValue(characterId, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Blacklist;
        }

        foreach (var entry in _cached)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _cached.TryRemove(entry);
            }
        }

        var blacklist = await blacklistHub.GetBlacklistedAsync(characterId);
        _cached[characterId] = new CachedBlacklist(blacklist, now + CacheDuration);
        return blacklist;
    }

    public void Invalidate(long characterId) => _cached.TryRemove(characterId, out _);

    private sealed record CachedBlacklist(IReadOnlyList<CharacterRelationStatus> Blacklist, Instant ExpiresAt);
}
