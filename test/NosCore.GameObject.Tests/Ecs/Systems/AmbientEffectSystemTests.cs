//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class AmbientEffectSystemTests
    {
        private TestWorld World;
        private MapInstance _map;
        private readonly AmbientEffectSystem _system = new();
        private readonly NpcMonsterDto _tower = new() { NpcMonsterVNum = 955, MaxHp = 100, Level = 1 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(0);
        }

        [TestMethod]
        public void AnNpcPlaysItsAuraOncePerDelay()
        {
            _map.EcsWorld.CreateNpc(301, _tower, _map, 12, 14, 2, 12, 14, false, false, null, 857, 4750, null);

            var first = TakeDue();
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(VisualType.Npc, first[0].EffectType);
            Assert.AreEqual(301, first[0].VisualEntityId);
            Assert.AreEqual(857, first[0].Id);

            World.Clock.Advance(Duration.FromMilliseconds(4000));
            Assert.AreEqual(0, TakeDue().Count);

            World.Clock.Advance(Duration.FromMilliseconds(750));
            Assert.AreEqual(1, TakeDue().Count);
        }

        [TestMethod]
        public void AnNpcWithoutAuraPlaysNothing()
        {
            _map.EcsWorld.CreateNpc(302, _tower, _map, 12, 14, 2, 12, 14, false, false, null, 0, 4750, null);

            Assert.AreEqual(0, TakeDue().Count);
        }

        private List<EffectPacket> TakeDue()
        {
            var due = new List<EffectPacket>();
            _system.TakeDue(_map.EcsWorld, World.Clock.GetCurrentInstant(), due);
            return due;
        }
    }
}
