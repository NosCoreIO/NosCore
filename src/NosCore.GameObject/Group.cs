using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using NosCore.Core.Serializing;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class Group : ConcurrentDictionary<long, IPlayableEntity>
    {
        public Group(GroupType type)
        {
            Type = type;
        }

        public long GroupId { get; set; }

        public GroupType Type { get; set; }

        public bool IsGroupFull => Count == (long) Type;

        public PinitPacket GeneratePinit()
        {
            var i = 0;

            return new PinitPacket
            {
                GroupSize = Count,
                PinitSubPackets = Values.Select(s => s.GenerateSubPinit(++i)).ToList()
            };
        }

        public List<PstPacket> GeneratePst()
        {
            var packetList = new List<PstPacket>();
            var i = 0;

            foreach (var member in Values)
            {
                packetList.Add(new PstPacket
                {
                    Type = member.VisualType,
                    VisualId = member.VisualId,
                    GroupOrder = ++i,
                    HpLeft = (int)(member.Hp / (float)member.MaxHp * 100),
                    MpLeft = (int)(member.Mp / (float)member.MaxMp * 100),
                    HpLoad = member.MaxHp,
                    MpLoad = member.MaxMp,
                    Class = member.Class,
                    Gender = (member as ICharacterEntity)?.Gender ?? GenderType.Male,
                    Morph = member.Morph,
                    BuffIds = null
                });
            }

            return packetList;
        }

        public bool IsMemberOfGroup(long characterId)
        {
            return this.Any(s => s.Value.VisualId == characterId);
        }

        public bool IsGroupLeader(long characterId)
        {
            var leader = Values.OrderBy(s => s.LastGroupJoin).FirstOrDefault();
            return Count > 0 && leader != null && leader.VisualId == characterId;
        }

        public void JoinGroup(long characterId)
        {
            var session = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == characterId);

            if (session == null)
            {
                return;
            }

            session.Character.Group = this;
            session.Character.LastGroupJoin = DateTime.Now;
            TryAdd(session.Character.CharacterId, session.Character);
        }

        public void LeaveGroup(long characterId)
        {
            var session = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == characterId);

            if (session == null)
            {
                return;
            }

            session.Character.Group = new Group(GroupType.Group);

            TryRemove(session.Character.CharacterId, out _);

            foreach (var member in Values)
            {
                var groupMember = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == member.VisualId);

                groupMember?.SendPacket(groupMember.Character.Group.GeneratePinit());
            }
        }
    }
}

