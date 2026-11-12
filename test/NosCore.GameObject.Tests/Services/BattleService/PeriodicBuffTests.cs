//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class PeriodicBuffTests
    {
        private static readonly CardDto Heat = new() { CardId = 339, Duration = 0 };

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

        private static BCardDto HpReduced(int percent, int steps) => new()
        {
            Type = (byte)BCardType.CardType.RecoveryAndDamagePercent, SubType = 12, FirstData = percent, ThirdData = steps,
        };
    }
}
