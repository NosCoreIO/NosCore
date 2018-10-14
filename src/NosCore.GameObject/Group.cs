using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class Group : ConcurrentDictionary<Tuple<VisualType, long>, Tuple<DateTime, INamedEntity>>
    {
        public Group(GroupType type)
        {
            Type = type;
            GroupId = -1;
        }

        public long GroupId { get; set; }

        public GroupType Type { get; set; }

        public bool IsGroupFull => Count == (long)Type;

        public new bool IsEmpty => Keys.Count(s => s.Item1 == VisualType.Monster) == 0;

        public new int Count => Keys.Count(s => s.Item1 == VisualType.Player);

        public PinitPacket GeneratePinit()
        {
            var i = 0;

            return new PinitPacket
            {
                GroupSize = Count == 1 ? 0 : Count,
                PinitSubPackets = Values.Select(s => s.Item2.GenerateSubPinit(Count == 1 ? i : ++i)).ToList()
            };
        }

        public List<PstPacket> GeneratePst()
        {
            var i = 0;

            return Values.Select(s => s.Item2).Select(member => new PstPacket
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
            }).ToList();
        }

        public bool IsGroupLeader(long visualId)
        {
            var leader = Values.OrderBy(s => s.Item1).FirstOrDefault(s => s.Item2.VisualType == VisualType.Player);
            return Count > 0 && leader?.Item2.VisualId == visualId;
        }

        public void JoinGroup(INamedEntity namedEntity)
        {
            TryAdd(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId), new Tuple<DateTime, INamedEntity>(DateTime.Now, namedEntity));
        }

        public void LeaveGroup(INamedEntity namedEntity)
        {
            TryRemove(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId), out _);
        }
    }
}

