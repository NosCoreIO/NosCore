//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class RespawnSystemTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        [TestMethod]
        public void OnlyEntitiesWhoseTimeHasComeAreTakenAndOnlyOnce()
        {
            var map = World.GetBaseMap(0);
            map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 1, VNum = 1 }, new() { MapMonsterId = 2, VNum = 1 }, new() { MapMonsterId = 3, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 10 } });
            var due = map.GetMonsterById(1).NotNull();
            var later = map.GetMonsterById(2).NotNull();
            var up = map.GetMonsterById(3).NotNull();
            var now = World.Clock.GetCurrentInstant();
            due.RespawnAt = now.Minus(Duration.FromSeconds(1));
            later.RespawnAt = now.Plus(Duration.FromSeconds(1));
            var system = new RespawnSystem();

            var first = new List<Entity>();
            system.TakeDue(map.EcsWorld, now, first);
            var second = new List<Entity>();
            system.TakeDue(map.EcsWorld, now, second);

            CollectionAssert.AreEqual(new[] { due.Entity }, first);
            Assert.AreEqual(0, second.Count);
            Assert.IsNull(due.RespawnAt);
            Assert.IsNotNull(later.RespawnAt);
            Assert.IsNull(up.RespawnAt);
        }
    }
}
