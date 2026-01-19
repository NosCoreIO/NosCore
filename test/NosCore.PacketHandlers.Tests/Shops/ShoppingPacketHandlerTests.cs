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
using NosCore.Algorithm.DignityService;
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
    public class ShoppingPacketHandlerTests
    {
        private ShoppingPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private Mock<IDignityService> DignityService = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            DignityService = new Mock<IDignityService>();
            DignityService.Setup(x => x.GetLevelFromDignity(It.IsAny<short>())).Returns(DignityType.Default);

            Handler = new ShoppingPacketHandler(
                Logger,
                DignityService.Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task ShoppingFromUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Shopping from unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingFromNonExistentNpcShouldBeIgnored()
        {
            await new Spec("Shopping from non existent npc should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromNonExistentNpc)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ShoppingFromNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Shopping from non existent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(ShoppingFromNonExistentPlayer)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task ShoppingFromUnknownVisualType()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = (VisualType)99,
                VisualId = 1,
                ShopType = 0
            }, Session);
        }

        private async Task ShoppingFromNonExistentNpc()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = VisualType.Npc,
                VisualId = 99999,
                ShopType = 0
            }, Session);
        }

        private async Task ShoppingFromNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new ShoppingPacket
            {
                VisualType = VisualType.Player,
                VisualId = 99999,
                ShopType = 0
            }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
