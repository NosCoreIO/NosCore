//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.Interfaces;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BroadcastService
{
    public class SessionRegistry(ILogger logger) : ISessionRegistry
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

        public void UpdateCharacter(string channelId, long characterId, Guid? mapInstanceId, ICharacterEntity? character)
        {
            if (_sessionsByChannelId.TryGetValue(channelId, out var session))
            {
                if (session.CharacterId.HasValue && session.CharacterId.Value != characterId)
                {
                    _channelIdByCharacterId.TryRemove(session.CharacterId.Value, out _);
                }

                session.CharacterId = characterId;
                session.MapInstanceId = mapInstanceId;
                session.Character = character;
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

        public IEnumerable<SessionInfo> GetSessionsByMapInstance(Guid mapInstanceId)
        {
            return _sessionsByChannelId.Values.Where(s => s.MapInstanceId == mapInstanceId);
        }

        public IEnumerable<SessionInfo> GetSessionsByGroup(long groupId)
        {
            return _sessionsByChannelId.Values.Where(s => s.GroupId == groupId);
        }

        public IEnumerable<SessionInfo> GetAllSessions()
        {
            return _sessionsByChannelId.Values;
        }

        public ICharacterEntity? GetCharacter(Func<ICharacterEntity, bool> predicate)
        {
            return _sessionsByChannelId.Values
                .Where(s => s.Character != null)
                .Select(s => s.Character!)
                .FirstOrDefault(predicate);
        }

        public IEnumerable<ICharacterEntity> GetCharacters(Func<ICharacterEntity, bool>? predicate = null)
        {
            var characters = _sessionsByChannelId.Values
                .Where(s => s.Character != null)
                .Select(s => s.Character!);

            return predicate == null ? characters : characters.Where(predicate);
        }

        public async Task BroadcastPacketAsync(IPacket packet)
        {
            var tasks = _sessionsByChannelId.Values.Select(async s =>
            {
                try
                {
                    await s.Sender.SendPacketAsync(packet);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Broadcast to {ChannelId} failed", s.ChannelId);
                }
            });
            await Task.WhenAll(tasks);
        }

        public async Task BroadcastPacketAsync(IPacket packet, string excludeChannelId)
        {
            var tasks = _sessionsByChannelId.Values
                .Where(s => s.ChannelId != excludeChannelId)
                .Select(async s =>
                {
                    try
                    {
                        await s.Sender.SendPacketAsync(packet);
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "Broadcast to {ChannelId} failed", s.ChannelId);
                    }
                });
            await Task.WhenAll(tasks);
        }

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
                new Subscriber
                {
                    Name = s.AccountName,
                    ConnectedCharacter = s.Character == null ? null : new Data.WebApi.Character
                    {
                        Name = s.Character.Name!,
                        Id = s.Character.VisualId,
                        FriendRequestBlocked = s.Character.FriendRequestBlocked
                    }
                }).ToList();
        }
    }
}
