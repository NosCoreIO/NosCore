//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.Concurrency;
using NosCore.GameObject.Ecs;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.ScriptedInstanceService;
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
using System.Threading;


namespace NosCore.GameObject.Services.GroupService
{
    public class Group(GroupType type, ISessionGroupFactory sessionGroupFactory) : ConcurrentDictionary<Tuple<VisualType, long>, Tuple<int, INamedEntity>>,
        IBroadcastable
    {
        public short MaxPacketsBuffer { get; } = 250;

        private int _lastId;

        // Held while a group is formed or grown, which reads one group and writes another.
        public static AsyncLock MembershipLock { get; } = new();

        private int _lootTurn;

        public ConcurrentQueue<IPacket> LastPackets { get; } = new();

        public long GroupId { get; set; } = -1;

        public GroupType Type { get; set; } = type;

        // The raid a team was opened for, and its run once the leader has started it.
        public ScriptedInstance? Raid { get; set; }

        public ScriptedInstanceRun? RaidRun { get; set; }

        // Who the raid team's leader has invited and has not answered yet.
        public ConcurrentDictionary<long, byte> Invited { get; } = new();

        public bool IsGroupFull => Count >= (long)Type;

        public new bool IsEmpty => Keys.Count(s => s.Item1 == VisualType.Player) <= 1;

        public new int Count => Keys.Count(s => s.Item1 == VisualType.Player);

        public ISessionGroup Sessions { get; set; } = sessionGroupFactory.Create();

        // The place a member holds is the same wherever it is told: who joined first comes first.
        public IEnumerable<INamedEntity> MembersInJoiningOrder => Values.OrderBy(s => s.Item1).Select(s => s.Item2);

        public PinitPacket GeneratePinit()
        {
            var i = 0;

            return new PinitPacket
            {
                GroupSize = Count == 1 ? 0 : Count,
                PinitSubPackets = MembersInJoiningOrder.Select(s => s.GenerateSubPinit(Count == 1 ? i : ++i)).ToList() as List<PinitSubPacket?>
            };
        }

        public List<PstPacket> GeneratePst()
        {
            var i = 0;

            return MembersInJoiningOrder.Select(member => new PstPacket
            {
                Type = member.VisualType,
                VisualId = member.VisualId,
                GroupOrder = ++i,
                HpLeft = member.MaxHp > 0 ? (int)(member.Hp / (float)member.MaxHp * 100) : 0,
                MpLeft = member.MaxMp > 0 ? (int)(member.Mp / (float)member.MaxMp * 100) : 0,
                HpLoad = member.MaxHp,
                MpLoad = member.MaxMp,
                Race = member.Race,
                Gender = member is PlayerComponentBundle player ? player.Gender : GenderType.Male,
                Morph = member.Morph,
                BuffIds = null
            }).ToList();
        }

        public INamedEntity? Leader => MembersInJoiningOrder.FirstOrDefault(s => s.VisualType == VisualType.Player);

        public bool IsGroupLeader(long visualId) => (Count > 1) && (Leader?.VisualId == visualId);

        public bool IsMember(long characterId) => ContainsKey(new Tuple<VisualType, long>(VisualType.Player, characterId));

        public int NextLootTurn() => (Interlocked.Increment(ref _lootTurn) - 1) & int.MaxValue;

        public void JoinGroup(INamedEntity namedEntity)
        {
            if (namedEntity is PlayerComponentBundle characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Add(characterEntity.Channel);
            }

            TryAdd(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId),
                new Tuple<int, INamedEntity>(Interlocked.Increment(ref _lastId), namedEntity));
        }

        public void LeaveGroup(INamedEntity namedEntity)
        {
            if (namedEntity is PlayerComponentBundle characterEntity && (characterEntity.Channel != null))
            {
                Sessions.Remove(characterEntity.Channel);
            }

            TryRemove(new Tuple<VisualType, long>(namedEntity.VisualType, namedEntity.VisualId), out _);
        }
    }
}
