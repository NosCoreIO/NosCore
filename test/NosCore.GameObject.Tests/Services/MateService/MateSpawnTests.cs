//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateSpawnTests
    {
        private TestWorld World;
        private ClientSession _session;
        private GameObject.Services.MateService.Mate _pet;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            _session.Character.MapInstance.LastPackets.Clear();
            _session.LastPackets.Clear();
        }

        [TestMethod]
        public async Task ASpawnedMateStandsWhereItWasPut()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);

            Assert.IsNotNull(_pet.Entity);
            Assert.AreEqual((7, 9), ((int)_pet.Entity.NotNull().PositionX, (int)_pet.Entity.NotNull().PositionY));
            var spawn = _session.Character.MapInstance.LastPackets.OfType<InPacket>().Single();
            Assert.AreEqual(_pet.MateTransportId, spawn.VisualId);
            Assert.AreEqual((short)7, spawn.PositionX);
        }

        [TestMethod]
        public async Task AHiddenOwnersMateIsOnlyShownToTheOwner()
        {
            _session.Character.Invisible = true;

            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);

            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<InPacket>().Any());
            Assert.IsTrue(_session.LastPackets.OfType<InPacket>().Any(s => s.VisualId == _pet.MateTransportId));
        }

        [TestMethod]
        public async Task ADespawnedMateLeavesTheMap()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            _session.Character.MapInstance.LastPackets.Clear();

            await _pet.DespawnAsync(_session.Character.MapInstance);

            Assert.IsNull(_pet.Entity);
            var gone = _session.Character.MapInstance.LastPackets.OfType<OutPacket>().Single();
            Assert.AreEqual(VisualType.Npc, gone.VisualType);
            Assert.AreEqual(_pet.MateTransportId, gone.VisualId);
        }

        [TestMethod]
        public async Task DespawningAMateThatIsNotOutSendsNothing()
        {
            await _pet.DespawnAsync(_session.Character.MapInstance);

            Assert.AreEqual(0, _session.Character.MapInstance.LastPackets.Count);
        }

        [TestMethod]
        public async Task AnOutMatesBarsReadItsEntity()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            var entity = _pet.Entity.NotNull();
            entity.Hp = 78;

            Assert.AreEqual(50, _pet.GeneratePst().HpLeft);
        }

        [TestMethod]
        public async Task AMateKeepsTheHealthItLostWhileOut()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            var entity = _pet.Entity.NotNull();
            entity.Hp = 40;
            entity.Mp = 3;

            await _pet.DespawnAsync(_session.Character.MapInstance);

            Assert.AreEqual((40, 3), (_pet.Hp, _pet.Mp));
        }

        [TestMethod]
        public async Task RefillingAnOutMateFillsItsEntity()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            var entity = _pet.Entity.NotNull();
            entity.Hp = 1;
            entity.Mp = 0;

            _pet.Refill();

            Assert.AreEqual((156, 10), (entity.Hp, entity.Mp));
        }

        [TestMethod]
        public async Task ADespawnNamesTheSameIdTheSpawnDid()
        {
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            await _pet.DespawnAsync(_session.Character.MapInstance);

            var packets = _session.Character.MapInstance.LastPackets;
            Assert.AreEqual(packets.OfType<InPacket>().Single().VisualId, packets.OfType<OutPacket>().Single().VisualId);
        }
    }
}
