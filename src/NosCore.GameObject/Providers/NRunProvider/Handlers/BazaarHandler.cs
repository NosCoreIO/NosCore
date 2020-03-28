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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Providers.NRunProvider.Handlers
{
    public class BazaarHandler : IEventHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>
    {
        public bool Condition(Tuple<IAliveEntity, NrunPacket> item)
        {
            return (item.Item2.Runner == NrunRunnerType.OpenNosBazaar)
                && item.Item1 is MapNpc;
        }

        public Task Execute(RequestData<Tuple<IAliveEntity, NrunPacket>> requestData)
        {
            if (requestData.ClientSession.Character!.InExchangeOrTrade)
            {
                return Task.CompletedTask;
            }

            var medalBonus = requestData.ClientSession.Character.StaticBonusList
                .FirstOrDefault(s =>
                    (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                    (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));
            var medal = medalBonus != null ? medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold
                ? (byte) MedalType.Gold : (byte) MedalType.Silver : (byte) 0;
            var time = medalBonus != null ? (int)(medalBonus.DateEnd == null ? 720 : ((TimeSpan)(medalBonus.DateEnd - SystemTime.Now())).TotalHours) : 0;
            requestData.ClientSession.SendPacket(new WopenPacket
            {
                Type = WindowType.NosBazaar,
                Unknown = medal,
                Unknown2 = (byte) time
            });
            return Task.CompletedTask;
        }
    }
}