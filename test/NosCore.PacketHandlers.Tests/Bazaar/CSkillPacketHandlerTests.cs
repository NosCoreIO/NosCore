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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Packets.ClientPackets.Bazaar;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Bazaar
{
    [TestClass]
    public class CSkillPacketHandlerTests
    {
        private CSkillPacketHandler CskillPacketHandler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Session.Character.StaticBonusList = new List<StaticBonusDto>();
            CskillPacketHandler = new CSkillPacketHandler(TestHelpers.Instance.Clock);
        }

        [TestMethod]
        public async Task OpeningBazaarWhileInShopShouldBeIgnored()
        {
            await new Spec("Opening bazaar while in shop should be ignored")
                .Given(CharacterIsInShop)
                .WhenAsync(OpeningBazaarViaMiddleware)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningBazaarWithoutMedalShouldShowInfo()
        {
            await new Spec("Opening bazaar without medal should show info")
                .WhenAsync(OpeningBazaar)
                .Then(ShouldReceiveMedalRequiredMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OpeningBazaarWithMedalShouldSucceed()
        {
            await new Spec("Opening bazaar with medal should succeed")
                .Given(CharacterHasBazaarMedal)
                .WhenAsync(OpeningBazaar)
                .Then(NoErrorShouldOccur)
                .ExecuteAsync();
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
        }

        private async Task OpeningBazaarViaMiddleware()
        {
            await Session.HandlePacketsAsync(new[] { new CSkillPacket() });
        }

        private void NoPacketShouldBeSent()
        {
            Assert.IsNull(Session.LastPackets.FirstOrDefault());
        }

        private async Task OpeningBazaar()
        {
            await CskillPacketHandler.ExecuteAsync(new CSkillPacket(), Session);
        }

        private void ShouldReceiveMedalRequiredMessage()
        {
            var packet = (InfoiPacket?)Session.LastPackets.FirstOrDefault(s => s is InfoiPacket);
            Assert.IsTrue(packet?.Message == Game18NConstString.NosMerchantMedaleAllowPlayerToUseNosbazarOnAllGeneralMaps);
        }

        private void CharacterHasBazaarMedal()
        {
            Session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                StaticBonusType = StaticBonusType.BazaarMedalGold
            });
        }

        private void NoErrorShouldOccur()
        {
            // Test passes if no exception is thrown
        }
    }
}
