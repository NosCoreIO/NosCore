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
    public class StashEndPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private StashEndPacketHandler Handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.GenerateSessionAsync();
            Handler = World.Create<StashEndPacketHandler>();
        }

        [TestMethod]
        public async Task StashEndPacketShouldExecuteWithoutError()
        {
            await new Spec("Stash end packet should execute without error")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouse)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StashEndPacketShouldNotThrowWhenWarehouseNotOpen()
        {
            await new Spec("Stash end packet should not throw when warehouse not open")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouseWithoutOpening)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task StashEndPacketShouldNotThrowMultipleTimes()
        {
            await new Spec("Stash end packet should not throw multiple times")
                .Given(CharacterIsOnMap)
                .WhenAsync(ClosingWarehouseMultipleTimes)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private async Task ClosingWarehouse()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task ClosingWarehouseWithoutOpening()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private async Task ClosingWarehouseMultipleTimes()
        {
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
            await Handler.ExecuteAsync(new StashEndPacket(), Session);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }
    }
}
