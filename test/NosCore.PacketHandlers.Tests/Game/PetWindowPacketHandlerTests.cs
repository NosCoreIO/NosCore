//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class PetWindowPacketHandlerTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.LastPackets.Clear();
        }

        [TestMethod]
        public async Task OpeningThePetWindowResendsTheRoster()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId);
            var partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            _session.Character.Mates[partner.MateTransportId] = partner;

            await new NpinfoPacketHandler().ExecuteAsync(new NpInfoPacket { Page = 0 }, _session);

            var sent = _session.LastPackets.ToList();
            Assert.IsInstanceOfType(sent[0], typeof(PclearPacket), "the list follows a p_clear");
            Assert.AreEqual(1, sent.OfType<ScpPacket>().Count());
            Assert.AreEqual(1, sent.OfType<ScnPacket>().Count());
        }

        [TestMethod]
        public async Task OpeningThePetWindowWithoutPetsOnlyClears()
        {
            await new NpinfoPacketHandler().ExecuteAsync(new NpInfoPacket { Page = 0 }, _session);

            Assert.AreEqual(1, _session.LastPackets.Count);
            Assert.IsInstanceOfType(_session.LastPackets.Single(), typeof(PclearPacket));
        }

        [TestMethod]
        public async Task RefreshingThePetWindowGetsNoReply()
        {
            await new ScpCtsPacketHandler().ExecuteAsync(new ScpCtsPacket(), _session);

            Assert.AreEqual(0, _session.LastPackets.Count, "the recording shows no answer to sc_p_cts");
        }
    }
}
