using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    public class GroupPacketController : PacketController
    {
        [UsedImplicitly]
        public GroupPacketController()
        {

        }

        /// <summary>
        ///     pjoin packet
        /// </summary>
        /// <param name="pjoinPacket"></param>
        public void ManageGroup(PjoinPacket pjoinPacket)
        {
            var targetSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == pjoinPacket.CharacterId);

            if (targetSession == null && pjoinPacket.RequestType != GroupRequestType.Sharing)
            {
                Logger.Log.Error(Language.Instance.GetMessageFromKey(LanguageKey.UNABLE_TO_REQUEST_GROUP, Session.Account.Language));
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

                    if (targetSession.Character.Group.IsGroupFull)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL, Session.Account.Language)
                        });
                        return;
                    }

                    if (!targetSession.Character.Group.IsEmpty && !Session.Character.Group.IsEmpty)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_IN_GROUP, Session.Account.Language)
                        });
                        return;
                    }

                    if (Session.Character.IsRelatedToCharacter(pjoinPacket.CharacterId, CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED, Session.Account.Language)
                        });
                        return;
                    }

                    if (targetSession.Character.GroupRequestBlocked)
                    {
                        Session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_BLOCKED, Session.Account.Language)
                        });
                        return;
                    }

                    Session.Character.GroupRequestCharacterIds.Add(pjoinPacket.CharacterId);

                    if (Session.Character.Group.IsEmpty || Session.Character.Group.Type == GroupType.Group)
                    {
                        if (targetSession.Character.Group.IsEmpty || targetSession.Character?.Group.Type == GroupType.Group)
                        {
                            Session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_INVITE, Session.Account.Language) });
                            targetSession.SendPacket(new DlgPacket
                            {
                                Question = Language.Instance.GetMessageFromKey(LanguageKey.INVITED_YOU_GROUP, targetSession.Account.Language),
                                YesPacket = new PjoinPacket { CharacterId = Session.Character.CharacterId, RequestType = GroupRequestType.Accepted },
                                NoPacket = new PjoinPacket { CharacterId = Session.Character.CharacterId, RequestType = GroupRequestType.Declined }
                            });
                        }
                    }

                    break;
                case GroupRequestType.Sharing:

                    if (Session.Character.Group.IsEmpty)
                    {
                        return;
                    }

                    Session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_SHARE_INFO, Session.Account.Language)
                    });

                    Session.Character.Group.Values.Where(s => s.Character.CharacterId != Session.Character.CharacterId).ToList().ForEach(s =>
                    {
                        s.Character.GroupRequestCharacterIds.Add(s.Character.CharacterId);
                        s.SendPacket(new DlgPacket
                        {
                            Question = Language.Instance.GetMessageFromKey(LanguageKey.INVITED_GROUP_SHARE, Session.Account.Language),
                            YesPacket = new PjoinPacket { CharacterId = Session.Character.CharacterId, RequestType = GroupRequestType.AcceptedShare },
                            NoPacket = new PjoinPacket { CharacterId = Session.Character.CharacterId, RequestType = GroupRequestType.DeclinedShare }
                        });
                    });

                    
                    break;
                case GroupRequestType.Accepted:
                    if (!targetSession.Character.GroupRequestCharacterIds.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.Character.GroupRequestCharacterIds.Remove(Session.Character.CharacterId);

                    if (!Session.Character.Group.IsEmpty && !targetSession.Character.Group.IsEmpty)
                    {
                        return;
                    }

                    if (Session.Character.Group.IsGroupFull || targetSession.Character.Group.IsGroupFull)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL, Session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                          Message  = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL, targetSession.Account.Language)
                        });
                        return;
                    }

                    if (!Session.Character.Group.IsEmpty)
                    {
                        Session.Character.Group.JoinGroup(targetSession.Character.CharacterId);
                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP, targetSession.Account.Language)
                        });
                    }
                    else if (!targetSession.Character.Group.IsEmpty)
                    {
                        if (targetSession.Character.Group.Type == GroupType.Group)
                        {
                            targetSession.Character.Group.JoinGroup(Session.Character.CharacterId);
                        }
                    }
                    else
                    {
                        Session.Character.Group.GroupId = ServerManager.Instance.GetNextGroupId();
                        Session.Character.Group.Type = GroupType.Group;
                        Session.Character.Group.JoinGroup(Session.Character.CharacterId);
                        Session.Character.Group.JoinGroup(targetSession.Character.CharacterId);
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.JOINED_GROUP, Session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_ADMIN, targetSession.Account.Language)
                        });

                        targetSession.Character.Group = Session.Character.Group;
                        Session.Character.GroupRequestCharacterIds.Clear();
                        targetSession.Character.GroupRequestCharacterIds.Clear();
                    }

                    if (Session.Character.Group.Type != GroupType.Group)
                    {
                        return;
                    }

                    var currentGroup = Session.Character.Group;

                    foreach (var member in currentGroup.Values)
                    {
                        member.SendPacket(member.Character.GeneratePinit());
                        member.SendPackets(member.Character.Group.GeneratePst());
                    }

                    ServerManager.Instance.Groups[currentGroup.GroupId] = currentGroup;
                    Session.Character.MapInstance?.Broadcast(Session.Character.GeneratePidx());
                    
                    break;
                case GroupRequestType.Declined:
                    if (!targetSession.Character.GroupRequestCharacterIds.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.Character.GroupRequestCharacterIds.Remove(Session.Character.CharacterId);
                    targetSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_REFUSED, targetSession.Account.Language)
                    });
                    break;
                case GroupRequestType.AcceptedShare:
                    if (!targetSession.Character.GroupRequestCharacterIds.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    if (Session.Character.Group.IsEmpty)
                    {
                        return;
                    }

                    targetSession.Character.GroupRequestCharacterIds.Remove(Session.Character.CharacterId);
                    Session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ACCEPTED_SHARE, Session.Account.Language),
                        Type = MessageType.Whisper
                    });

                    //TODO: add a way to change respawn points when system will be done
                    break;
                case GroupRequestType.DeclinedShare:
                    if (!targetSession.Character.GroupRequestCharacterIds.Contains(Session.Character.CharacterId))
                    {
                        return;
                    }

                    targetSession.Character.GroupRequestCharacterIds.Remove(Session.Character.CharacterId);
                    targetSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.SHARED_REFUSED, targetSession.Account.Language)
                    });
                    break;
            }
        }

        public void LeaveGroup(PleavePacket pleavePacket)
        {
            var group = Session.Character.Group;

            if (group.IsEmpty)
            {
                return;
            }

            if (group.Count > 2)
            {
                group.LeaveGroup(Session.Character.CharacterId);

                if (group.IsGroupLeader(Session.Character.CharacterId))
                {
                    ServerManager.Instance.Broadcast(Session, new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.NEW_LEADER, Session.Account.Language) }, ReceiverType.OnlySomeone, string.Empty, group.Values.First().Character.CharacterId);
                }

                if (group.Type != GroupType.Group)
                {
                    return;
                }

                foreach (var member in group.Values)
                {
                    member.SendPacket(member.Character.GeneratePinit());
                    member.SendPacket(new MsgPacket
                    {
                        Message = string.Format(Language.Instance.GetMessageFromKey(LanguageKey.LEAVE_GROUP, Session.Account.Language), Session.Character.Name)
                    });
                }

                Session.SendPacket(Session.Character.GeneratePinit());
                ServerManager.Instance.Broadcast(Session.Character.GeneratePidx(true));
                Session.SendPacket(new MsgPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_LEFT, Session.Account.Language) });
            }
            else
            {
                var memberList = new List<ClientSession>();
                memberList.AddRange(group.Values);

                foreach (var member in memberList)
                {
                    member.SendPacket(new MsgPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_CLOSED, member.Account.Language), Type = MessageType.Whisper });
                    ServerManager.Instance.Broadcast(member.Character.GeneratePidx(true));
                    group.LeaveGroup(member.Character.CharacterId);
                    member.SendPacket(member.Character.GeneratePinit());
                }

                ServerManager.Instance.Groups.TryRemove(group.GroupId, out _);
            }
        }

        public void GroupTalk(GroupTalkPacket groupTalkPacket)
        {
            if (Session.Character.Group.IsEmpty)
            {
                return;
            }

            ServerManager.Instance.Broadcast(Session, Session.Character.GenerateSpk(new SpeakPacket { Message = groupTalkPacket.Message, SpeakType = SpeakType.Group }), ReceiverType.Group);
        }
    }
}
