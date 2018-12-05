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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.ExchangeInfo
{
    public class ExchangeInfoService : IExchangeInfoService
    {
        public ExchangeInfoService()
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
    }
}
