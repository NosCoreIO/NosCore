//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class PeriodicBuffTests
    {
        private static readonly CardDto Heat = new() { CardId = 339, Duration = 0 };
        private static readonly CardDto Poison = new() { CardId = 455, Duration = 600 };
        private static readonly CardDto Bleeding = new() { CardId = 42, Duration = 200 };

        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task HpDropsByItsPercentEveryStep()
        {
            var character = _session.Character;
            character.Hp = character.MaxHp;
            await _buffs.ApplyPermanentAsync(character, Heat, [HpReduced(8, 1)]);

            World.Clock.Advance(Duration.FromSeconds(1));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxHp, character.Hp);

            World.Clock.Advance(Duration.FromSeconds(1));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxHp - character.MaxHp * 8 / 100, character.Hp);
        }

        [TestMethod]
        public async Task ThirdDataCountsStepsOfTwoSeconds()
        {
            var character = _session.Character;
            character.Hp = character.MaxHp;
            await _buffs.ApplyPermanentAsync(character, Heat, [HpReduced(8, 2)]);

            World.Clock.Advance(Duration.FromSeconds(3));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxHp, character.Hp);

            World.Clock.Advance(Duration.FromSeconds(1));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxHp - character.MaxHp * 8 / 100, character.Hp);
        }

        [TestMethod]
        public async Task PeriodicDamageLeavesOneHp()
        {
            var character = _session.Character;
            character.Hp = 2;
            await _buffs.ApplyPermanentAsync(character, Heat, [HpReduced(50, 1)]);

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);

            Assert.AreEqual(1, character.Hp);
        }

        [TestMethod]
        public async Task ABCardWithoutAnIntervalDoesNotPulse()
        {
            var character = _session.Character;
            character.Hp = character.MaxHp;
            await _buffs.ApplyPermanentAsync(character, Heat, [HpReduced(8, 0)]);

            World.Clock.Advance(Duration.FromSeconds(10));
            await _buffs.TickAsync(character);

            Assert.AreEqual(character.MaxHp, character.Hp);
        }

        [TestMethod]
        public async Task MpRecoversUpToItsMaximum()
        {
            var character = _session.Character;
            character.Mp = character.MaxMp - 1;
            await _buffs.ApplyPermanentAsync(character, Heat, [new BCardDto
            {
                Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 21, FirstData = 20, ThirdData = 1,
            }]);

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);

            Assert.AreEqual(character.MaxMp, character.Mp);
        }

        [TestMethod]
        public async Task AMonsterLosesHpEveryStepAndEveryoneSeesTheBar()
        {
            var monster = Monster(100);
            await _buffs.ApplyAsync(monster, Poison, [HpReduced(8, 2)], null);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(4));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(92, monster.Hp);
            var st = map.LastPackets.OfType<StPacket>().Single(p => p.VisualId == monster.VisualId);
            Assert.AreEqual(92, st.CurrentHp);
        }

        [TestMethod]
        public async Task PeriodicDamageNeverKillsAMonster()
        {
            var monster = Monster(100);
            monster.Hp = 3;
            await _buffs.ApplyAsync(monster, Poison, [HpReduced(50, 1)], null);

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(1, monster.Hp);
            Assert.IsTrue(monster.IsAlive);
        }

        [TestMethod]
        public async Task AMonsterRegeneratesUpToItsMaximum()
        {
            var monster = Monster(100);
            monster.Hp = 90;
            await _buffs.ApplyAsync(monster, Poison, [new BCardDto
            {
                Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 11, FirstData = 20, ThirdData = 3,
            }], null);

            World.Clock.Advance(Duration.FromSeconds(6));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(100, monster.Hp);
        }

        [TestMethod]
        public async Task TheHpAMonsterLosesFloatsOverItForTheMap()
        {
            var monster = Monster(200);
            await _buffs.ApplyAsync(monster, Poison, [HpReduced(8, 2)], null);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(4));
            await _buffs.TickAsync(monster);

            var shown = map.LastPackets.OfType<DmPacket>().Single();
            Assert.AreEqual(VisualType.Monster, shown.VisualType);
            Assert.AreEqual(monster.VisualId, shown.VisualId);
            Assert.AreEqual(16, shown.Damages);
        }

        [TestMethod]
        public async Task TheNumberIsWhatWasTakenWhenOneHpIsLeft()
        {
            var monster = Monster(200);
            monster.Hp = 5;
            await _buffs.ApplyAsync(monster, Poison, [HpReduced(8, 2)], null);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(4));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(4, map.LastPackets.OfType<DmPacket>().Single().Damages);
        }

        [TestMethod]
        public async Task TheHpAMonsterRecoversFloatsOverItForTheMap()
        {
            var monster = Monster(200);
            monster.Hp = 190;
            await _buffs.ApplyAsync(monster, Poison, [HpRecovered(20, 3)], null);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(6));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(10, map.LastPackets.OfType<RcPacket>().Single().HealAmount);
            Assert.IsFalse(map.LastPackets.OfType<DmPacket>().Any());
        }

        [TestMethod]
        public async Task NoNumberWhenOnlyMpChanges()
        {
            var monster = Monster(200);
            monster.Mp = 10;
            await _buffs.ApplyAsync(monster, Poison, [new BCardDto
            {
                Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 22, FirstData = 50, ThirdData = 2,
            }], null);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(4));
            await _buffs.TickAsync(monster);

            Assert.AreEqual(5, monster.Mp);
            Assert.IsFalse(map.LastPackets.OfType<DmPacket>().Any());
            Assert.IsFalse(map.LastPackets.OfType<RcPacket>().Any());
        }

        [TestMethod]
        public async Task APlayersOwnLossFloatsOverThemForTheMap()
        {
            var character = _session.Character;
            character.Hp = character.MaxHp;
            await _buffs.ApplyPermanentAsync(character, Heat, [HpReduced(8, 1)]);
            character.MapInstance.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);

            var shown = character.MapInstance.LastPackets.OfType<DmPacket>().Single();
            Assert.AreEqual(VisualType.Player, shown.VisualType);
            Assert.AreEqual(character.MaxHp * 8 / 100, shown.Damages);
        }

        private static BCardDto HpRecovered(int percent, int steps) => new()
        {
            Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 11, FirstData = percent, ThirdData = steps,
        };

        [TestMethod]
        public async Task BleedingTakesItsValueTimesTheLevelOfWhoeverInflictedIt()
        {
            var victim = Monster(5000);
            var attacker = Monster(100, level: 42, id: 9002);
            await _buffs.ApplyAsync(victim, Bleeding, [Fixed(HpTaken, 4, steps: 1, perLevel: true)], attacker);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(victim);

            Assert.AreEqual(5000 - 168, victim.Hp);
            Assert.AreEqual(168, map.LastPackets.OfType<DmPacket>().Single().Damages);
        }

        [TestMethod]
        public async Task AFixedAmountThatDoesNotScaleIsTakenAsItIs()
        {
            var victim = Monster(5000);
            await _buffs.ApplyAsync(victim, Bleeding, [Fixed(HpTaken, 30, steps: 2, perLevel: false)], Monster(100, level: 42, id: 9002));

            World.Clock.Advance(Duration.FromSeconds(3));
            await _buffs.TickAsync(victim);
            Assert.AreEqual(5000, victim.Hp);

            World.Clock.Advance(Duration.FromSeconds(1));
            await _buffs.TickAsync(victim);
            Assert.AreEqual(4970, victim.Hp);
        }

        [TestMethod]
        public async Task BleedingStopsAtOneHp()
        {
            var victim = Monster(5000);
            victim.Hp = 100;
            await _buffs.ApplyAsync(victim, Bleeding, [Fixed(HpTaken, 4, steps: 1, perLevel: true)], Monster(100, level: 42, id: 9002));
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(victim);

            Assert.AreEqual(1, victim.Hp);
            Assert.IsTrue(victim.IsAlive);
            Assert.AreEqual(99, map.LastPackets.OfType<DmPacket>().Single().Damages);
        }

        [TestMethod]
        public async Task RegenerationGivesHpUpToTheMaximumAndShowsWhatItGave()
        {
            var healed = Monster(5000);
            healed.Hp = 4900;
            await _buffs.ApplyAsync(healed, Bleeding, [Fixed(HpGiven, 3, steps: 1, perLevel: true)], Monster(100, level: 42, id: 9002));
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(healed);

            Assert.AreEqual(5000, healed.Hp);
            Assert.AreEqual(100, map.LastPackets.OfType<RcPacket>().Single().HealAmount);
        }

        [TestMethod]
        public async Task MpIsTakenAndGivenTheSameWay()
        {
            var character = _session.Character;
            character.Mp = character.MaxMp;
            await _buffs.ApplyAsync(character, Bleeding, [Fixed(MpTaken, 2, steps: 1, perLevel: true)], Monster(100, level: 10, id: 9002));

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxMp - 20, character.Mp);

            await _buffs.ApplyAsync(character, Bleeding, [Fixed(MpGiven, 1, steps: 1, perLevel: true)], Monster(100, level: 10, id: 9003));
            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);
            Assert.AreEqual(character.MaxMp - 10, character.Mp);
        }

        [TestMethod]
        public async Task ACardNobodyInflictedScalesWithItsBearersLevel()
        {
            var character = _session.Character;
            character.Level = 30;
            character.Hp = character.MaxHp;
            await _buffs.ApplyPermanentAsync(character, Heat, [Fixed(HpTaken, 2, steps: 1, perLevel: true)]);

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(character);

            Assert.AreEqual(character.MaxHp - 60, character.Hp);
        }

        [TestMethod]
        public async Task AFixedLossAndAPercentLossAddUp()
        {
            var victim = Monster(1000);
            await _buffs.ApplyAsync(victim, Bleeding,
                [Fixed(HpTaken, 50, steps: 1, perLevel: false), HpReduced(10, 1)], null);

            World.Clock.Advance(Duration.FromSeconds(2));
            await _buffs.TickAsync(victim);

            Assert.AreEqual(850, victim.Hp);
        }

        private const byte HpGiven = 11, HpTaken = 12, MpGiven = 21, MpTaken = 22;

        private static BCardDto Fixed(byte subType, int value, int steps, bool perLevel) => new()
        {
            Type = (byte)BCardType.CardType.HealingBurningAndCasting, SubType = subType, FirstData = value,
            ThirdData = steps, IsLevelScaled = perLevel,
        };

        private MonsterComponentBundle Monster(int maxHp, byte level = 1, int id = 9001)
        {
            var map = _session.Character.MapInstance;
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = level, MaxHp = maxHp, MaxMp = 10 };
            return new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(id, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
        }

        private static BCardDto HpReduced(int percent, int steps) => new()
        {
            Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 12, FirstData = percent, ThirdData = steps,
        };
    }
}
