//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Networking.SessionGroup;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MapChangeService
{
    [TestClass]
    public class MapChangeConcurrencyTests
    {
        private TestWorld World;
        private static readonly QueryDescription Players = new QueryDescription().WithAll<PlayerComponent>();

        private ClientSession _session;
        private MapInstance _start;
        private MapInstance _first;
        private MapInstance _second;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _start = World.GetBaseMap(0);
            _first = World.GetBaseMap(1);
            _second = World.GetMap(World.MinilandId);
        }

        private Task ChangeMapAsync(MapInstance to) =>
            World.MapChangeService.ChangeMapInstanceAsync(_session, to.MapInstanceId, 1, 1);

        private int PlayerEntities() =>
            new[] { _start, _first, _second }.Sum(map => map.EcsWorld.Exclusive(w => w.CountEntities(in Players)));

        [TestMethod]
        public async Task TwoMapChangesAtOnceMoveThePlayerOnce()
        {
            for (var round = 0; round < 50; round++)
            {
                await Task.WhenAll(
                    Task.Run(() => ChangeMapAsync(_first)),
                    Task.Run(() => ChangeMapAsync(_second)));

                Assert.IsTrue(_session.HasPlayerEntity, $"round {round}: the player lost their character");
                Assert.AreEqual(1, PlayerEntities(), $"round {round}");
            }
        }

        [TestMethod]
        public async Task TheCharacterStaysReadableWhileThePlayerChangesMap()
        {
            using var stop = new CancellationTokenSource();
            var failures = 0;
            var reader = Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        _ = _session.Character.Hp;
                    }
                    catch (InvalidOperationException)
                    {
                        Interlocked.Increment(ref failures);
                    }
                }
            });

            for (var round = 0; round < 50; round++)
            {
                await ChangeMapAsync(round % 2 == 0 ? _first : _second);
            }

            await stop.CancelAsync();
            await reader;
            Assert.AreEqual(0, failures);
        }

        [TestMethod]
        public async Task WritesMadeDuringAMapChangeAreKept()
        {
            using var stop = new CancellationTokenSource();
            var lost = 0;
            var writer = Task.Run(() =>
            {
                long gold = 0;
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        _session.Character.Gold = ++gold;
                        if (_session.Character.Gold < gold)
                        {
                            Interlocked.Increment(ref lost);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        gold--;
                    }
                }
            });

            for (var round = 0; round < 50; round++)
            {
                await ChangeMapAsync(round % 2 == 0 ? _first : _second);
            }

            await stop.CancelAsync();
            await writer;
            Assert.AreEqual(0, lost);
        }

        [TestMethod]
        public async Task AnEventPublishedBeforeAMapChangeSeesThePlayerAfterIt()
        {
            _session.Character.Hp = 42;
            var damaged = new EntityDamagedEvent(_session.Character, _session.Character, 1, false);

            await ChangeMapAsync(_first);

            Assert.AreEqual(42, damaged.Target.Hp);
            Assert.AreEqual(_first.MapInstanceId, damaged.Target.MapInstance.MapInstanceId);
        }

        [TestMethod]
        public void AMapWithSomebodyOnItDoesNotFallAsleep()
        {
            var clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            var map = new MapInstance(new Map.Map { MapId = 0, NameI18NKey = "testMap", Data = new byte[] { 2, 0, 2, 0, 0, 0, 0, 0 } },
                Guid.NewGuid(), false, MapInstanceType.NormalInstance, World.MapItemProvider.NotNull(),
                NullLogger<MapInstance>.Instance, clock, World.MapChangeService,
                World.SessionGroupFactory, World.SessionRegistry,
                World.DistanceCalculator);
            var sessions = new Mock<ISessionGroup>();
            map.Sessions = sessions.Object;

            // The last player leaves and asks for sleep while somebody else arrives.
            map.IsSleeping = false;
            map.LastUnregister = clock.GetCurrentInstant();
            map.IsSleeping = true;
            sessions.Setup(s => s.Count).Returns(1);
            clock.Advance(Duration.FromMinutes(1));

            Assert.IsFalse(map.IsSleeping);
            map.Dispose();
        }
    }
}
