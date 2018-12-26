//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemBuilder;
using NosCore.GameObject.Services.ItemBuilder.Item;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;
using Serilog;

namespace NosCore.GameObject.Services.ExchangeInfo
{
    public class ExchangeAccessService
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IItemBuilderService _itemBuilderService;

        public ExchangeAccessService()
        {
            ExchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            ExchangeRequests = new ConcurrentDictionary<Guid, long>();
        }

        [UsedImplicitly]
        public ExchangeAccessService(IItemBuilderService itemBuilderService)
        {
            ExchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            ExchangeRequests = new ConcurrentDictionary<Guid, long>();
            _itemBuilderService = itemBuilderService;
        }

        public ConcurrentDictionary<long, ExchangeData> ExchangeDatas { get; set; }

        public ConcurrentDictionary<Guid, long> ExchangeRequests { get; set; }

        public void CloseExchange(ClientSession session, ClientSession targetSession, ExchangeCloseType closeType)
        {
            if (targetSession != null)
            {
                ResetExchangeData(targetSession);
                targetSession.SendPacket(new ExcClosePacket { Type = closeType });
            }

            if (session == null)
            {
                return;
            }
            
            ResetExchangeData(session);
            session.SendPacket(new ExcClosePacket { Type = closeType });
        }

        public void ResetExchangeData(ClientSession session)
        {
            if (session == null)
            {
                return;
            }

            session.Character.InExchange = false;
            ExchangeDatas[session.Character.CharacterId] = new ExchangeData();
            session.Character.ExchangeRequests = new ConcurrentDictionary<Guid, long>();
        }

        public void ProcessExchange(ClientSession session, ClientSession targetSession)
        {
            var sessionData = ExchangeDatas[session.Character.CharacterId];
            foreach (var item in sessionData.ExchangeItems.Values)
            {
                if (session.Character.Inventory.LoadByItemInstanceId<IItemInstance>(item.Id)?.Amount >= item.Amount)
                {
                    session.Character.Inventory.RemoveItemAmountFromInventory(item.Amount, item.Id);
                    var temp2 = session.Character.Inventory.LoadBySlotAndType<IItemInstance>(item.Slot, item.Type);

                    session.SendPacket(temp2.GeneratePocketChange(item.Type, item.Slot));
                }
                else
                {
                    _logger.Error(Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_ITEMS, session.Account.Language));
                    return;
                }
            }
            
            foreach (var item in sessionData.ExchangeItems.Values)
            {
                var itemCpy = _itemBuilderService.Create(item.ItemVNum, targetSession.Character.CharacterId,
                    amount: item.Amount, rare: (sbyte) item.Rare, upgrade: item.Upgrade, design: (byte) item.Design);

                var inv = targetSession.Character.Inventory.AddItemToPocket(itemCpy).FirstOrDefault();

                if (inv == null)
                {
                    continue;
                }
                
                targetSession.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));
            }

            session.Character.Gold -= sessionData.Gold;
            session.Account.BankMoney -= sessionData.BankGold * 1000;
            session.SendPacket(session.Character.GenerateGold());

            targetSession.Character.Gold += sessionData.Gold;
            targetSession.Account.BankMoney += sessionData.BankGold * 1000;
            targetSession.SendPacket(targetSession.Character.GenerateGold());
        }

        public void OpenExchange(ClientSession session, ClientSession targetSession)
        {
            var sessionData = ExchangeDatas[session.Character.CharacterId];
            var targetData = ExchangeDatas[targetSession.Character.CharacterId];
            sessionData.TargetVisualId = targetSession.Character.VisualId;
            targetData.TargetVisualId = session.Character.VisualId;
            session.Character.InExchange = true;
            targetSession.Character.InExchange = true;

            session.SendPacket(new ServerExcListPacket
            {
                SenderType = SenderType.Server,
                VisualId = targetSession.Character.VisualId,
                Gold = -1
            });

            targetSession.SendPacket(new ServerExcListPacket
            {
                SenderType = SenderType.Server,
                VisualId = session.Character.CharacterId,
                Gold = -1
            });
        }

        public void RequestExchange(ClientSession session, ClientSession targetSession)
        {
            if (targetSession.Character.InExchangeOrShop || session.Character.InExchangeOrShop)
            {
                session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE,
                        session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            if (targetSession.Character.GroupRequestBlocked)
            {
                session.SendPacket(session.Character.GenerateSay(
                    Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED, session.Account.Language),
                    SayColorType.Purple));
                return;
            }

            if (session.Character.IsRelatedToCharacter(targetSession.Character.VisualId, CharacterRelationType.Blocked))
            {
                session.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED,
                        session.Account.Language)
                });
                return;
            }

            if (session.Character.InShop || targetSession.Character.InShop)
            {
                session.SendPacket(new MsgPacket
                {
                    Message =
                        Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED, session.Account.Language),
                    Type = MessageType.White
                });
                return;
            }

            session.SendPacket(new ModalPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE,
                    session.Account.Language),
                Type = 0
            });

            session.Character.ExchangeRequests.TryAdd(Guid.NewGuid(), targetSession.Character.VisualId);
            targetSession.Character.SendPacket(new DlgPacket
            {
                YesPacket = new ExchangeRequestPacket
                    {RequestType = RequestExchangeType.List, VisualId = session.Character.VisualId},
                NoPacket = new ExchangeRequestPacket
                    {RequestType = RequestExchangeType.Declined, VisualId = session.Character.VisualId},
                Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, session.Account.Language)
            });
        }
    }
}