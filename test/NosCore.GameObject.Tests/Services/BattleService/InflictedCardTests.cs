//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    // Type 25 is the most widespread effect in the game - 1344 skills declare one - and nothing
    // read it. These defend the reading of the two fields, because getting them the wrong way
    // round raises nothing: FirstData is a valid card id 1278 times out of 1341, so a server that
    // swapped them would apply a real, wrong card at a real, wrong probability and never
    // complain.
    [TestClass]
    public class InflictedCardTests
    {
        // Star Attack: 60% of card 7, "Blackout".
        private const short StunCardId = 7;

        private InflictedCardService _service = null!;
        private Mock<ICardCatalog> _catalog = null!;
        private Mock<IBuffService> _buffs = null!;
        private Mock<IRandomProvider> _random = null!;
        private IAliveEntity _target = null!;
        private IAliveEntity _caster = null!;

        private static readonly CardDto StunCard = new() { CardId = StunCardId, Duration = 30 };

        private static readonly List<BCardDto> StunCardEffects = new()
        {
            new BCardDto { CardId = StunCardId, Type = (byte)BCardType.CardType.SpecialActions, FirstData = 1 }
        };

        private static BCardDto Declares(AdditionalTypes.Buff subType, int percent, short cardId) =>
            new()
            {
                Type = (byte)BCardType.CardType.Buff,
                SubType = (byte)subType,
                FirstData = (short)percent,
                SecondData = (short)cardId
            };

        [TestInitialize]
        public void Setup()
        {
            _catalog = new Mock<ICardCatalog>();
            _catalog.Setup(c => c.GetCard(StunCardId)).Returns(StunCard);
            _catalog.Setup(c => c.GetCardBCards(StunCardId)).Returns(StunCardEffects);

            _buffs = new Mock<IBuffService>();
            _random = new Mock<IRandomProvider>();
            _target = new Mock<IAliveEntity>().Object;
            _caster = new Mock<IAliveEntity>().Object;

            _service = new InflictedCardService(_catalog.Object, _buffs.Object, _random.Object);
        }

        private void RollGives(int value) => _random.Setup(r => r.Next(0, 100)).Returns(value);

        [TestMethod]
        public async Task TheCardTheSkillNamesIsTheOneApplied()
        {
            RollGives(0);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceCausing, 60, StunCardId) });

            // The card, and its own effects - not the skill's. Applying the skill's BCards here
            // would look right and give the wrong effect.
            _buffs.Verify(b => b.ApplyAsync(_target, StunCard, StunCardEffects, _caster, -1), Times.Once);
        }

        [TestMethod]
        public async Task AFailedRollAppliesNothing()
        {
            RollGives(60);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceCausing, 60, StunCardId) });

            _buffs.Verify(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<CardDto>(),
                It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }

        // 717 of the 1341 declarations say 100, so an off-by-one on the comparison would silently
        // drop the most common case of all: Next(0,100) can return 99 and never 100.
        [TestMethod]
        public async Task AHundredPercentAlwaysLands()
        {
            RollGives(99);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceCausing, 100, StunCardId) });

            _buffs.Verify(b => b.ApplyAsync(_target, StunCard, StunCardEffects, _caster, -1), Times.Once);
        }

        [TestMethod]
        public async Task ZeroPercentNeverLands()
        {
            RollGives(0);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceCausing, 0, StunCardId) });

            _buffs.Verify(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<CardDto>(),
                It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }

        // The sign of the value in the file selects 11 or 12, and 12 is the opposite action.
        // Treating them alike would apply the card a skill is meant to strip off.
        [TestMethod]
        public async Task TheRemovingSubtypeRemovesInsteadOfApplying()
        {
            RollGives(0);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceRemoving, 100, StunCardId) });

            _buffs.Verify(b => b.RemoveAsync(_target, StunCardId), Times.Once);
            _buffs.Verify(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<CardDto>(),
                It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task OtherEffectTypesAreLeftAlone()
        {
            RollGives(0);

            await _service.InflictAsync(_target, _caster, new[]
            {
                new BCardDto { Type = (byte)BCardType.CardType.AttackPower, SubType = 11, FirstData = 50 }
            });

            _buffs.Verify(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<CardDto>(),
                It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }

        // One of the 1341 ids is not in Card.dat. A blow must not fail over a bad row.
        [TestMethod]
        public async Task ACardTheFileDoesNotHaveIsSkipped()
        {
            RollGives(0);
            _catalog.Setup(c => c.GetCard(It.Is<short>(v => v != StunCardId))).Returns((CardDto?)null);

            await _service.InflictAsync(_target, _caster,
                new[] { Declares(AdditionalTypes.Buff.ChanceCausing, 100, 9999) });

            _buffs.Verify(b => b.ApplyAsync(It.IsAny<IAliveEntity>(), It.IsAny<CardDto>(),
                It.IsAny<IReadOnlyList<BCardDto>>(), It.IsAny<IAliveEntity>(), It.IsAny<int>()), Times.Never);
        }
    }
}
