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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Shops;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Shops
{
    [TestClass]
    public class BuyPacketHandlerTests
    {
        private BuyPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new BuyPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task BuyingFromUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Buying from unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromUnknownVisualType)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromNonExistentNpcShouldBeIgnored()
        {
            await new Spec("Buying from non existent npc should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromNonExistentNpc)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task BuyingFromNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Buying from non existent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(BuyingFromNonExistentPlayer)
                .Then(NoErrorPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task BuyingFromUnknownVisualType()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = (VisualType)99,
                VisualId = 1,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private async Task BuyingFromNonExistentNpc()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private async Task BuyingFromNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new BuyPacket
            {
                VisualType = VisualType.Player,
                VisualId = 99999,
                Slot = 0,
                Amount = 1
            }, Session);
        }

        private void NoErrorPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
