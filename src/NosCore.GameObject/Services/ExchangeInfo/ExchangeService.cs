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
using System.Text;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ExchangeInfo
{
    public class ExchangeService : IExchangeService
    {
        public ExchangeService()
        {
            ExchangeData = new ExchangeData();
            ExchangeRequests = new ConcurrentDictionary<Guid, long>();
        }

        public ExchangeData ExchangeData { get; set; }

        public ConcurrentDictionary<Guid, long> ExchangeRequests { get; set; }

        public void CloseExchange(ClientSession session, ClientSession targetSession)
        {
            if (targetSession?.Character.ExchangeInfo.ExchangeData != null)
            {
                targetSession.Character.InExchange = false;
                targetSession.SendPacket(new ExcClosePacket());
                targetSession.Character.ExchangeInfo.ExchangeData = new ExchangeData();
                targetSession.Character.ExchangeInfo.ExchangeRequests = new ConcurrentDictionary<Guid, long>();
            }

            if (session?.Character.ExchangeInfo.ExchangeData == null)
            {
                return;
            }

            session.Character.InExchange = false;
            session.SendPacket(new ExcClosePacket());
            session.Character.ExchangeInfo.ExchangeData = new ExchangeData();
            session.Character.ExchangeInfo.ExchangeRequests = new ConcurrentDictionary<Guid, long>();
        }

        public void ProcessExchange(ClientSession session, ClientSession targetSession)
        {

        }

        public void OpenExchange(ClientSession session, ClientSession targetSession)
        {
            session.Character.ExchangeInfo.ExchangeData.TargetVisualId = targetSession.Character.VisualId;
            targetSession.Character.ExchangeInfo.ExchangeData.TargetVisualId = session.Character.VisualId;
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
                session.SendPacket(new MsgPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.ALREADY_EXCHANGE, session.Account.Language), Type = MessageType.White });
                return;
            }

            if (targetSession.Character.GroupRequestBlocked)
            {
                session.SendPacket(session.Character.GenerateSay(Language.Instance.GetMessageFromKey(LanguageKey.EXCHANGE_BLOCKED, session.Account.Language), SayColorType.Purple));
                return;
            }

            if (session.Character.IsRelatedToCharacter(targetSession.Character.VisualId, CharacterRelationType.Blocked))
            {
                session.SendPacket(new InfoPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.BLACKLIST_BLOCKED, session.Account.Language) });
                return;
            }

            if (session.Character.InShop || targetSession.Character.InShop)
            {
                session.SendPacket(new MsgPacket { Message = Language.Instance.GetMessageFromKey(LanguageKey.HAS_SHOP_OPENED, session.Account.Language), Type = MessageType.White });
                return;
            }

            session.SendPacket(new ModalPacket
            {
                Message = Language.Instance.GetMessageFromKey(LanguageKey.YOU_ASK_FOR_EXCHANGE, session.Account.Language),
                Type = 0
            });

            session.Character.ExchangeInfo.ExchangeRequests.TryAdd(Guid.NewGuid(), targetSession.Character.VisualId);
            targetSession.Character.SendPacket(new DlgPacket
            {
                YesPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.List, VisualId = session.Character.VisualId },
                NoPacket = new ExchangeRequestPacket { RequestType = RequestExchangeType.Declined, VisualId = session.Character.VisualId },
                Question = Language.Instance.GetMessageFromKey(LanguageKey.INCOMING_EXCHANGE, session.Account.Language)
            });
        }
    }
}
