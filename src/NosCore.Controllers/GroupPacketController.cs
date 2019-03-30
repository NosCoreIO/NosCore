﻿//  __  _  __    __   ___ __  ___ ___  
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

using System.Collections.Generic;
using System.Linq;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ChannelMatcher;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using Serilog;

namespace NosCore.Controllers
{
    public class GroupPacketController : PacketController
    {
        private readonly ILogger _logger;

        public GroupPacketController() { }

        public GroupPacketController(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     pjoin packet
        /// </summary>
        /// <param name="pjoinPacket"></param>
        public void ManageGroup(PjoinPacket pjoinPacket)
        {
            var targetSession =
                Broadcaster.Instance.GetCharacter(s =>
                    s.VisualId == pjoinPacket.CharacterId);

            if (targetSession == null && pjoinPacket.RequestType != GroupRequestType.Sharing)
            {
                _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.UNABLE_TO_REQUEST_GROUP,
                    Session.Account.Language));
                return;
            }

            switch (pjoinPacket.RequestType)
            {
                case GroupRequestType.Requested:
                case GroupRequestType.Invited:
                    if (pjoinPacket.CharacterId == Session.Character.CharacterId)
                    {
                        return;
                    }

                    if (targetSession.Group.IsGroupFull)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL,
                                Session.Account.Language)
                        });
                        return;
                    }

                    if (targetSession.Group.Count > 1 && Session.Character.Group.Count > 1)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_IN_GROUP,
                                Session.Account.Language)
                        });
                        return;
                    }

                    if (Session.Character.IsRelatedToCharacter(pjoinPacket.CharacterId, CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                Session.Account.Language)
                        });
                        return;
                    }

                    if (targetSession.GroupRequestBlocked)
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_BLOCKED,
                                Session.Account.Language)
                        });
                        return;
                    }

                    Session.Character.GroupRequestCharacterIds.TryAdd(pjoinPacket.CharacterId, pjoinPacket.CharacterId);

                    if ((Session.Character.Group.Count == 1 || Session.Character.Group.Type == GroupType.Group)
                        && (targetSession.Group.Count == 1 || targetSession?.Group.Type == GroupType.Group))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Session.GetMessageFromKey(LanguageKey.GROUP_INVITE)
                        });
                        targetSession.SendPacket(new DlgPacket
                        {
                            Question = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.INVITED_YOU_GROUP, targetSession.AccountLanguage), Session.Character.Name),
                            YesPacket = new PjoinPacket
                            {
                                CharacterId = Session.Character.CharacterId,
                                RequestType = GroupRequestType.Accepted
                            },
                            NoPacket = new PjoinPacket
                            {
                                CharacterId = Session.Character.CharacterId,
                                RequestType = GroupRequestType.Declined
                            }
                        });
                    }

                    break;
                case GroupRequestType.Sharing:

                    if (Session.Character.Group.Count == 1)
                    {
                        return;
                    }

                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_SHARE_INFO,
                            Session.Account.Language)
                    });

                    Session.Character.Group.Values.Where(s => s.Item2.VisualId != Session.Character.CharacterId)
                        .ToList().ForEach(s =>
                        {
                            var session =
                                Broadcaster.Instance.GetCharacter(v =>
                                    v.VisualId == s.Item2.VisualId);

                            if (session == null)
                            {
                                return;
                            }

                            session.GroupRequestCharacterIds.TryAdd(s.Item2.VisualId, s.Item2.VisualId);
                            session.SendPacket(new DlgPacket
                            {
                                Question = Language.Instance.GetMessageFromKey(LanguageKey.INVITED_GROUP_SHARE,
                                    Session.Account.Language),
                                YesPacket = new PjoinPacket
                                {
                                    CharacterId = Session.Character.CharacterId,
                                    RequestType = GroupRequestType.AcceptedShare
                                },
                                NoPacket = new PjoinPacket
                                {
                                    CharacterId = Session.Character.CharacterId,
                                    RequestType = GroupRequestType.DeclinedShare
                                }
                            });
                        });

                    break;
                case GroupRequestType.Accepted:
                    if (!targetSession.GroupRequestCharacterIds.Values.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(Session.Character.CharacterId, out _);

                    if (Session.Character.Group.Count > 1 && targetSession.Group.Count > 1)
                    {
                        return;
                    }

                    if (Session.Character.Group.IsGroupFull || targetSession.Group.IsGroupFull)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL,
                                Session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL, targetSession.AccountLanguage)
                        });
                        return;
                    }

                    if (Session.Character.Group.Count > 1)
                    {
                        targetSession.JoinGroup(Session.Character.Group);
                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP, targetSession.AccountLanguage)
                        });
                    }
                    else if (targetSession.Group.Count > 1)
                    {
                        if (targetSession.Group.Type == GroupType.Group)
                        {
                            Session.Character.JoinGroup(targetSession.Group);
                        }
                    }
                    else
                    {
                        targetSession.Group.GroupId = GroupAccess.Instance.GetNextGroupId();
                        Session.Character.JoinGroup(targetSession.Group);
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP,
                                Session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_ADMIN, targetSession.AccountLanguage)
                        });

                        Session.Character.Group = targetSession.Group;
                        Session.Character.GroupRequestCharacterIds.Clear();
                    }

                    if (Session.Character.Group.Type != GroupType.Group)
                    {
                        return;
                    }

                    var currentGroup = Session.Character.Group;

                    foreach (var member in currentGroup.Values.Where(s => s.Item2 is ICharacterEntity))
                    {
                        var session =
                            Broadcaster.Instance.GetCharacter(s =>
                                s.VisualId == member.Item2.VisualId);
                        session?.SendPacket(currentGroup.GeneratePinit());
                        session?.SendPackets(currentGroup.GeneratePst(session.VisualId));
                    }

                    GroupAccess.Instance.Groups[currentGroup.GroupId] = currentGroup;
                    Session.Character.MapInstance?.Sessions.SendPacket(
                        Session.Character.Group.GeneratePidx(Session.Character));

                    break;
                case GroupRequestType.Declined:
                    if (!targetSession.GroupRequestCharacterIds.Values.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(Session.Character.CharacterId, out _);
                    targetSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_REFUSED, targetSession.AccountLanguage)
                    });
                    break;
                case GroupRequestType.AcceptedShare:
                    if (!targetSession.GroupRequestCharacterIds.Values.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    if (Session.Character.Group.Count == 1)
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(Session.Character.CharacterId, out _);
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ACCEPTED_SHARE,
                            Session.Account.Language),
                        Type = MessageType.White
                    });

                    //TODO: add a way to change respawn points when system will be done
                    break;
                case GroupRequestType.DeclinedShare:
                    if (!targetSession.GroupRequestCharacterIds.Values.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.GroupRequestCharacterIds.TryRemove(Session.Character.CharacterId, out _);
                    targetSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.SHARED_REFUSED, targetSession.AccountLanguage)
                    });
                    break;
                default:
                    _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.GROUPREQUESTTYPE_UNKNOWN));
                    break;
            }
        }

        public void LeaveGroup(PleavePacket __)
        {
            var group = Session.Character.Group;

            if (group.Count == 1)
            {
                return;
            }

            if (group.Count > 2)
            {
                if (group.IsGroupLeader(Session.Character.CharacterId))
                {
                    Session.Character.LeaveGroup();

                    var session = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == group.Values.OrderBy(t => t.Item1).FirstOrDefault(u =>
                        u.Item2.VisualType == VisualType.Player).Item2.VisualId);

                    if (session == null)
                    {
                        return;
                    }

                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NEW_LEADER, Session.Account.Language)
                    });
                }
                else
                {
                    Session.Character.LeaveGroup();
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values.Where(s => s.Item2 is ICharacterEntity))
                {
                    var character = member.Item2 as ICharacterEntity;
                    character.SendPacket(character.Group.GeneratePinit());
                    character.SendPacket(new MsgPacket
                    {
                        Message = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.LEAVE_GROUP, Session.Account.Language),
                            Session.Character.Name)
                    });
                }

                Session.SendPacket(Session.Character.Group.GeneratePinit());
                Session.SendPacket(new MsgPacket
                    {Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEFT, Session.Account.Language)});
                Session.Character.MapInstance.Sessions.SendPacket(
                    Session.Character.Group.GeneratePidx(Session.Character));
            }
            else
            {
                var memberList = new List<INamedEntity>();
                memberList.AddRange(group.Values.Select(s => s.Item2));

                foreach (var member in memberList.Where(s => s is ICharacterEntity))
                {
                    var session =
                        Broadcaster.Instance.GetCharacter(s =>
                            s.VisualId == member.VisualId);

                    if (session == null)
                    {
                        continue;
                    }

                    session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED, session.AccountLanguage),
                        Type = MessageType.White
                    });

                    session.LeaveGroup();
                    session.SendPacket(session.Group.GeneratePinit());
                    Broadcaster.Instance.Sessions.SendPacket(session.Group.GeneratePidx(session));
                }

                GroupAccess.Instance.Groups.TryRemove(group.GroupId, out _);
            }
        }

        public void GroupTalk(GroupTalkPacket groupTalkPacket)
        {
            if (Session.Character.Group.Count == 1)
            {
                return;
            }

            Session.Character.Group.Sessions.SendPacket(
                Session.Character.GenerateSpk(new SpeakPacket
                    {Message = groupTalkPacket.Message, SpeakType = SpeakType.Group}));
        }
    }
}