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

using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Networking.SessionGroup;


namespace NosCore.GameObject
{
    public class Group(GroupType type, ISessionGroupFactory sessionGroupFactory) : ConcurrentDictionary<(VisualType VisualType, long VisualId), int>,
        IBroadcastable
    {
        public short MaxPacketsBuffer { get; } = 250;

        private int _lastId;

        public ConcurrentQueue<IPacket> LastPackets { get; } = new();

        public long GroupId { get; set; } = -1;

        public GroupType Type { get; set; } = type;

        public bool IsGroupFull => Count == (long)Type;

        public new bool IsEmpty => Keys.Count(s => s.VisualType == VisualType.Player) <= 1;

        public new int Count => Keys.Count(s => s.VisualType == VisualType.Player);

        public ISessionGroup Sessions { get; set; } = sessionGroupFactory.Create();

        public IEnumerable<(VisualType VisualType, long VisualId)> GetMemberIds()
        {
            return Keys.OrderBy(k => this[k]);
        }

        public IEnumerable<long> GetPlayerIds()
        {
            return Keys.Where(k => k.VisualType == VisualType.Player).OrderBy(k => this[k]).Select(k => k.VisualId);
        }

        public PinitPacket GeneratePinit(ISessionRegistry sessionRegistry)
        {
            var i = 0;
            var subPackets = new List<PinitSubPacket?>();

            foreach (var (visualType, visualId) in GetMemberIds())
            {
                if (visualType == VisualType.Player)
                {
                    var player = sessionRegistry.GetPlayer(p => p.CharacterId == visualId);
                    if (player != null)
                    {
                        subPackets.Add(new PinitSubPacket
                        {
                            VisualType = VisualType.Player,
                            VisualId = visualId,
                            GroupPosition = Count == 1 ? i : ++i,
                            Level = player.Value.Level,
                            Name = player.Value.Name,
                            Gender = player.Value.Gender,
                            Race = (byte)player.Value.Class,
                            Morph = player.Value.Morph,
                            HeroLevel = player.Value.HeroLevel
                        });
                    }
                }
            }

            return new PinitPacket
            {
                GroupSize = Count == 1 ? 0 : Count,
                PinitSubPackets = subPackets
            };
        }

        public List<PstPacket> GeneratePst(ISessionRegistry sessionRegistry)
        {
            var i = 0;
            var packets = new List<PstPacket>();

            foreach (var (visualType, visualId) in GetMemberIds())
            {
                if (visualType == VisualType.Player)
                {
                    var player = sessionRegistry.GetPlayer(p => p.CharacterId == visualId);
                    if (player != null)
                    {
                        var p = player.Value;
                        packets.Add(new PstPacket
                        {
                            Type = VisualType.Player,
                            VisualId = visualId,
                            GroupOrder = ++i,
                            HpLeft = p.MaxHp > 0 ? (int)(p.Hp / (float)p.MaxHp * 100) : 0,
                            MpLeft = p.MaxMp > 0 ? (int)(p.Mp / (float)p.MaxMp * 100) : 0,
                            HpLoad = p.MaxHp,
                            MpLoad = p.MaxMp,
                            Race = (short)p.Class,
                            Gender = p.Gender,
                            Morph = p.Morph,
                            BuffIds = null
                        });
                    }
                }
            }

            return packets;
        }

        public bool IsGroupLeader(long visualId)
        {
            var leader = Keys
                .Where(k => k.VisualType == VisualType.Player)
                .OrderBy(k => this[k])
                .FirstOrDefault();
            return Count > 1 && leader.VisualId == visualId;
        }

        public void JoinGroup(PlayerContext player)
        {
            if (player.Channel != null)
            {
                Sessions.Add(player.Channel);
            }

            TryAdd((VisualType.Player, player.CharacterId), ++_lastId);
        }

        public void JoinGroup(VisualType visualType, long visualId, IChannel? channel = null)
        {
            if (channel != null)
            {
                Sessions.Add(channel);
            }

            TryAdd((visualType, visualId), ++_lastId);
        }

        public void LeaveGroup(PlayerContext player)
        {
            if (player.Channel != null)
            {
                Sessions.Remove(player.Channel);
            }

            TryRemove((VisualType.Player, player.CharacterId), out _);
        }

        public void LeaveGroup(VisualType visualType, long visualId, IChannel? channel = null)
        {
            if (channel != null)
            {
                Sessions.Remove(channel);
            }

            TryRemove((visualType, visualId), out _);
        }
    }
}