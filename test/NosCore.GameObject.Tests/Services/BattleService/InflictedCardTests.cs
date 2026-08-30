//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Components;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class InflictedCardTests
    {
        private const short StunCardId = 7;

        private BuffService _service = null!;
        private Mock<ICardCatalog> _catalog = null!;
        private Mock<IRandomProvider> _random = null!;
        private MapWorld _world = null!;
        private IAliveEntity _target = null!;
        private IAliveEntity _caster = null!;

        private static readonly CardDto StunCard = new() { CardId = StunCardId, Duration = 30 };

        private static readonly List<BCardDto> StunCardEffects = new()
        {
            new BCardDto { CardId = StunCardId, Type = (byte)BCardType.CardType.SpecialActions, FirstData = 1 }
        };

        private static BCardDto Declares(BCardEffect effect, int percent, short cardId) =>
            new()
            {
                Type = effect.Type(),
                SubType = effect.SubType(),
                FirstData = (short)percent,
                SecondData = cardId
            };

        [TestInitialize]
        public void Setup()
        {
            _catalog = new Mock<ICardCatalog>();
            _catalog.Setup(c => c.GetCard(StunCardId)).Returns(StunCard);
            _catalog.Setup(c => c.GetCardBCards(StunCardId)).Returns(StunCardEffects);

            _random = new Mock<IRandomProvider>();
            _world = new MapWorld();
            _target = Entity();
            _caster = Entity();

            _service = new BuffService(new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0)),
                _catalog.Object, _random.Object);
        }

        // A bundle carrying only the two components the buff path reads. MapInstance comes off
        // NpcStateComponent and is left null, so ApplyAsync stores the buff and sends no packet.
        private IAliveEntity Entity() =>
            new MonsterComponentBundle(
                _world.World.Create(
                    new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
                    new NpcStateComponent(null!, null!, null!, null!, null, null, null!, null, false)),
                _world);

        private void RollGives(int value) => _random.Setup(r => r.Next(0, 100)).Returns(value);

        private Task Inflict(params BCardDto[] declared) =>
            _service.InflictCardsAsync(_target, _caster, declared);

        [TestMethod]
        public async Task TheCardTheSkillNamesIsTheOneApplied()
        {
            RollGives(0);

            await Inflict(Declares(BCardEffect.BuffChanceCausing, 60, StunCardId));

            var buff = _service.GetActiveBuffs(_target);
            Assert.AreEqual(1, buff.Count);
            Assert.AreEqual(StunCardId, System.Linq.Enumerable.First(buff).CardId);
            CollectionAssert.AreEqual(StunCardEffects,
                (System.Collections.ICollection)System.Linq.Enumerable.First(buff).BCards);
            Assert.AreSame(_caster, System.Linq.Enumerable.First(buff).Caster);
        }

        [TestMethod]
        public async Task AFailedRollAppliesNothing()
        {
            RollGives(60);

            await Inflict(Declares(BCardEffect.BuffChanceCausing, 60, StunCardId));

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }

        [TestMethod]
        public async Task AHundredPercentAlwaysLands()
        {
            RollGives(99);

            await Inflict(Declares(BCardEffect.BuffChanceCausing, 100, StunCardId));

            Assert.IsTrue(_service.HasBuff(_target, StunCardId));
        }

        [TestMethod]
        public async Task ZeroPercentNeverLands()
        {
            RollGives(0);

            await Inflict(Declares(BCardEffect.BuffChanceCausing, 0, StunCardId));

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }

        [TestMethod]
        public async Task TheRemovingSubtypeRemovesInsteadOfApplying()
        {
            RollGives(0);
            await _service.ApplyAsync(_target, StunCard, StunCardEffects, _caster);
            Assert.IsTrue(_service.HasBuff(_target, StunCardId));

            await Inflict(Declares(BCardEffect.BuffChanceRemoving, 100, StunCardId));

            Assert.IsFalse(_service.HasBuff(_target, StunCardId));
        }

        [TestMethod]
        public async Task OtherEffectTypesAreLeftAlone()
        {
            RollGives(0);

            await Inflict(new BCardDto
            {
                Type = (byte)BCardType.CardType.AttackPower, SubType = 11, FirstData = 50
            });

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }

        [TestMethod]
        public async Task ACardTheFileDoesNotHaveIsSkipped()
        {
            RollGives(0);
            _catalog.Setup(c => c.GetCard(It.Is<short>(v => v != StunCardId))).Returns((CardDto?)null);

            await Inflict(Declares(BCardEffect.BuffChanceCausing, 100, 9999));

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }
    }
}
