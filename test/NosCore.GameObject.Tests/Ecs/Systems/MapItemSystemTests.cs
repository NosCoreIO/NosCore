//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Tests.Shared;
using System.Collections.Generic;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class MapItemSystemTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        [TestMethod]
        public void OnlyItemsLyingOnTheMapAreCountedAndFound()
        {
            var map = World.GetBaseMap(0);
            map.LoadMonsters(new List<MapMonsterDto> { new() { MapMonsterId = 7, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 10 } });
            var first = map.EcsWorld.CreateMapItem(7, 1012, 1, map.MapInstanceId, 0, 0, null, Instant.MinValue, Guid.NewGuid(), null);
            map.EcsWorld.CreateMapItem(8, 1012, 1, map.MapInstanceId, 0, 0, null, Instant.MinValue, Guid.NewGuid(), null);
            var system = new MapItemSystem();

            Assert.AreEqual(2, system.Count(map.EcsWorld));
            Assert.AreEqual(first, system.Find(map.EcsWorld, 7));
            Assert.IsNull(system.Find(map.EcsWorld, 9));
        }
    }
}
