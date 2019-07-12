using System;
using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        private readonly IFriendHttpClient _friendHttpClient;
        public FinsPacketHandler(IFriendHttpClient friendHttpClient)
        {
            _friendHttpClient = friendHttpClient;
        }

        public override void Execute(FinsPacket finsPacket, ClientSession session)
        {
            var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == finsPacket.CharacterId);
            if (targetCharacter != null)
            {
                var result = _friendHttpClient.AddFriend(new FriendShipRequest { CharacterId = session.Character.CharacterId, FinsPacket = finsPacket });

                switch (result)
                {
                    case LanguageKey.FRIENDLIST_FULL:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.BLACKLIST_BLOCKED:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.ALREADY_FRIEND:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND,
                                    session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_BLOCKED:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                                    session.Character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_SENT:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                                    session.Character.AccountLanguage)
                        });
                        targetCharacter.SendPacket(new DlgPacket
                        {
                            Question = string.Format(
                                Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD, session.Character.AccountLanguage),
                                session.Character.Name),
                            YesPacket = new FinsPacket
                            { Type = FinsPacketType.Accepted, CharacterId = session.Character.VisualId },
                            NoPacket = new FinsPacket
                            { Type = FinsPacketType.Rejected, CharacterId = session.Character.VisualId }
                        });
                        break;

                    case LanguageKey.FRIEND_ADDED:
                        session.Character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                    session.Character.AccountLanguage)
                        });
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                session.Character.AccountLanguage)
                        });

                        targetCharacter.SendPacket(targetCharacter.GenerateFinit(_webApiAccess));
                        session.Character.SendPacket(session.Character.GenerateFinit(_webApiAccess));
                        break;

                    case LanguageKey.FRIEND_REJECTED:
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                session.Character.AccountLanguage)
                        });
                        break;

                    default:
                        throw new ArgumentException();
                }
            }
        }
    }
}
