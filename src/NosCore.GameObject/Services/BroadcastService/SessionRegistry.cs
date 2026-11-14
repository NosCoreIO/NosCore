//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.BroadcastService
{
    public class SessionRegistry(ILogger<SessionRegistry> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : ISessionRegistry
    {
        private readonly ConcurrentDictionary<string, SessionInfo> _sessionsByChannelId = new();
        private readonly ConcurrentDictionary<long, string> _channelIdByCharacterId = new();

        public void Register(SessionInfo sessionInfo)
        {
            _sessionsByChannelId[sessionInfo.ChannelId] = sessionInfo;
        }

        public void Unregister(string channelId)
        {
            if (_sessionsByChannelId.TryRemove(channelId, out var session) && session.CharacterId.HasValue)
            {
                _channelIdByCharacterId.TryRemove(session.CharacterId.Value, out _);
            }
        }

        public void UpdateCharacter(string channelId, long characterId, Guid? mapInstanceId)
        {
            if (_sessionsByChannelId.TryGetValue(channelId, out var session))
            {
                if (session.CharacterId.HasValue && session.CharacterId.Value != characterId)
                {
                    _channelIdByCharacterId.TryRemove(session.CharacterId.Value, out _);
                }

                session.CharacterId = characterId;
                session.MapInstanceId = mapInstanceId;
                _channelIdByCharacterId[characterId] = channelId;
            }
        }

        public IPacketSender? GetSenderByChannelId(string channelId)
        {
            return _sessionsByChannelId.TryGetValue(channelId, out var session) ? session.Sender : null;
        }

        public IPacketSender? GetSenderByCharacterId(long characterId)
        {
            if (_channelIdByCharacterId.TryGetValue(characterId, out var channelId))
            {
                return GetSenderByChannelId(channelId);
            }
            return null;
        }

        public ClientSession? GetSessionByCharacterId(long characterId)
        {
            if (_channelIdByCharacterId.TryGetValue(characterId, out var channelId) &&
                _sessionsByChannelId.TryGetValue(channelId, out var session))
            {
                return session.Sender as ClientSession;
            }
            return null;
        }

        public ClientSession? GetSession(Func<ClientSession, bool> predicate)
        {
            return _sessionsByChannelId.Values
                .Select(s => s.Sender as ClientSession)
                .Where(s => s?.HasSelectedCharacter == true)
                .FirstOrDefault(s => s != null && predicate(s));
        }

        public IEnumerable<ClientSession> GetSessions(Func<ClientSession, bool>? predicate = null)
        {
            var sessions = _sessionsByChannelId.Values
                .Select(s => s.Sender as ClientSession)
                .Where(s => s?.HasSelectedCharacter == true)
                .Cast<ClientSession>();

            return predicate == null ? sessions : sessions.Where(predicate);
        }

        public IEnumerable<SessionInfo> GetSessionsByMapInstance(Guid mapInstanceId)
        {
            return _sessionsByChannelId.Values.Where(s => s.MapInstanceId == mapInstanceId);
        }

        public IEnumerable<ClientSession> GetClientSessionsByMapInstance(Guid mapInstanceId)
        {
            return _sessionsByChannelId.Values
                .Where(s => s.MapInstanceId == mapInstanceId)
                .Select(s => s.Sender as ClientSession)
                .Where(s => s?.HasPlayerEntity == true)
                .Cast<ClientSession>();
        }

        public PlayerComponentBundle GetCharacter(Func<PlayerComponentBundle, bool> predicate)
        {
            if (TryGetCharacter(predicate, out var character))
            {
                return character;
            }
            throw new InvalidOperationException("Character not found");
        }

        public bool TryGetCharacter(Func<PlayerComponentBundle, bool> predicate, out PlayerComponentBundle character)
        {
            foreach (var session in _sessionsByChannelId.Values)
            {
                if (session.Sender is ClientSession clientSession && clientSession.HasPlayerEntity)
                {
                    var c = clientSession.Character;
                    if (predicate(c))
                    {
                        character = c;
                        return true;
                    }
                }
            }
            character = default;
            return false;
        }

        public IEnumerable<PlayerComponentBundle> GetCharacters(Func<PlayerComponentBundle, bool>? predicate = null)
        {
            var characters = _sessionsByChannelId.Values
                .Select(s => s.Sender)
                .OfType<ClientSession>()
                .Where(s => s.HasPlayerEntity)
                .Select(s => s.Character);

            return predicate == null ? characters : characters.Where(predicate);
        }

        public IEnumerable<SessionInfo> GetSessionsByGroup(long groupId)
        {
            return _sessionsByChannelId.Values.Where(s => s.GroupId == groupId);
        }

        public IEnumerable<SessionInfo> GetAllSessions()
        {
            return _sessionsByChannelId.Values;
        }

        public Task BroadcastPacketAsync(IPacket packet) => BroadcastAsync(_sessionsByChannelId.Values, packet);

        public Task BroadcastPacketAsync(IPacket packet, string excludeChannelId) =>
            BroadcastAsync(_sessionsByChannelId.Values.Where(s => s.ChannelId != excludeChannelId), packet);

        private Task BroadcastAsync(IEnumerable<SessionInfo> sessions, IPacket packet) =>
            Task.WhenAll(sessions.Select(async s =>
            {
                try
                {
                    await s.Sender.SendPacketAsync(packet);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, logLanguage[LogLanguageKey.BROADCAST_FAILED], s.ChannelId);
                }
            }));

        public async Task DisconnectByCharacterIdAsync(long characterId)
        {
            if (_channelIdByCharacterId.TryGetValue(characterId, out var channelId) &&
                _sessionsByChannelId.TryGetValue(channelId, out var session))
            {
                await session.Disconnect();
            }
        }

        public List<Subscriber> GetConnectedAccounts()
        {
            return _sessionsByChannelId.Values.Select(s =>
            {
                var clientSession = s.Sender as ClientSession;
                return new Subscriber
                {
                    Name = s.AccountName,
                    ConnectedCharacter = clientSession?.HasSelectedCharacter == true
                        ? new Data.WebApi.Character
                        {
                            Name = clientSession.Character.Name,
                            Id = clientSession.Character.CharacterId,
                            FriendRequestBlocked = clientSession.Character.FriendRequestBlocked,
                            WhisperBlocked = clientSession.Character.WhisperBlocked
                        }
                        : null
                };
            }).ToList();
        }
    }
}
