//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NodaTime.Testing;
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
    public class TimeoutBuffTests
    {
        private const short FirstCardId = 136;
        private const short FollowUpCardId = 29;

        private static readonly CardDto FirstCard = new()
        {
            CardId = FirstCardId, Duration = 100, TimeoutBuff = FollowUpCardId, TimeoutBuffChance = 30
        };

        private static readonly CardDto FollowUpCard = new() { CardId = FollowUpCardId, Duration = 1200 };

        private FakeClock _clock = null!;
        private Mock<IRandomProvider> _random = null!;
        private BuffService _service = null!;
        private MapWorld _world = null!;
        private IAliveEntity _target = null!;

        [TestInitialize]
        public void Setup()
        {
            _clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0));
            var catalog = new Mock<ICardCatalog>();
            catalog.Setup(c => c.GetCard(FirstCardId)).Returns(FirstCard);
            catalog.Setup(c => c.GetCard(FollowUpCardId)).Returns(FollowUpCard);
            catalog.Setup(c => c.GetCardBCards(It.IsAny<short>())).Returns(Array.Empty<BCardDto>());
            _random = new Mock<IRandomProvider>();
            _world = new MapWorld();
            _target = new MonsterComponentBundle(
                _world.Exclusive(w => w.Create(
                    new BuffStateComponent(new ConcurrentDictionary<short, BuffInstance>()),
                    new NpcStateComponent(null!, null!, null!, null!, null, null, null, null!, null, false))),
                _world);
            _service = new BuffService(_clock, catalog.Object, _random.Object);
        }

        private void RollGives(int value) => _random.Setup(r => r.Next(0, 100)).Returns(value);

        [TestMethod]
        public async Task AnExpiredCardHandsOverToItsFollowUp()
        {
            RollGives(29);
            await _service.ApplyAsync(_target, FirstCard, Array.Empty<BCardDto>(), null);

            _clock.Advance(Duration.FromSeconds(10));
            await _service.TickAsync(_target);

            Assert.IsFalse(_service.HasBuff(_target, FirstCardId));
            Assert.IsTrue(_service.HasBuff(_target, FollowUpCardId));
        }

        [TestMethod]
        public async Task AFailedRollLeavesNoFollowUp()
        {
            RollGives(30);
            await _service.ApplyAsync(_target, FirstCard, Array.Empty<BCardDto>(), null);

            _clock.Advance(Duration.FromSeconds(10));
            await _service.TickAsync(_target);

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }

        [TestMethod]
        public async Task TheFollowUpWaitsForTheCardToRunOut()
        {
            RollGives(0);
            await _service.ApplyAsync(_target, FirstCard, Array.Empty<BCardDto>(), null);

            _clock.Advance(Duration.FromSeconds(9));
            await _service.TickAsync(_target);

            Assert.IsTrue(_service.HasBuff(_target, FirstCardId));
            Assert.IsFalse(_service.HasBuff(_target, FollowUpCardId));
        }

        [TestMethod]
        public async Task ASkillBuffSharingACardIdHasNoFollowUp()
        {
            RollGives(0);
            await _service.ApplySkillBuffAsync(_target, FirstCardId, 10,
                new[] { new BCardDto { Type = (byte)NosCore.Data.Enumerations.Buff.BCardType.CardType.AttackPower } }, null);

            _clock.Advance(Duration.FromSeconds(1));
            await _service.TickAsync(_target);

            Assert.AreEqual(0, _service.GetActiveBuffs(_target).Count);
        }
    }
}
