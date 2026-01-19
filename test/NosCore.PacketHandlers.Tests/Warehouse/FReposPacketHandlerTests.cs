//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Warehouse;
using NosCore.Packets.ClientPackets.Warehouse;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Warehouse
{
    [TestClass]
    public class FReposPacketHandlerTests
    {
        private ClientSession Session = null!;
        private FReposPacketHandler Handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new FReposPacketHandler();
        }

        [TestMethod]
        public async Task FamilyReposPacketShouldExecuteWithoutError()
        {
            await new Spec("Family repos packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(RearrangingFamilyWarehouseItems)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyReposFromSlot0ToSlot1ShouldNotThrow()
        {
            await new Spec("Family repos from slot 0 to slot 1 should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(MovingItemFromSlot0ToSlot1)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyReposFromInvalidSlotShouldNotThrow()
        {
            await new Spec("Family repos from invalid slot should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(MovingItemFromInvalidSlot)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyReposToInvalidSlotShouldNotThrow()
        {
            await new Spec("Family repos to invalid slot should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(MovingItemToInvalidSlot)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyReposSameSlotShouldNotThrow()
        {
            await new Spec("Family repos same slot should not throw")
                .Given(CharacterIsOnMap)
                .WhenAsync(MovingItemToSameSlot)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task RearrangingFamilyWarehouseItems()
        {
            await Handler.ExecuteAsync(new FReposPacket(), Session);
        }

        private async Task MovingItemFromSlot0ToSlot1()
        {
            await Handler.ExecuteAsync(new FReposPacket { OldSlot = 0, NewSlot = 1 }, Session);
        }

        private async Task MovingItemFromInvalidSlot()
        {
            await Handler.ExecuteAsync(new FReposPacket { OldSlot = 255, NewSlot = 0 }, Session);
        }

        private async Task MovingItemToInvalidSlot()
        {
            await Handler.ExecuteAsync(new FReposPacket { OldSlot = 0, NewSlot = 255 }, Session);
        }

        private async Task MovingItemToSameSlot()
        {
            await Handler.ExecuteAsync(new FReposPacket { OldSlot = 5, NewSlot = 5 }, Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
