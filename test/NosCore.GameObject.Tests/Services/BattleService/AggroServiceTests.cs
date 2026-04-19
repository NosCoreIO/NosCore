//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NodaTime.Testing;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // The aggro service touches AggroComponent on the underlying bundle world. Our test
    // doubles are not full bundles, so these tests exercise the observable behavior:
    // calling AddThreat on a non-bundle entity is a no-op (no component to read), and
    // Current returns "no target" for such entities.
    [TestClass]
    public class AggroServiceTests
    {
        [TestMethod]
        public void NonBundleEntityHasNoAggro()
        {
            var service = new AggroService(new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)));
            var mob = new FakeEntity();
            var player = new FakeEntity();

            service.AddThreat(mob, player, 50);
            var snap = service.Current(mob);

            Assert.IsFalse(snap.HasTarget);
            Assert.AreEqual(0, snap.TargetVisualId);
        }

        private class FakeEntity : IAliveEntity
        {
            public Entity Handle { get; set; }
            public bool IsSitting { get; set; }
            public byte Speed { get; set; }
            public byte Size { get; set; }
            public int Mp { get; set; } = 100;
            public int Hp { get; set; } = 100;
            public short Morph => 0;
            public byte MorphUpgrade => 0;
            public short MorphDesign => 0;
            public byte MorphBonus => 0;
            public bool NoAttack => false;
            public bool NoMove => false;
            public bool IsAlive => Hp > 0;
            public short MapX => 0;
            public short MapY => 0;
            public int MaxHp => 100;
            public int MaxMp => 100;
            public byte Level { get; set; } = 1;
            public byte HeroLevel => 0;
            public short Race => 0;
            public Shop? Shop { get; set; }
            public SemaphoreSlim HitSemaphore { get; } = new(1, 1);
            public ConcurrentDictionary<Entity, int> HitList { get; } = new();
            public VisualType VisualType => VisualType.Monster;
            public short VNum => 0;
            public long VisualId => 1;
            public byte Direction { get; set; }
            public Guid MapInstanceId { get; }
            public NosCore.GameObject.Services.MapInstanceGenerationService.MapInstance MapInstance { get; set; } = null!;
            public short PositionX { get; set; }
            public short PositionY { get; set; }
        }
    }
}
