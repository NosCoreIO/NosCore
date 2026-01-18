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
using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.BroadcastService
{
    public interface ISessionRegistry
    {
        void Register(SessionInfo sessionInfo);
        void Unregister(string channelId);
        void UpdatePlayer(string channelId, long characterId, Guid? mapInstanceId, Entity entity);

        IPacketSender? GetSenderByChannelId(string channelId);
        IPacketSender? GetSenderByCharacterId(long characterId);
        ClientSession? GetSessionByCharacterId(long characterId);
        IEnumerable<SessionInfo> GetSessionsByMapInstance(Guid mapInstanceId);
        IEnumerable<SessionInfo> GetSessionsByGroup(long groupId);
        IEnumerable<SessionInfo> GetAllSessions();

        PlayerContext? GetPlayer(Func<PlayerContext, bool> predicate);
        IEnumerable<PlayerContext> GetPlayers(Func<PlayerContext, bool>? predicate = null);

        Task BroadcastPacketAsync(IPacket packet);
        Task BroadcastPacketAsync(IPacket packet, string excludeChannelId);
        Task DisconnectByCharacterIdAsync(long characterId);

        List<Subscriber> GetConnectedAccounts();
    }

    public class SessionInfo
    {
        public required string ChannelId { get; init; }
        public required long SessionId { get; init; }
        public required IPacketSender Sender { get; init; }
        public required string AccountName { get; init; }
        public required Func<Task> Disconnect { get; init; }

        public long? CharacterId { get; set; }
        public Guid? MapInstanceId { get; set; }
        public long? GroupId { get; set; }
        public Entity? PlayerEntity { get; set; }
    }
}
