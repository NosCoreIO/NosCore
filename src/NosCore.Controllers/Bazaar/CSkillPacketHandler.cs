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
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSkillPacketHandler : PacketHandler<CSkillPacket>, IWorldPacketHandler
    {
        public override void Execute(CSkillPacket packet, ClientSession clientSession)
        {
            if (clientSession.Character.InExchangeOrTrade)
            {
                return;
            }

            var medalBonus = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));
            if (medalBonus != null)
            {
                var medal = medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte) MedalType.Gold
                    : (byte) MedalType.Silver;
                var time = (int)(medalBonus.DateEnd == null ? 720 : ((TimeSpan)(medalBonus.DateEnd - SystemTime.Now())).TotalHours);
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.INFO_BAZAAR,
                        clientSession.Account.Language),
                    Type = MessageType.Whisper
                });
                clientSession.SendPacket(new WopenPacket
                {
                    Type = WindowType.NosBazaar,
                    Unknown = medal,
                    Unknown2 = (byte) time
                });
            }
            else
            {
                clientSession.SendPacket(new InfoPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_BAZAAR_MEDAL,
                        clientSession.Account.Language)
                });
            }
        }
    }
}