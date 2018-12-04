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
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.Enumerations.Items;
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
        public void ExchangeList(ExcListPacket packet)
        {
            if (packet.Gold < 0 || packet.Gold > Session.Character.Gold || packet.BankGold > Session.Account.BankMoney)
            {
                return;
            }

            var list = new ExcListPacket
            {
                Unknown = 1,
                VisualId = Session.Character.VisualId,
                Gold = packet.Gold,
                BankGold = packet.BankGold,
                SubPackets = new List<ExcListSubPacket>()
            };

            foreach (var value in packet.SubPackets)
            {
                if (value.PocketType == PocketType.Bazaar)
                {
                    return;
                }

                var item = Session.Character.Inventory.LoadBySlotAndType<IItemInstance>(value.Slot, value.PocketType);

                if (item == null || item.Amount <= 0 || item.Amount < value.Amount)
                {
                    return;
                }

                if (!item.Item.IsTradable)
                {
                    var target = Broadcaster.Instance.GetCharacter(s =>
                        s.VisualId == Session.Character.ExchangeInfo.ExchangeData.TargetVisualId &&
                        s.MapInstanceId == Session.Character.MapInstanceId);

                    if (target is Character chara)
                    {
                        chara.SendPacket(new ExcClosePacket { Type = 0 });
                        chara.ExchangeInfo.ExchangeData = new ExchangeData();
                    }

                    Session.SendPacket(new ExcClosePacket { Type = 0 });
                    Session.Character.ExchangeInfo.ExchangeData = new ExchangeData();
                    return;
                }

                var itemCpy = (IItemInstance)item.Clone();
                itemCpy.Amount = value.Amount;
                Session.Character.ExchangeInfo.ExchangeData.ExchangeItems.TryAdd(Session.Character.ExchangeInfo.ExchangeData.TargetVisualId, itemCpy);
                var subPacket = new ExcListSubPacket
                {
                    //TODO: finish this
                };

                Session.Character.ExchangeInfo.ExchangeData.Gold = packet.Gold;
                Session.Character.ExchangeInfo.ExchangeData.BankGold = packet.BankGold;
            }
        }

        [UsedImplicitly]
        public void RequestExchange(ExchangeRequestPacket packet)
        {
            var target = Broadcaster.Instance.GetCharacter(s => s.VisualId == packet.VisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

            switch (packet.RequestType)
            {
                case RequestExchangeType.Requested:
                    if (target == null)
                    {
                        return;
                    }

                    if (target.InExchangeOrShop || Session.Character.InExchangeOrShop)
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

                    if (Session.Character.InShop || target.InShop)
                    {
                        Session.SendPacket(new MsgPacket {Message = Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED, Session.Account.Language), Type = MessageType.White });
                        return;
                    }

                    Session.SendPacket(new ModalPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE, Session.Account.Language),
                        Type = 0
                    });

                    Session.Character.ExchangeInfo.ExchangeRequests.TryAdd(Guid.NewGuid(), target.VisualId);
                    target.SendPacket(new DlgPacket
                    {
                        YesPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.List, VisualId = Session.Character.VisualId },
                        NoPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.Declined, VisualId = Session.Character.VisualId },
                        Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, Session.Account.Language)
                    });

                    break;

                case RequestExchangeType.List:
                    if (target == null || target.InExchangeOrShop)
                    {
                        return;
                    }
                    
                    Session.Character.ExchangeInfo.ExchangeData.TargetVisualId = target.VisualId;
                    target.ExchangeInfo.ExchangeData.TargetVisualId = Session.Character.VisualId;
                    Session.Character.InExchange = true;
                    target.InExchange = true;

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
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeInfo.ExchangeData.TargetVisualId && s.MapInstanceId == Session.Character.MapInstanceId) as Character;

                    if (target == null)
                    {
                        _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER, Session.Account.Language));
                        return;
                    }
                    

                    break;
                case RequestExchangeType.Cancelled:
                    //TODO: Clear current items in exchange
                    target = Broadcaster.Instance.GetCharacter(s => s.VisualId == Session.Character.ExchangeInfo.ExchangeData.TargetVisualId) as Character;


                    if (target != null)
                    {
                        target.InExchange = false;
                        target.ExchangeInfo.ExchangeData = new ExchangeData();
                        target.SendPacket(new ExcClosePacket { Type = 0 });
                    }

                    Session.Character.InExchange = false;
                    Session.Character.ExchangeInfo.ExchangeData = new ExchangeData();
                    Session.SendPacket(new ExcClosePacket { Type = 0 });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
