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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Networking;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;


namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CSkillPacketHandlerTest
    {
        private CSkillPacketHandler? _cskillPacketHandler;
        private ClientSession? _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            var player = _session.Player;
            player.StaticBonusList = new List<StaticBonusDto>();
            _cskillPacketHandler = new CSkillPacketHandler(TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task OpenWhenInShopAsync()
        {
            var player = _session!.Player;
            player.InShop = true;
            await _session!.HandlePacketsAsync(new[]
            {
                new CSkillPacket()
            }).ConfigureAwait(false);
            Assert.IsNull(_session.LastPackets.FirstOrDefault());
        }


        [TestMethod]
        public async Task OpenWhenNoMedalAsync()
        {
            await _cskillPacketHandler!.ExecuteAsync(new CSkillPacket(), _session!).ConfigureAwait(false);
            var lastpacket = (InfoiPacket?)_session!.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(lastpacket?.Message == Game18NConstString.NosMerchantMedaleAllowPlayerToUseNosbazarOnAllGeneralMaps);
        }

        [TestMethod]
        public async Task OpenAsync()
        {
            _session!.Player.StaticBonusList.Add(new StaticBonusDto
            {
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
            await _cskillPacketHandler!.ExecuteAsync(new CSkillPacket(), _session).ConfigureAwait(false);
        }
    }
}