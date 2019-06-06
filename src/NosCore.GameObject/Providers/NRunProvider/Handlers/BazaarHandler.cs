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
using System.Linq;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.ClientPackets.Npcs;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class BazaarHandler : IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item) => item.Item2.Runner == NrunRunnerType.OpenNosBazaar
            && item.Item1 is MapNpc;

        public void Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var medalBonus = requestData.ClientSession.Character.StaticBonusList
                .FirstOrDefault(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            if (medalBonus != null)
            {
                byte medal = medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver;
                int time = (int)(medalBonus.DateEnd - DateTime.Now).TotalHours;

                requestData.ClientSession.SendPacket(new WopenPacket
                {
                    Type = WindowType.NosBazaar,
                    Unknown = medal,
                    Unknown2 = (byte)time
                });
            }
        }
    }
}