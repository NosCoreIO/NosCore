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
    public class FStashEndPacketHandlerTests
    {
        private ClientSession Session = null!;
        private FStashEndPackettHandler Handler = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new FStashEndPackettHandler();
        }

        [TestMethod]
        public async Task FamilyStashEndPacketShouldExecuteWithoutError()
        {
            await new Spec("Family stash end packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingFamilyWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyStashEndPacketShouldNotThrowWhenWarehouseNotOpen()
        {
            await new Spec("Family stash end packet should not throw when warehouse not open")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingFamilyWarehouseWithoutOpening)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task FamilyStashEndPacketShouldNotThrowMultipleTimes()
        {
            await new Spec("Family stash end packet should not throw multiple times")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingFamilyWarehouseMultipleTimes)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private async Task ClosingFamilyWarehouse()
        {
            await Handler.ExecuteAsync(new FStashEndPacket(), Session);
        }

        private async Task ClosingFamilyWarehouseWithoutOpening()
        {
            await Handler.ExecuteAsync(new FStashEndPacket(), Session);
        }

        private async Task ClosingFamilyWarehouseMultipleTimes()
        {
            await Handler.ExecuteAsync(new FStashEndPacket(), Session);
            await Handler.ExecuteAsync(new FStashEndPacket(), Session);
            await Handler.ExecuteAsync(new FStashEndPacket(), Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
