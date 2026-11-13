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
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class PeriodicBuffTests
    {
        private static readonly CardDto Heat = new() { CardId = 339, Duration = 0 };
        private static readonly CardDto Poison = new() { CardId = 455, Duration = 600 };

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

        private MonsterComponentBundle Monster(int maxHp)
        {
            var map = _session.Character.MapInstance;
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = 1, MaxHp = maxHp, MaxMp = 10 };
            return new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9001, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
        }

        private static BCardDto HpReduced(int percent, int steps) => new()
        {
            Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 12, FirstData = percent, ThirdData = steps,
        };
    }
}
