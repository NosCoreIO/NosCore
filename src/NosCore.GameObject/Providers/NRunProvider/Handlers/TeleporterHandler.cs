//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
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
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ClientPackets.Npcs;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class TeleporterHandler : IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item) => item.Item2.Runner == (NrunRunnerType)16
            && item.Item1.MapInstance.Npcs.Find(
                s => s.VisualId == item.Item2.VisualId
                && s.VisualType == item.Item2.VisualType
                && s.Dialog >= 439 && s.Dialog <= 441)
            != null;

        private void CheckOut(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData, short mapId, long GoldToPay, short x1, short x2, short y1, short y2)
        {
            if (requestData.ClientSession.Character.Gold >= GoldToPay)
            {
                requestData.ClientSession.Character.RemoveGold(GoldToPay);
                requestData.ClientSession.ChangeMap(
                        mapId, (short)RandomFactory.Instance.RandomNumber(x1, x2), (short)RandomFactory.Instance.RandomNumber(y1, y2));
                return;
            }
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, requestData.ClientSession.Account.Language), SayColorType.Yellow
                    ));
        }

        public void Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            switch (requestData.Data.Item2.Type)
            {
                case 1:
                    CheckOut(requestData, 20, 1000, 7, 11, 90, 94);
                    break;
                case 2:
                    CheckOut(requestData, 145, 2000, 11, 15, 108, 112);
                    break;
                default:
                    CheckOut(requestData, 1, 0, 77, 82, 113, 119);
                    break;
            }
        }
    }
}