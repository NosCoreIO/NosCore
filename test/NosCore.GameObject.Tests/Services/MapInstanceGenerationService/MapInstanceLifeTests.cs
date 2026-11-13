//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Tests.Services.MapInstanceGenerationService
{
    [TestClass]
    public class MapInstanceLifeTests
    {
        private TestWorld World;
        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
        }

        private MapInstance NewMap(IBuffService? buffs = null, IRegenerationService? regeneration = null) =>
            new(new GameObject.Map.Map
                {
                    MapId = 0,
                    NameI18NKey = "testMap",
                    Data = new byte[] { 2, 0, 2, 0, 0, 0, 0, 0 },
                },
                Guid.NewGuid(), false, MapInstanceType.NormalInstance, World.MapItemProvider.NotNull(),
                NullLogger<MapInstance>.Instance, new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object, World.Clock, World.MapChangeService,
                World.SessionGroupFactory, World.SessionRegistry,
                World.DistanceCalculator, buffService: buffs, regenerationService: regeneration);

        [TestMethod]
        public async Task OneFailingEntityDoesNotCostTheRestOfTheMapItsTick()
        {
            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.TickAsync(It.IsAny<IAliveEntity>())).ThrowsAsync(new InvalidOperationException());
            var map = NewMap(buffs.Object);
            map.LoadMonsters(new List<MapMonsterDto> { new() { MapMonsterId = 1, VNum = 1 }, new() { MapMonsterId = 2, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 10 } });
            var buffed = map.GetMonsterById(1).NotNull();
            var now = World.Clock.GetCurrentInstant();
            buffed.ActiveBuffs[1] = new BuffInstance(1, BuffType.Bad, null, now.Minus(Duration.FromSeconds(2)),
                now.Minus(Duration.FromSeconds(1)), []);
            var dead = map.GetMonsterById(2).NotNull();
            dead.Hp = 0;
            dead.IsAlive = false;
            dead.RespawnAt = now;
            map.IsSleeping = false;
            await map.StartLifeAsync();

            for (var waited = 0; !dead.IsAlive && waited < 100; waited++)
            {
                await Task.Delay(50);
            }

            var respawned = dead.IsAlive;
            await map.DisposeAsync();

            buffs.Verify(b => b.TickAsync(It.IsAny<IAliveEntity>()), Times.AtLeastOnce());
            Assert.IsTrue(respawned, "the respawn never ran once a buff tick threw");
        }

        [TestMethod]
        public async Task ADeadMonsterComesBackWholeAtItsSpawnOnceItsTimeHasCome()
        {
            var map = NewMap();
            map.LoadMonsters(new List<MapMonsterDto> { new() { MapMonsterId = 1, VNum = 1, MapX = 1, MapY = 0 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 10, MaxMp = 5 } });
            var monster = map.GetMonsterById(1).NotNull();
            monster.Hp = 0;
            monster.Mp = 0;
            monster.IsAlive = false;
            monster.PositionX = 0;
            monster.RespawnAt = World.Clock.GetCurrentInstant();
            map.IsSleeping = false;
            await map.StartLifeAsync();

            for (var waited = 0; !monster.IsAlive && waited < 100; waited++)
            {
                await Task.Delay(50);
            }

            Assert.IsTrue(monster.IsAlive);
            Assert.AreEqual(10, monster.Hp);
            Assert.AreEqual(5, monster.Mp);
            Assert.AreEqual(1, monster.PositionX);
            Assert.IsNull(monster.RespawnAt);
            await map.DisposeAsync();
        }

        [TestMethod]
        public async Task RemovingAMapStopsItsLifeAndReleasesItsWorld()
        {
            var map = NewMap();
            await World.MapInstanceGeneratorService.AddMapInstanceAsync(map);
            await map.StartLifeAsync();
            var item = map.EcsWorld.CreateMapItem(1, 1012, 1, map.MapInstanceId, 0, 0, null, Instant.MinValue,
                Guid.NewGuid(), null);

            await World.MapInstanceGeneratorService.RemoveMapAsync(map.MapInstanceId);

            Assert.IsTrue(map.Executor.IsClosed);
            Assert.IsFalse(map.EcsWorld.IsAlive(item));
        }

        [TestMethod]
        public async Task OnlyOnePickerClaimsAnItem()
        {
            var map = NewMap();
            for (var round = 0; round < 2_000; round++)
            {
                map.EcsWorld.CreateMapItem(round, 1012, 1, map.MapInstanceId, 0, 0, null, Instant.MinValue,
                    Guid.NewGuid(), null);
                using var start = new Barrier(4);
                var visualId = round;
                var claims = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
                {
                    start.SignalAndWait();
                    try
                    {
                        return map.TryRemoveMapItem(visualId);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })));

                Assert.AreEqual(1, claims.Count(claimed => claimed), $"round {round}");
            }

            await map.DisposeAsync();
        }

        [TestMethod]
        public async Task WorkOnAMapRunsOneItemAtATime()
        {
            var map = NewMap();
            var inside = 0;
            var overlapped = false;

            await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => Task.Run(() => map.Executor.RunAsync(async () =>
            {
                if (Interlocked.Increment(ref inside) > 1)
                {
                    overlapped = true;
                }

                await Task.Yield();
                Interlocked.Decrement(ref inside);
            }))));

            Assert.IsFalse(overlapped);
            await map.DisposeAsync();
        }

        [TestMethod]
        public async Task WorkAlreadyOnAMapCanAskForItAgain()
        {
            var map = NewMap();
            var ran = false;

            var nested = map.Executor.RunAsync(() => map.Executor.RunAsync(() =>
            {
                ran = true;
                return Task.CompletedTask;
            }));

            Assert.AreSame(nested, await Task.WhenAny(nested, Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.IsTrue(ran);
            await map.DisposeAsync();
        }

        [TestMethod]
        public async Task TwoMapsTakenInOppositeOrdersDoNotDeadlock()
        {
            var first = NewMap();
            var second = NewMap();

            var both = Task.WhenAll(Enumerable.Range(0, 200).Select(i => Task.Run(() =>
                i % 2 == 0
                    ? MapExecutor.RunExclusiveAsync(first.Executor, second.Executor, () => Task.Delay(1))
                    : MapExecutor.RunExclusiveAsync(second.Executor, first.Executor, () => Task.Delay(1)))));

            Assert.AreSame(both, await Task.WhenAny(both, Task.Delay(TimeSpan.FromSeconds(30))));
            await first.DisposeAsync();
            await second.DisposeAsync();
        }

        [TestMethod]
        public async Task AMapCanBeDisposedFromItsOwnTick()
        {
            var map = NewMap();

            var disposing = map.Executor.RunAsync(() => map.DisposeAsync().AsTask());

            Assert.AreSame(disposing, await Task.WhenAny(disposing, Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.IsTrue(map.Executor.IsClosed);
        }
    }
}
