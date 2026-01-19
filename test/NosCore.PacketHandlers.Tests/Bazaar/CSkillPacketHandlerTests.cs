//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
