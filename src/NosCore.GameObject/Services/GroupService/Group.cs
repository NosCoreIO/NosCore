//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


namespace NosCore.GameObject.Services.GroupService
{
    public class Group(GroupType type, ISessionGroupFactory sessionGroupFactory) : ConcurrentDictionary<Tuple<VisualType, long>, Tuple<int, INamedEntity>>,
        IBroadcastable
    {
        public short MaxPacketsBuffer { get; } = 250;

        private int _lastId;

        public ConcurrentQueue<IPacket> LastPackets { get; } = new();

        public long GroupId { get; set; } = -1;

        public GroupType Type { get; set; } = type;

        public bool IsGroupFull => Count == (long)Type;

        public new bool IsEmpty => Keys.Count(s => s.Item1 == VisualType.Player) <= 1;

        public new int Count => Keys.Count(s => s.Item1 == VisualType.Player);

        public ISessionGroup Sessions { get; set; } = sessionGroupFactory.Create();

        public PinitPacket GeneratePinit()
        {
            var i = 0;

            return new PinitPacket
            {
                GroupSize = Count == 1 ? 0 : Count,
                PinitSubPackets = Values.Select(s => s.Item2.GenerateSubPinit(Count == 1 ? i : ++i)).ToList() as List<PinitSubPacket?>
            };
        }

        public List<PstPacket> GeneratePst()
        {
            var i = 0;

            return Values.OrderBy(s => s.Item1).Select(s => s.Item2).Select(member => new PstPacket
            {
                Type = member.VisualType,
                VisualId = member.VisualId,
                GroupOrder = ++i,
                HpLeft = (int)(member.Hp / (float)member.MaxHp * 100),
                MpLeft = (int)(member.Mp / (float)member.MaxMp * 100),
                HpLoad = member.MaxHp,
                MpLoad = member.MaxMp,
                Race = member.Race,
                Gender = (member as ICharacterEntity)?.Gender ?? GenderType.Male,
                Morph = member.Morph,
                BuffIds = null
            }).ToList();
        }

        public bool IsGroupLeader(long visualId)
        {
            var leader = Values.OrderBy(s => s.Item1).FirstOrDefault(s => s.Item2.VisualType == VisualType.Player);
            return (Count > 1) && (leader?.Item2.VisualId == visualId);
        }

        public void JoinGroup(INamedEntity namedEntity)
        {
            if (namedEntity is ICharacterEntity characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Add(characterEntity.Channel);
            }

            TryAdd(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId),
                new Tuple<int, INamedEntity>(++_lastId, namedEntity));
        }

        public void LeaveGroup(INamedEntity namedEntity)
        {
            if (namedEntity is ICharacterEntity characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Remove(characterEntity.Channel);
            }

            TryRemove(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId), out _);
        }
    }
}
