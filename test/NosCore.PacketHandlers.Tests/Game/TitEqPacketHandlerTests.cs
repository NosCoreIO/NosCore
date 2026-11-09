//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class TitEqPacketHandlerTests
    {
        private TitEqPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private const short TitleId = 1;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new TitEqPacketHandler();
        }

        [TestMethod]
        public async Task TitEqWithInvalidTitleShouldDoNothing()
        {
            await new Spec("TitEq with invalid title should do nothing")
                .Given(CharacterIsOnMap)
                .WhenAsync(ActivatingNonExistentTitle)
                .Then(NoInfoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TitEqMode1ShouldToggleVisibility()
        {
            await new Spec("TitEq mode 1 should toggle visibility")
                .Given(CharacterIsOnMap)
                .And(CharacterHasTitle)
                .WhenAsync(TogglingTitleVisibility)
                .Then(TitleVisibilityShouldBeToggled)
                .And(TitleChangedInfoShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TitEqMode0ShouldToggleActive()
        {
            await new Spec("TitEq mode 0 should toggle active")
                .Given(CharacterIsOnMap)
                .And(CharacterHasTitle)
                .WhenAsync(TogglingTitleActive)
                .Then(TitleActiveShouldBeToggled)
                .And(TitleEffectChangedInfoShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
        }

        private void CharacterHasTitle()
        {
            Session.Character.Titles = new List<TitleDto>
            {
                new TitleDto { TitleType = TitleId, Visible = false, Active = false }
            };
        }

        private async Task ActivatingNonExistentTitle()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new TitEqPacket
            {
                TitleId = 9999,
                Mode = 0
            }, Session);
        }

        private async Task TogglingTitleVisibility()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new TitEqPacket
            {
                TitleId = TitleId,
                Mode = 1
            }, Session);
        }

        private async Task TogglingTitleActive()
        {
            Session.LastPackets.Clear();
            await Handler.ExecuteAsync(new TitEqPacket
            {
                TitleId = TitleId,
                Mode = 0
            }, Session);
        }

        private void NoInfoPacketShouldBeSent()
        {
            Assert.IsFalse(Session.LastPackets.Any(p => p is InfoiPacket));
        }

        private void TitleVisibilityShouldBeToggled()
        {
            var title = Session.Character.Titles.First(t => t.TitleType == TitleId);
            Assert.IsTrue(title.Visible);
        }

        private void TitleActiveShouldBeToggled()
        {
            var title = Session.Character.Titles.First(t => t.TitleType == TitleId);
            Assert.IsTrue(title.Active);
        }

        private void TitleChangedInfoShouldBeSent()
        {
            Assert.AreEqual(Game18NConstString.TitleChangedOrHidden, Session.LastPackets.OfType<InfoiPacket>().Single().Message);
        }

        private void TitleEffectChangedInfoShouldBeSent()
        {
            Assert.AreEqual(Game18NConstString.TitleEffectChangedOrDeactivated, Session.LastPackets.OfType<InfoiPacket>().Single().Message);
        }
    }
}
