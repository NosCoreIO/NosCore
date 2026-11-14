//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.AuthService
{
    public class AuthCodeService(IClock clock) : IAuthCodeService
    {
        public static readonly Duration AuthCodeLifetime = Duration.FromMinutes(2);

        // The client puts no timer on its channel list, so this has to outlast a player who
        // leaves the list open for a while.
        public static readonly Duration ReadyForAuthLifetime = Duration.FromMinutes(10);

        public static readonly Duration ChannelChangeLifetime = Duration.FromSeconds(15);

        private readonly ConcurrentDictionary<string, Expiring<string>> _authCodes = new();
        private readonly ConcurrentDictionary<string, Expiring<long>> _readyForAuth = new();
        private readonly ConcurrentDictionary<string, Expiring<ChannelChangeEntry>> _channelChanges = new();
        private readonly ConcurrentDictionary<string, string> _sessionIps = new();

        public void StoreAuthCode(string authCode, string accountName)
        {
            RemoveExpired(_authCodes);
            _authCodes[authCode] = new Expiring<string>(accountName, clock.GetCurrentInstant() + AuthCodeLifetime);
        }

        public string? ConsumeAuthCode(string authCode)
        {
            return _authCodes.TryRemove(authCode, out var entry) && !IsExpired(entry.ExpiresAt) ? entry.Value : null;
        }

        public void MarkReadyForAuth(string accountName, long sessionId)
        {
            RemoveExpired(_readyForAuth);
            _readyForAuth[accountName] = new Expiring<long>(sessionId, clock.GetCurrentInstant() + ReadyForAuthLifetime);
        }

        // Only an auth code redeemed by the login server marks an account ready, and the WebApi
        // checks MFA before it issues a code.
        public WorldEntryGrant? ConsumeReadyForAuth(string accountName, long sessionId)
        {
            return TryConsume(_readyForAuth, accountName, sessionId)
                ? new WorldEntryGrant(true)
                : null;
        }

        public void MarkReadyForChannelChange(string accountName, long channelId, bool mfaValidated)
        {
            RemoveExpired(_channelChanges);
            _channelChanges[accountName] = new Expiring<ChannelChangeEntry>(
                new ChannelChangeEntry(channelId, mfaValidated), clock.GetCurrentInstant() + ChannelChangeLifetime);
        }

        public WorldEntryGrant? ConsumeChannelChange(string accountName, long channelId)
        {
            if (!_channelChanges.TryGetValue(accountName, out var entry) || entry.Value.ChannelId != channelId)
            {
                return null;
            }

            return _channelChanges.TryRemove(new KeyValuePair<string, Expiring<ChannelChangeEntry>>(accountName, entry))
                && !IsExpired(entry.ExpiresAt)
                    ? new WorldEntryGrant(entry.Value.MfaValidated)
                    : null;
        }

        public void RegisterSessionIp(string accountName, string ipAddress)
        {
            _sessionIps[accountName] = ipAddress;
        }

        public void UnregisterSessionIp(string accountName)
        {
            _sessionIps.TryRemove(accountName, out _);
        }

        public string? GetSessionIp(string accountName)
        {
            return _sessionIps.TryGetValue(accountName, out var ip) ? ip : null;
        }

        private bool TryConsume(ConcurrentDictionary<string, Expiring<long>> entries, string accountName, long expected)
        {
            if (!entries.TryGetValue(accountName, out var entry) || entry.Value != expected)
            {
                return false;
            }

            return entries.TryRemove(new KeyValuePair<string, Expiring<long>>(accountName, entry))
                && !IsExpired(entry.ExpiresAt);
        }

        private bool IsExpired(Instant expiresAt) => clock.GetCurrentInstant() >= expiresAt;

        private void RemoveExpired<T>(ConcurrentDictionary<string, Expiring<T>> entries)
        {
            foreach (var entry in entries)
            {
                if (IsExpired(entry.Value.ExpiresAt))
                {
                    entries.TryRemove(entry);
                }
            }
        }

        private sealed record Expiring<T>(T Value, Instant ExpiresAt);

        private sealed record ChannelChangeEntry(long ChannelId, bool MfaValidated);
    }
}
