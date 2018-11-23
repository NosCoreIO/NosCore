using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.Controllers
{
    public class ExchangePacketController : PacketController
    {
        private readonly IItemBuilderService _itemBuilderService;
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        
        public ExchangePacketController(IItemBuilderService itemBuilderService)
        {
            _itemBuilderService = itemBuilderService;
        }

        [UsedImplicitly]
        public ExchangePacketController()
        {

        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == packet.VisualId) as Character;

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (target == null)
                    {
                        return;
                    }

                    if (target.InExchangeOrTrade || Session.Character.InExchangeOrTrade)
                    {
                        Session.SendPacket(new MsgPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE, Session.Account.Language), Type = MessageType.White });
                        return;
                    }

                    if (target.GroupRequestBlocked)
                    {
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED, Session.Account.Language), SayColorType.Purple));
                        return;
                    }

                    if (Session.Character.IsRelatedToCharacter(target.VisualId, CharacterRelationType.Blocked))
                    {
                        Session.SendPacket(new InfoPacket {Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED, Session.Account.Language)});
                        return;
                    }

                    if (Session.Character.HasShopOpened || target.HasShopOpened)
                    {
                        Session.SendPacket(new MsgPacket {Message = Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED, Session.Account.Language), Type = MessageType.White });
                        return;
                    }

                    Session.SendPacket(new ModalPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE, Session.Account.Language),
                        Type = 0
                    });

                    Session.Character.TradeRequests.TryAdd(Guid.NewGuid(), target.VisualId);
                    target.SendPacket(new DlgPacket
                    {
                        YesPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.List, VisualId = Session.Character.VisualId },
                        NoPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.Declined, VisualId = Session.Character.VisualId },
                        Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, Session.Account.Language)
                    });

                    break;

                case RequestExchangeType.List:
                    if (target == null || target.InExchangeOrTrade)
                    {
                        return;
                    }
                    
                    Session.Character.ExchangeData.TargetVisualId = target.VisualId;
                    target.ExchangeData.TargetVisualId = Session.Character.VisualId;
                    Session.Character.InExchangeOrTrade = true;
                    target.InExchangeOrTrade = true;

                    Session.SendPacket(new ExcListPacket
                    {
                        Unknown = 1,
                        VisualId = packet.VisualId.Value,
                        Gold = -1
                    });

                    target.SendPacket(new ExcListPacket
                    {
                        Unknown = 1,
                        VisualId = Session.Character.CharacterId,
                        Gold = -1
                    });
                    break;
                case RequestExchangeType.Declined:
                    Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED, Session.Account.Language), SayColorType.Yellow));
                    target?.SendPacket(target.GenerateSay(target.GetMessageFromKey(LanguageKey.EXCHANGE_REFUSED), SayColorType.Yellow));
                    break;
                case RequestExchangeType.Confirmed:
                    break;
                case RequestExchangeType.Cancelled:
                    //TODO: Clear current items in exchange
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeData.TargetVisualId) as Character;


                    if (target != null)
                    {
                        target.InExchangeOrTrade = false;
                        target.ExchangeData.TargetVisualId = -1;
                        target.SendPacket(new ExcClosePacket { Type = 0 });
                    }

                    Session.Character.InExchangeOrTrade = false;
                    Session.Character.ExchangeData.TargetVisualId = -1;
                    Session.SendPacket(new ExcClosePacket { Type = 0 });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
