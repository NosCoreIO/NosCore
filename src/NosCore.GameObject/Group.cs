using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using NosCore.Core.Serializing;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.I18N;

namespace NosCore.GameObject
{
    public class Group : ConcurrentDictionary<long, ClientSession>
    {
        public Group(GroupType type)
        {
            Type = type;
        }

        public long GroupId { get; set; }

        public GroupType Type { get; set; }

        public bool IsGroupFull => Count == (long) Type;

        public bool IsMemberOfGroup(long characterId)
        {
            return this.Any(s => s.Value.Character.CharacterId == characterId);
        }

        public bool IsGroupLeader(long characterId)
        {
            var leader = Values.OrderBy(s => s.Character.LastGroupJoin).FirstOrDefault();
            return Count > 0 && leader != null && leader.Character.CharacterId == characterId;
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
            TryAdd(session.Character.CharacterId, session);
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

            if (Count > 1)
            {
                foreach (var member in Values)
                {
                    member.SendPacket(member.Character.GeneratePinit());
                }
            }

            if (!IsGroupLeader(session.Character.CharacterId))
            {
                return;
            }

            foreach (var member in Values)
            {
                member.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEADER_CHANGE, member.Account.Language),
                    Type = MessageType.Whisper
                });
            }
        }
    }
}

