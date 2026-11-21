//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class MapItemDespawnSystemTests
    {
        private TestWorld World;
        private MapInstance _map;
        private readonly MapItemDespawnSystem _system = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(0);
        }

        [TestMethod]
        public void AnItemLyingForItsWholeLifetimeIsRemoved()
        {
            var item = DropItem(7);
            World.Clock.Advance(MapItemDespawnSystem.Lifetime);

            CollectionAssert.AreEqual(new List<long> { 7 }, TakeExpired());
            Assert.IsFalse(_map.EcsWorld.IsAlive(item));
        }

        [TestMethod]
        public void AnItemYoungerThanItsLifetimeStays()
        {
            var item = DropItem(7);
            World.Clock.Advance(MapItemDespawnSystem.Lifetime - Duration.FromSeconds(1));

            Assert.AreEqual(0, TakeExpired().Count);
            Assert.IsTrue(_map.EcsWorld.IsAlive(item));
        }

        [TestMethod]
        public void OnlyTheExpiredItemsGo()
        {
            DropItem(7);
            World.Clock.Advance(Duration.FromSeconds(100));
            var fresh = DropItem(8);
            World.Clock.Advance(Duration.FromSeconds(80));

            CollectionAssert.AreEqual(new List<long> { 7 }, TakeExpired());
            Assert.IsTrue(_map.EcsWorld.IsAlive(fresh));
            Assert.AreEqual(1, _map.MapItemCount);
        }

        private Arch.Core.Entity DropItem(long visualId) =>
            _map.EcsWorld.CreateMapItem(visualId, 1012, 1, _map.MapInstanceId, 0, 0, null,
                World.Clock.GetCurrentInstant(), Guid.NewGuid(), null);

        private List<long> TakeExpired()
        {
            var removed = new List<long>();
            _system.TakeExpired(_map.EcsWorld, World.Clock.GetCurrentInstant(), removed);
            return removed;
        }
    }
}
