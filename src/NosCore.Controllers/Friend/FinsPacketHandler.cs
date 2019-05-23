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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Friend
{
    public class FinsPacketHandler : PacketHandler<FinsPacket>, IWorldPacketHandler
    {
        private readonly IWebApiAccess _webApiAccess;
        public FinsPacketHandler(IWebApiAccess webApiAccess)
        {
            _webApiAccess = webApiAccess;
        }

        public override void Execute(FinsPacket finsPacket, ClientSession session)
        {
            var server = _webApiAccess.Get<List<ChannelInfo>>(WebApiRoute.Channel)
                ?.FirstOrDefault(c => c.Type == ServerType.FriendServer);
            var character = Broadcaster.Instance.GetCharacter(s => s.VisualId == session.Character.CharacterId);
            var targetCharacter = Broadcaster.Instance.GetCharacter(s => s.VisualId == finsPacket.CharacterId);
            if (server != null && character != null && targetCharacter != null)
            {
                var result = _webApiAccess.Post<LanguageKey>(WebApiRoute.Friend, new FriendShipRequest { CharacterId = session.Character.CharacterId, FinsPacket = finsPacket }, server.WebApi);
            
                switch (result)
                {
                    case LanguageKey.FRIENDLIST_FULL:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIENDLIST_FULL,
                                character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.BLACKLIST_BLOCKED:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                                character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.ALREADY_FRIEND:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_FRIEND,
                                    character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_BLOCKED:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_BLOCKED,
                                    character.AccountLanguage)
                        });
                        break;

                    case LanguageKey.FRIEND_REQUEST_SENT:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REQUEST_SENT,
                                    character.AccountLanguage)
                        });
                        targetCharacter.SendPacket(new DlgPacket
                        {
                            Question = string.Format(
                                Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADD, character.AccountLanguage),
                                character.Name),
                            YesPacket = new FinsPacket
                            { Type = FinsPacketType.Accepted, CharacterId = character.VisualId },
                            NoPacket = new FinsPacket
                            { Type = FinsPacketType.Rejected, CharacterId = character.VisualId }
                        });
                        break;

                    case LanguageKey.FRIEND_ADDED:
                        character.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                    character.AccountLanguage)
                        });
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_ADDED,
                                character.AccountLanguage)
                        });

                        targetCharacter.SendPacket(targetCharacter.GenerateFinit(_webApiAccess));
                        character.SendPacket(character.GenerateFinit(_webApiAccess));
                        break;

                    case LanguageKey.FRIEND_REJECTED:
                        targetCharacter.SendPacket(new InfoPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.FRIEND_REJECTED,
                                character.AccountLanguage)
                        });
                        break;
                }
            }
        }
    }
}
