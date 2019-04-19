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
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;
        private readonly WorldConfiguration _worldConfiguration;

        public FinsPacketHandler(WorldConfiguration worldConfiguration, ILogger logger)
        {
            _worldConfiguration = worldConfiguration;
            _logger = logger;
        }
        public override void Execute(FinsPacket finsPacket, ClientSession session)
        {
            if (_worldConfiguration.FeatureFlags[FeatureFlag.FriendServerEnabled])
            {
                var server = WebApiAccess.Instance.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                    ?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
                //WebApiAccess.Instance.Post<FriendShip>("api/friend", server.WebApi);
            }
            else
            {
                if (session.Character.IsFriendListFull)
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL,
                            session.Account.Language)
                    });
                    return;
                }

                if (session.Character.IsRelatedToCharacter(finsPacket.CharacterId, CharacterRelationType.Blocked))
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                            session.Account.Language)
                    });
                    return;
                }

                if (session.Character.IsRelatedToCharacter(finsPacket.CharacterId, CharacterRelationType.Friend))
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND,
                            session.Account.Language)
                    });
                    return;
                }

                //TODO: Make character options & check if the character has friend requests blocked
                if (session.Character.FriendRequestBlocked)
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                            session.Account.Language)
                    });
                    return;
                }

                var targetSession =
                    Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == finsPacket.CharacterId);

                if (targetSession == null)
                {
                    return;
                }

                if (!targetSession.FriendRequestCharacters.Values.Contains(session.Character.CharacterId))
                {
                    session.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                            session.Account.Language)
                    });

                    targetSession.SendPacket(new DlgPacket
                    {
                        Question = string.Format(
                            Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD, session.Account.Language),
                            session.Character.Name),
                        YesPacket = new FinsPacket
                        { Type = FinsPacketType.Accepted, CharacterId = session.Character.CharacterId },
                        NoPacket = new FinsPacket
                        { Type = FinsPacketType.Rejected, CharacterId = session.Character.CharacterId }
                    });
                    session.Character.FriendRequestCharacters[session.Character.CharacterId] = finsPacket.CharacterId;
                    return;
                }

                switch (finsPacket.Type)
                {
                    case FinsPacketType.Accepted:
                        session.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                session.Account.Language)
                        });

                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                session.Account.Language)
                        });

                        var relation = session.Character.AddRelation(targetSession.VisualId,
                            CharacterRelationType.Friend);
                        var targetRelation = targetSession.AddRelation(session.Character.CharacterId,
                            CharacterRelationType.Friend);

                        session.Character.RelationWithCharacter.TryAdd(targetRelation.CharacterRelationId,
                            targetRelation);
                        targetSession.RelationWithCharacter.TryAdd(relation.CharacterRelationId, relation);

                        session.Character.FriendRequestCharacters.TryRemove(session.Character.CharacterId, out _);
                        break;
                    case FinsPacketType.Rejected:
                        targetSession.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                session.Account.Language)
                        });

                        session.Character.FriendRequestCharacters.TryRemove(session.Character.CharacterId, out _);
                        break;
                    default:
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.INVITETYPE_UNKNOWN));
                        break;
                }
            }
        }
    }
}
