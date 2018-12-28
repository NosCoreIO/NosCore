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
using System.Linq;
using JetBrains.Annotations;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking;
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

namespace NosCore.GameObject.Services.ExchangeAccess
{
    public class ExchangeAccessService
    {
        private readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();
        private readonly IItemBuilderService _itemBuilderService;

        public ExchangeAccessService()
        {
            ExchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            ExchangeRequests = new ConcurrentDictionary<long, long>();
        }

        [UsedImplicitly]
        public ExchangeAccessService(IItemBuilderService itemBuilderService)
        {
            ExchangeDatas = new ConcurrentDictionary<long, ExchangeData>();
            ExchangeRequests = new ConcurrentDictionary<long, long>();
            _itemBuilderService = itemBuilderService;
        }

        public ConcurrentDictionary<long, ExchangeData> ExchangeDatas { get; set; }

        public ConcurrentDictionary<long, long> ExchangeRequests { get; set; }

        //TODO: replace ClientSessions with VisualId
        public ExcClosePacket CloseExchange(long visualId, ExchangeCloseType closeType)
        {
            ResetExchangeData(visualId);
            return new ExcClosePacket
            {
                Type = closeType
            };
        }

        public void ResetExchangeData(long visualId)
        {
            var target = (Character)Broadcaster.Instance.GetCharacter(s => s.VisualId == visualId);

            if (target == null)
            {
                return;
            }

            target.InExchange = false;
            ExchangeDatas.TryRemove(visualId, out _);
            ExchangeRequests.TryRemove(visualId, out _);
        }

        public void OpenExchange(ClientSession session, ClientSession targetSession)
        {
            ExchangeDatas[session.Character.CharacterId] = new ExchangeData();
            ExchangeDatas[targetSession.Character.CharacterId] = new ExchangeData();

            ExchangeDatas[session.Character.CharacterId].TargetVisualId = targetSession.Character.VisualId;
            ExchangeDatas[targetSession.Character.CharacterId].TargetVisualId = session.Character.VisualId;
            session.Character.InExchange = true;
            targetSession.Character.InExchange = true;

            session.SendPacket(new ServerExcListPacket
            {
                VisualType = VisualType.Player,
                VisualId = targetSession.Character.VisualId,
                Gold = null
            });

            targetSession.SendPacket(new ServerExcListPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Gold = null
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

            if (targetSession.Character.ExchangeBlocked)
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

            ExchangeRequests[session.Character.CharacterId] = targetSession.Character.CharacterId;
            targetSession.Character.SendPacket(new DlgPacket
            {
                YesPacket = new ExchangeRequestPacket
                    {RequestType = RequestExchangeType.List, VisualId = session.Character.VisualId},
                NoPacket = new ExchangeRequestPacket
                    {RequestType = RequestExchangeType.Declined, VisualId = session.Character.VisualId},
                Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, session.Account.Language)
            });
        }

        private void ExchangeItems(ClientSession session, ClientSession targetSession)
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
                var inv = targetSession.Character.Inventory.AddItemToPocket(_itemBuilderService.Create(item.ItemVNum,
                    targetSession.Character.CharacterId, amount: item.Amount, rare: (sbyte)item.Rare, upgrade: item.Upgrade, design: (byte)item.Design)).FirstOrDefault();

                if (inv == null)
                {
                    throw new ArgumentNullException(nameof(inv));
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

        public void ProcessExchange(ClientSession session, ClientSession targetSession)
        {
            ExchangeItems(session, targetSession);
            ExchangeItems(targetSession, session);
        }
    }
}