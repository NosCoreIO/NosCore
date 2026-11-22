//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Game;
using NosCore.Packets;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class PetWindowPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.LastPackets.Clear();
        }

        [TestMethod]
        public async Task OpeningThePetWindowResendsTheRoster()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId);
            var partner = TestMates.Partner(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            _session.Character.Mates[partner.MateTransportId] = partner;

            await World.Create<NpinfoPacketHandler>().ExecuteAsync(new NpInfoPacket { Page = 0 }, _session);

            var sent = _session.LastPackets.ToList();
            Assert.IsInstanceOfType(sent[0], typeof(PclearPacket), "the list follows a p_clear");
            Assert.AreEqual(1, sent.OfType<ScpPacket>().Count());
            Assert.AreEqual(1, sent.OfType<ScnPacket>().Count());
        }

        [TestMethod]
        public async Task ThePetWindowShowsThePetsOwnAttackAndDefenceLevels()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId);
            pet.Attack = 3;
            pet.Defence = 2;
            pet.NpcMonster.AttackUpgrade = 5;
            pet.NpcMonster.DefenceUpgrade = 7;
            _session.Character.Mates[pet.MateTransportId] = pet;

            await World.Create<NpinfoPacketHandler>().ExecuteAsync(new NpInfoPacket { Page = 0 }, _session);

            var scp = _session.LastPackets.OfType<ScpPacket>().Single();
            Assert.AreEqual(3, scp.AttackUpgrade, "the pet's own attack level, not its monster's weapon upgrade");
            Assert.AreEqual(2, scp.DefenceUpgrade, "the pet's own defence level, not its monster's armour upgrade");
        }

        [TestMethod]
        public void TheClientsPetWindowRequestReachesTheHandler()
        {
            var packetTypes = PacketTypes.World();

            var packet = new Deserializer(packetTypes).Deserialize(new Serializer(packetTypes).Serialize(new NpInfoPacket { Page = 0 }));

            Assert.IsInstanceOfType(packet, typeof(NpinfoPacketHandler).BaseType.NotNull().GetGenericArguments()[0],
                "handlers are looked up by the exact type the deserializer produces");
        }

        [TestMethod]
        public async Task OpeningThePetWindowWithoutPetsOnlyClears()
        {
            await World.Create<NpinfoPacketHandler>().ExecuteAsync(new NpInfoPacket { Page = 0 }, _session);

            Assert.AreEqual(1, _session.LastPackets.Count);
            Assert.IsInstanceOfType(_session.LastPackets.Single(), typeof(PclearPacket));
        }

        [TestMethod]
        public async Task RefreshingThePetWindowGetsNoReply()
        {
            await World.Create<ScpCtsPacketHandler>().ExecuteAsync(new ScpCtsPacket(), _session);

            Assert.AreEqual(0, _session.LastPackets.Count, "the recording shows no answer to sc_p_cts");
        }
    }
}
