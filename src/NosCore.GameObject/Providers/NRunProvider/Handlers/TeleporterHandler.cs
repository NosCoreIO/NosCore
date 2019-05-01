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
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item) => item.Item2.Runner == (NrunRunnerType)16;

        public void Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            var tp = requestData.ClientSession.Character.MapInstance.Npcs.Find(
                s => s.VisualId == requestData.Data.Item2.VisualId
                && s.VisualType == requestData.Data.Item2.VisualType
                && s.Dialog >= 439 && s.Dialog <= 441);

            if (tp != null)
            {
                bool CheckOut(long GoldToPay)
                {
                    if (requestData.ClientSession.Character.Gold >= GoldToPay)
                    {
                        requestData.ClientSession.Character.RemoveGold(GoldToPay);
                        return true;
                    }
                    requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                        Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, requestData.ClientSession.Account.Language), SayColorType.Yellow
                            ));
                    return false;
                }

                if (requestData.Data.Item2.Type == 0)
                {
                    requestData.ClientSession.ChangeMap(
                        1, (short)RandomFactory.Instance.RandomNumber(78, 81), (short)RandomFactory.Instance.RandomNumber(114, 118));
                } else if (requestData.Data.Item2.Type == 1)
                {
                    if (CheckOut(1_000))
                    {
                        requestData.ClientSession.ChangeMap(
                            20, (short)RandomFactory.Instance.RandomNumber(7, 11), (short)RandomFactory.Instance.RandomNumber(90, 94));
                    }
                } else
                {
                    if (CheckOut(2_000))
                    {
                        requestData.ClientSession.ChangeMap(
                            145, (short)RandomFactory.Instance.RandomNumber(11, 15), (short)RandomFactory.Instance.RandomNumber(108, 112));
                    }
                }
            }
        }
    }
}