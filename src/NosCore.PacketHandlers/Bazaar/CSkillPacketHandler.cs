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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace NosCore.PacketHandlers.Bazaar
{
    public class CSkillPacketHandler : PacketHandler<CSkillPacket>, IWorldPacketHandler
    {
        private readonly IClock _clock;

        public CSkillPacketHandler(IClock clock)
        {
            _clock = clock;
        }
        public override async Task ExecuteAsync(CSkillPacket packet, ClientSession clientSession)
        {
            var medalBonus = clientSession.Character.StaticBonusList.FirstOrDefault(s =>
                (s.StaticBonusType == StaticBonusType.BazaarMedalGold) ||
                (s.StaticBonusType == StaticBonusType.BazaarMedalSilver));
            if (medalBonus != null)
            {
                var medal = medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver;
                var time = (int)(medalBonus.DateEnd == null ? 720 : (((Instant)medalBonus.DateEnd) - _clock.GetCurrentInstant()).TotalHours);
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.AttackWhileBazar
                }).ConfigureAwait(false);
                await clientSession.SendPacketAsync(new WopenPacket
                {
                    Type = WindowType.NosBazaar,
                    Unknown = medal,
                    Unknown2 = (byte)time
                }).ConfigureAwait(false);
            }
            else
            {
                await clientSession.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.NosMerchantMedaleAllowPlayerToUseNosbazarOnAllGeneralMaps
                }).ConfigureAwait(false);
            }
        }
    }
}