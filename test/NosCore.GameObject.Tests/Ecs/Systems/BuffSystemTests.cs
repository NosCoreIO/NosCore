//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Systems
{
    [TestClass]
    public class BuffSystemTests
    {
        private static readonly CardDto ShortBuff = new() { CardId = 455, Duration = 10 };
        private static readonly CardDto LongBuff = new() { CardId = 456, Duration = 600 };

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private readonly BuffSystem _system = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task OnlyAnExpiredBuffMakesAnEntityDue()
        {
            var buffless = Monster(9001);
            var expiring = Monster(9002);
            var lasting = Monster(9003);
            await _buffs.ApplyAsync(expiring, ShortBuff, [], null);
            await _buffs.ApplyAsync(lasting, LongBuff, [], null);

            World.Clock.Advance(Duration.FromSeconds(2));

            CollectionAssert.AreEqual(new[] { (expiring.Entity, VisualType.Monster) }, FindDue());
        }

        [TestMethod]
        public async Task APeriodicEffectIsDueOnlyOnItsStep()
        {
            var character = _session.Character;
            await _buffs.ApplyAsync(character, LongBuff, [HpReduced(steps: 2)], null);

            World.Clock.Advance(Duration.FromSeconds(3));
            Assert.AreEqual(0, FindDue().Count);

            World.Clock.Advance(Duration.FromSeconds(1));
            CollectionAssert.AreEqual(new[] { (character.Entity, VisualType.Player) }, FindDue());

            await _buffs.TickAsync(character);
            Assert.AreEqual(0, FindDue().Count);
        }

        [TestMethod]
        public async Task ASkillBuffNeverPulses()
        {
            var monster = Monster(9001);
            await _buffs.ApplySkillBuffAsync(monster, 1, 600, [HpReduced(steps: 1)], null);

            World.Clock.Advance(Duration.FromSeconds(10));

            Assert.AreEqual(0, FindDue().Count);
        }

        private List<(Entity, VisualType)> FindDue()
        {
            var due = new List<(Entity Entity, VisualType VisualType)>();
            _system.FindDue(_session.Character.MapInstance.EcsWorld, World.Clock.GetCurrentInstant(), due);
            return due.Select(d => (d.Entity, d.VisualType)).ToList();
        }

        private MonsterComponentBundle Monster(int visualId)
        {
            var map = _session.Character.MapInstance;
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = 1, MaxHp = 100, MaxMp = 10 };
            return new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(visualId, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
        }

        private static BCardDto HpReduced(int steps) => new()
        {
            Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 12, FirstData = 8, ThirdData = steps,
        };
    }
}
