//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
using System.Threading.Tasks;

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
                TestHelpers.Instance.SessionRegistry,
                TestHelpers.Instance.WorldConfiguration);
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
