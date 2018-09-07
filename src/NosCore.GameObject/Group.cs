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
    public class Group
    {

        public Group(GroupType type)
        {
            Characters = new ConcurrentDictionary<long, ClientSession>();
            Type = type;
        }

        public long GroupId { get; set; }

        public GroupType Type { get; set; }

        public bool IsGroupFull => Characters.Count == (long) Type;

        public ConcurrentDictionary<long, ClientSession> Characters { get; set; }

        public List<PstPacket> GeneratePst(ClientSession session)
        {
            var packetList = new List<PstPacket>();
            int i = 0;

            foreach (var member in Characters.Values)
            {
                packetList.Add(new PstPacket
                {
                    Type = member.Character.VisualType,
                    VisualId = member.Character.VisualId,
                    GroupOrder = ++i,
                    HpLeft = (int)(member.Character.Hp / member.Character.HPLoad() * 100),
                    MpLeft = (int)(member.Character.Mp / member.Character.MPLoad() * 100),
                    HpLoad = (int)member.Character.HPLoad(),
                    MpLoad = (int)member.Character.MPLoad(),
                    Class = (CharacterClassType)member.Character.Class,
                    Gender = member.Character.Gender,
                    Morph = member.Character.Morph
                    //TODO: Add buffs if member isn't equal to "session"
                });
            }

            return packetList;

        }

        public bool IsMemberOfGroup(ClientSession session)
        {
            return IsMemberOfGroup(session.Character.CharacterId);
        }

        public bool IsMemberOfGroup(long characterId)
        {
            return Characters.Any(s => s.Value.Character.CharacterId == characterId);
        }

        public bool IsGroupLeader(ClientSession session)
        {
            var leader = Characters.Values.OrderBy(s => s.Character.LastGroupJoin).FirstOrDefault();
            return Characters.Count > 0 && leader != null && leader == session;
        }

        public void JoinGroup(ClientSession session)
        {
            session.Character.Group = this;
            session.Character.LastGroupJoin = DateTime.Now;
            Characters.TryAdd(session.Character.CharacterId, session);
        }

        public void LeaveGroup(ClientSession session)
        {
            session.Character.Group = null;

            Characters.TryRemove(session.Character.CharacterId, out _);

            if (!IsGroupLeader(session) || Characters.Count <= 1)
            {
                return;
            }

            foreach (var member in Characters)
            {
                member.Value.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEADER_CHANGE, member.Value.Account.Language),
                    Type = MessageType.Whisper
                });
            }
        }

        public PinitPacket GeneratePinit()
        {
            var subPackets = new List<PinitSubPacket>();
            int i = 0;

            foreach (var member in Characters.Values)
            {
                subPackets.Add(new PinitSubPacket
                {
                    VisualType = member.Character.VisualType,
                    VisualId = member.Character.CharacterId,
                    GroupPosition = ++i,
                    Level = member.Character.Level,
                    Name = member.Character.Name,
                    Unknown = 0,
                    Gender = member.Character.Gender,
                    Class = (CharacterClassType)member.Character.Class,
                    Morph = member.Character.Morph,
                    HeroLevel = member.Character.HeroLevel
                });
            }

            return new PinitPacket
            {
                GroupSize = i,
                PinitSubPackets = subPackets
            };
        }
    }
}

