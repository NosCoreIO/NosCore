//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Ecs
{
    [TestClass]
    public class MapWorldConcurrencyTests
    {
        private const int Iterations = 200_000;

        private static MonsterComponentBundle Monster(MapWorld world, int id) =>
            new(world.Exclusive(w => w.Create(
                new EntityIdentityComponent(id, VisualType.Monster, 0),
                new HealthComponent(id, id, true))), world);

        [TestMethod]
        public async Task WritersOfDifferentFieldsOfOneComponentKeepEachOthersWrites()
        {
            using var world = new MapWorld();
            var monster = Monster(world, 0);
            var hpWentBack = false;

            var hpWriter = Task.Run(() =>
            {
                for (var i = 1; i <= Iterations; i++)
                {
                    monster.Hp = i;
                    if (monster.Hp < i)
                    {
                        hpWentBack = true;
                    }
                }
            });
            var maxHpWriter = Task.Run(() =>
            {
                for (var i = 1; i <= Iterations; i++)
                {
                    monster.MaxHp = i;
                }
            });
            await Task.WhenAll(hpWriter, maxHpWriter);

            Assert.IsFalse(hpWentBack, "a write to MaxHp put back an older Hp");
            Assert.AreEqual(Iterations, monster.Hp);
            Assert.AreEqual(Iterations, monster.MaxHp);
        }

        [TestMethod]
        public async Task EntitiesComingAndGoingLeaveTheOthersIntact()
        {
            using var world = new MapWorld();
            var residents = Enumerable.Range(1, 64).Select(i => Monster(world, i)).ToArray();
            var failures = new ConcurrentQueue<string>();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Same archetype as the residents, so every destroy moves a resident inside its chunk.
            var churn = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        var visitor = Monster(world, -1);
                        world.DestroyEntity(visitor.Entity);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e.GetType().Name);
                    }
                }
            }));

            var residentsWork = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    foreach (var resident in residents)
                    {
                        try
                        {
                            var id = (int)resident.VisualId;
                            resident.Hp = id;
                            if (resident.Hp != id || resident.MaxHp != id)
                            {
                                failures.Enqueue($"resident {id} read Hp {resident.Hp} MaxHp {resident.MaxHp}");
                            }
                        }
                        catch (Exception e)
                        {
                            failures.Enqueue(e.GetType().Name);
                        }
                    }
                }
            }));

            await Task.WhenAll(churn.Concat(residentsWork));

            Assert.IsTrue(failures.IsEmpty, string.Join(", ", failures.Distinct().Take(5)));
            foreach (var resident in residents)
            {
                Assert.IsTrue(world.IsAlive(resident.Entity));
                Assert.AreEqual((int)resident.VisualId, resident.Hp);
            }
        }

        [TestMethod]
        public async Task OnlyOneOfSeveralRemoversDestroysAnEntity()
        {
            using var world = new MapWorld();
            for (var round = 0; round < 2_000; round++)
            {
                var item = world.CreateMapItem(round, 1012, 1, Guid.Empty, 0, 0, null, Instant.MinValue,
                    Guid.NewGuid(), null);
                using var start = new Barrier(4);
                var removals = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
                {
                    start.SignalAndWait();
                    try
                    {
                        return world.DestroyEntity(item);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })));

                Assert.AreEqual(1, removals.Count(removed => removed), $"round {round}");
            }
        }

        [TestMethod]
        public void ADisposedWorldReadsAsEmptyInsteadOfThrowing()
        {
            var world = new MapWorld();
            var monster = Monster(world, 1);
            world.Dispose();

            Assert.IsFalse(world.IsAlive(monster.Entity));
            Assert.IsNull(world.TryGetComponent<HealthComponent>(monster.Entity));
            monster.Hp = 5;
        }
    }
}
