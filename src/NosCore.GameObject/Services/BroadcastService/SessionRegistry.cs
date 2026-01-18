//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.BroadcastService
{
    public class SessionRegistry : ISessionRegistry
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

        public void UpdatePlayer(string channelId, long characterId, Guid? mapInstanceId, Entity entity)
        {
            if (_sessionsByChannelId.TryGetValue(channelId, out var session))
            {
                if (session.CharacterId.HasValue && session.CharacterId.Value != characterId)
                {
                    _channelIdByCharacterId.TryRemove(session.CharacterId.Value, out _);
                }

                session.CharacterId = characterId;
                session.MapInstanceId = mapInstanceId;
                session.PlayerEntity = entity;
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

        public PlayerContext? GetPlayer(Func<PlayerContext, bool> predicate)
        {
            foreach (var session in _sessionsByChannelId.Values)
            {
                if (session.Sender is ClientSession clientSession && clientSession.TryGetPlayer(out var player))
                {
                    if (predicate(player))
                    {
                        return player;
                    }
                }
            }
            return null;
        }

        public IEnumerable<PlayerContext> GetPlayers(Func<PlayerContext, bool>? predicate = null)
        {
            var players = _sessionsByChannelId.Values
                .Select(s => s.Sender as ClientSession)
                .Where(cs => cs != null && cs.TryGetPlayer(out _))
                .Select(cs => cs!.Player);

            return predicate == null ? players : players.Where(predicate);
        }

        public async Task BroadcastPacketAsync(IPacket packet)
        {
            await Task.WhenAll(_sessionsByChannelId.Values.Select(s => s.Sender.SendPacketAsync(packet)));
        }

        public async Task BroadcastPacketAsync(IPacket packet, string excludeChannelId)
        {
            await Task.WhenAll(_sessionsByChannelId.Values
                .Where(s => s.ChannelId != excludeChannelId)
                .Select(s => s.Sender.SendPacketAsync(packet)));
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
            {
                var clientSession = s.Sender as ClientSession;
                PlayerContext? player = null;
                if (clientSession?.TryGetPlayer(out var p) == true)
                {
                    player = p;
                }

                return new Subscriber
                {
                    Name = s.AccountName,
                    ConnectedCharacter = player == null ? null : new Data.WebApi.Character
                    {
                        Name = player.Value.Name,
                        Id = player.Value.VisualId,
                        FriendRequestBlocked = player.Value.CharacterData.FriendRequestBlocked
                    }
                };
            }).ToList();
        }
    }
}
