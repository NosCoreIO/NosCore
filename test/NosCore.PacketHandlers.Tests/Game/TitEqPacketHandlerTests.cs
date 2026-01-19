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
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

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
            Broadcaster.Reset();
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

        [TestMethod]
        public async Task TitEqVisibilityShouldResetOtherTitles()
        {
            await new Spec("TitEq visibility should reset other titles")
                .Given(CharacterIsOnMap)
                .And(CharacterHasMultipleTitles)
                .WhenAsync(TogglingTitleVisibility)
                .Then(OnlySelectedTitleShouldBeVisible)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TitEqActiveShouldResetOtherTitles()
        {
            await new Spec("TitEq active should reset other titles")
                .Given(CharacterIsOnMap)
                .And(CharacterHasMultipleTitles)
                .WhenAsync(TogglingTitleActive)
                .Then(OnlySelectedTitleShouldBeActive)
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

        private void CharacterHasMultipleTitles()
        {
            Session.Character.Titles = new List<TitleDto>
            {
                new TitleDto { TitleType = TitleId, Visible = false, Active = false },
                new TitleDto { TitleType = 2, Visible = true, Active = true }
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
            Assert.IsTrue(Session.LastPackets.Any(p => p is InfoiPacket));
        }

        private void TitleEffectChangedInfoShouldBeSent()
        {
            Assert.IsTrue(Session.LastPackets.Any(p => p is InfoiPacket));
        }

        private void OnlySelectedTitleShouldBeVisible()
        {
            var selectedTitle = Session.Character.Titles.First(t => t.TitleType == TitleId);
            var otherTitles = Session.Character.Titles.Where(t => t.TitleType != TitleId);
            Assert.IsTrue(selectedTitle.Visible);
            Assert.IsTrue(otherTitles.All(t => !t.Visible));
        }

        private void OnlySelectedTitleShouldBeActive()
        {
            var selectedTitle = Session.Character.Titles.First(t => t.TitleType == TitleId);
            var otherTitles = Session.Character.Titles.Where(t => t.TitleType != TitleId);
            Assert.IsTrue(selectedTitle.Active);
            Assert.IsTrue(otherTitles.All(t => !t.Active));
        }
    }
}
