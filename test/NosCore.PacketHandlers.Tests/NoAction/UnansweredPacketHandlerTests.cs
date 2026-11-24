//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Generated;
using NosCore.PacketHandlers.NoAction;
using NosCore.Packets;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ClientPackets.Shops;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.NoAction
{
    // The client sends these on its own and the server never answers them; without a handler each
    // one is logged and counted as a refused packet.
    [TestClass]
    public class UnansweredPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.LastPackets.Clear();
        }

        [TestMethod]
        public Task ClosingAShopWindowIsTakenSilently() =>
            AssertTakenSilentlyAsync(World.Create<ShopClosePacketHandler>(), new ShopClosePacket());

        [TestMethod]
        public Task ClosingTheProductionWindowIsTakenSilently() =>
            AssertTakenSilentlyAsync(World.Create<PdtClosePacketHandler>(), new PdtClosePacket());

        private async Task AssertTakenSilentlyAsync(IPacketHandler handler, IPacket sent)
        {
            var packetTypes = PacketTypes.World();
            var received = new Deserializer(packetTypes).Deserialize(new Serializer(packetTypes).Serialize(sent));

            Assert.AreEqual(handler.PacketType, received.GetType(), "handlers are looked up by the exact type the deserializer produces");
            Assert.IsTrue(GeneratedPacketHandlers.WorldHandlerTypes.Contains(handler.GetType()), "the world registers the handler");

            await handler.ExecuteAsync(received, Session);

            Assert.AreEqual(0, Session.LastPackets.Count, "the recordings show no answer");
        }
    }
}
