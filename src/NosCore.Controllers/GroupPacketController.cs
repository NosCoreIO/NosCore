using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NosCore.Configuration;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using NosCore.Shared.I18N;

namespace NosCore.Controllers
{
    public class GroupPacketController : PacketController
    {
        private readonly WorldConfiguration _worldConfiguration;

        [UsedImplicitly]
        public GroupPacketController()
        {
            
        }

        public GroupPacketController(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        /// <summary>
        ///     pjoin packet
        /// </summary>
        /// <param name="pjoinPacket"></param>
        public void GroupJoin(PjoinPacket pjoinPacket)
        {
            var targetSession = ServerManager.Instance.Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == (long)pjoinPacket.CharacterId);

            if (targetSession == null && pjoinPacket.RequestType != GroupRequestType.Sharing)
            {
                return;
            }

            switch (pjoinPacket.RequestType)
            {

                case GroupRequestType.Requested:
                case GroupRequestType.Invited:
                    if (pjoinPacket.CharacterId == 0 || targetSession == null || (long)pjoinPacket.CharacterId == Session.Character.CharacterId)
                    {
                        return;
                    }

                    if (targetSession.Character.Group != null && targetSession.Character.Group.IsGroupFull)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.GROUP_FULL, Session.Account.Language)
                        });
                        return;
                    }

                    if (targetSession.Character.Group != null && Session.Character.Group != null)
                    {
                        Session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_IN_GROUP, Session.Account.Language)
                        });
                        return;
                    }

                    if (Session.Character.IsRelatedToCharacter((long)pjoinPacket.CharacterId, CharacterRelationType.Blocked))
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

                    Session.Character.GroupRequestCharacterIds.Add((long)pjoinPacket.CharacterId);

                    if (Session.Character.Group == null || Session.Character.Group.Type == GroupType.Group)
                    {
                        if (targetSession.Character?.Group == null || targetSession.Character?.Group.Type == GroupType.Group)
                        {
                            targetSession.SendPacket(new DlgPacket
                            {
                                Question = Language.Instance.GetMessageFromKey(LanguageKey.INVITED_YOU_GROUP, targetSession.Account.Language),
                                YesPacket = new PjoinPacket { CharacterId = (ulong)Session.Character.CharacterId, RequestType = GroupRequestType.Accepted },
                                NoPacket = new PjoinPacket { CharacterId = (ulong)Session.Character.CharacterId, RequestType = GroupRequestType.Declined }
                            });
                        }
                    }

                    break;
            }
        }
    }
}
