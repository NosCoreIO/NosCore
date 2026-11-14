//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class NonPlayerInPacketTests
    {
        private TestWorld World;
        private MapInstance _map;
        private readonly NpcMonsterDto _creature = new() { NpcMonsterVNum = 24, MaxHp = 200, MaxMp = 0, Level = 5 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(0);
        }

        [TestMethod]
        public void AMonsterIsSummonedIn()
        {
            var monster = new MonsterComponentBundle(
                _map.EcsWorld.CreateMonster(300, _creature, _map, 12, 14, 2, 12, 14, false, true, false), _map.EcsWorld);
            monster.Hp = 50;

            var packet = monster.GenerateIn();

            Assert.AreEqual(VisualType.Monster, packet.VisualType);
            Assert.AreEqual(300, packet.VisualId);
            Assert.AreEqual("24", packet.VNum);
            Assert.AreEqual(SpawnEffectType.Summon, packet.InNonPlayerSubPacket?.SpawnEffect);
            Assert.AreEqual(25, packet.InNonPlayerSubPacket?.InAliveSubPacket?.Hp);
            Assert.AreEqual(100, packet.InNonPlayerSubPacket?.InAliveSubPacket?.Mp);
        }

        [TestMethod]
        public void AnNpcAppearsWithItsDialog()
        {
            var npc = new NpcComponentBundle(
                _map.EcsWorld.CreateNpc(301, _creature, _map, 12, 14, 2, 12, 14, false, false, 17, 0, 0, null), _map.EcsWorld);

            var packet = npc.GenerateIn();

            Assert.AreEqual(VisualType.Npc, packet.VisualType);
            Assert.AreEqual(301, packet.VisualId);
            Assert.AreEqual(SpawnEffectType.NoEffect, packet.InNonPlayerSubPacket?.SpawnEffect);
            Assert.AreEqual((short)17, packet.InNonPlayerSubPacket?.Dialog);
            Assert.AreEqual((short)40, npc.GenerateIn(40).InNonPlayerSubPacket?.Dialog);
        }
    }
}
