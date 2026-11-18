//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class OnHitBuffTests
    {
        private TestWorld World;
        private ClientSession _attacker;
        private ClientSession _defender;
        private Mock<IBuffService> _buffs;
        private Mock<IEquipmentStatsService> _gear;
        private Mock<INpcCombatCatalog> _catalog;
        private OnHitBuffService _service;
        private const short ThornwalkerVNum = 67;
        private const short Blackout = 7;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _attacker = await World.CreateSessionAsync();
            _defender = await World.CreateSessionAsync();
            _buffs = World.Freeze<Mock<IBuffService>>();
            _buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            _gear = World.Freeze<Mock<IEquipmentStatsService>>();
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _catalog = World.Freeze<Mock<INpcCombatCatalog>>();
            _catalog.Setup(c => c.GetNpcBCards(It.IsAny<short>())).Returns(System.Array.Empty<BCardDto>());
            _service = World.Create<OnHitBuffService>();
        }

        [TestMethod]
        public async Task AttackerGearCanInflictACardOnTheOpponent()
        {
            Wear(_attacker, BCardEffect.ChanceCausingXBuffOnEnemyOnAttack, 3, 4424);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false, []);

            VerifyInflicted(_defender, _attacker, 3, 4424);
        }

        [TestMethod]
        public async Task AttackerGearCanGiveTheAttackerACard()
        {
            Wear(_attacker, BCardEffect.ChanceReceivingXBuffOnAttack, 8, 4426);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false, []);

            VerifyInflicted(_attacker, _attacker, 8, 4426);
        }

        [TestMethod]
        public async Task DefenderGearCanInflictACardOnTheAttacker()
        {
            Wear(_defender, BCardEffect.ChanceCausingXBuffOnEnemyOnDefence, 5, 100);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false, []);

            VerifyInflicted(_attacker, _defender, 5, 100);
        }

        [TestMethod]
        public async Task DefenderGearCanGiveTheDefenderACard()
        {
            Wear(_defender, BCardEffect.ChanceReceivingXBuffOnDefence, 5, 101);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false, []);

            VerifyInflicted(_defender, _defender, 5, 101);
        }

        [DataTestMethod]
        [DataRow(BCardEffect.SecondSpCardCastBuffOnAttacker)]
        [DataRow(BCardEffect.SecondSpCardChanceCausingXBuffOnAttacker)]
        public async Task ADefenderBuffCanInflictACardOnTheAttacker(BCardEffect effect)
        {
            var id = _defender.Character.VisualId;
            _buffs.Setup(b => b.GetActiveBuffs(It.Is<IAliveEntity>(e => e.VisualId == id))).Returns(new List<BuffInstance>
            {
                new(144, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                    [new BCardDto { Type = effect.Type(), SubType = effect.SubType(), FirstData = 100, SecondData = 372 }]),
            });

            await _service.TriggerAsync(_attacker.Character, _defender.Character, true, []);

            VerifyInflicted(_attacker, _defender, 100, 372);
        }

        [TestMethod]
        public async Task AKilledOpponentGetsNothing()
        {
            Wear(_attacker, BCardEffect.ChanceCausingXBuffOnEnemyOnAttack, 3, 4424);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, true, []);

            _buffs.Verify(b => b.InflictCardsAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                It.IsAny<IReadOnlyList<BCardDto>>()), Times.Never);
        }

        [TestMethod]
        public async Task AMonsterPutsItsOwnCardOnWhatItHits()
        {
            var monster = AThornwalkerCarrying(MonsterCard(BCardRecipient.Enemy));

            await _service.TriggerAsync(monster, _defender.Character, false, []);

            VerifyMonsterInflicted(monster, Times.Once());
        }

        [TestMethod]
        public async Task AMonsterCardMeantForAnotherRecipientStaysOff()
        {
            var monster = AThornwalkerCarrying(MonsterCard(BCardRecipient.Caster));

            await _service.TriggerAsync(monster, _defender.Character, false, []);

            VerifyMonsterInflicted(monster, Times.Never());
        }

        [TestMethod]
        public async Task AMonsterCardOnItsDeathSlotStaysOff()
        {
            var monster = AThornwalkerCarrying(MonsterCard(BCardRecipient.Enemy, slot: 2));

            await _service.TriggerAsync(monster, _defender.Character, false, []);

            VerifyMonsterInflicted(monster, Times.Never());
        }

        [TestMethod]
        public async Task AMonsterCardTheSkillAlreadyCarriesIsNotRolledTwice()
        {
            var monster = AThornwalkerCarrying(MonsterCard(BCardRecipient.Enemy));

            await _service.TriggerAsync(monster, _defender.Character, false,
                [new BCardDto { Type = BCardEffect.BuffChanceCausing.Type(), SubType = BCardEffect.BuffChanceCausing.SubType(), FirstData = 50, SecondData = Blackout }]);

            VerifyMonsterInflicted(monster, Times.Never());
        }

        [TestMethod]
        public async Task AMonsterPutsNothingOnWhatItKilled()
        {
            var monster = AThornwalkerCarrying(MonsterCard(BCardRecipient.Enemy));

            await _service.TriggerAsync(monster, _defender.Character, true, []);

            VerifyMonsterInflicted(monster, Times.Never());
        }

        private static BCardDto MonsterCard(BCardRecipient recipient, byte slot = 0) => new()
        {
            NpcMonsterVNum = ThornwalkerVNum,
            Type = BCardEffect.BuffChanceCausing.Type(),
            SubType = BCardEffect.BuffChanceCausing.SubType(),
            FirstData = 6,
            SecondData = Blackout,
            Recipient = recipient,
            Slot = slot,
        };

        private IAliveEntity AThornwalkerCarrying(BCardDto card)
        {
            _catalog.Setup(c => c.GetNpcBCards(ThornwalkerVNum)).Returns([card]);
            return _defender.Character.MapInstance.SummonMonster(
                new NpcMonsterDto { NpcMonsterVNum = ThornwalkerVNum, MaxHp = 1000 }, 1, 1, true);
        }

        private void VerifyMonsterInflicted(IAliveEntity monster, Times times)
        {
            var targetId = _defender.Character.VisualId;
            var monsterId = monster.VisualId;
            _buffs.Verify(b => b.InflictCardsAsync(
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Player && e.VisualId == targetId),
                It.Is<IAliveEntity>(e => e.VisualType == VisualType.Monster && e.VisualId == monsterId),
                It.Is<IReadOnlyList<BCardDto>>(cards => cards.Count == 1 && cards[0].Effect() == BCardEffect.BuffChanceCausing
                    && cards[0].FirstData == 6 && cards[0].SecondData == Blackout)), times);
        }

        private void Wear(ClientSession wearer, BCardEffect effect, short chance, int cardId)
        {
            var id = wearer.Character.VisualId;
            _gear.Setup(g => g.Resolve(It.Is<IAliveEntity>(e => e.VisualId == id))).Returns(EquipmentStats.None with
            {
                BCards = [new BCardDto { Type = effect.Type(), SubType = effect.SubType(), FirstData = chance, SecondData = cardId }]
            });
        }

        private void VerifyInflicted(ClientSession target, ClientSession caster, short chance, int cardId)
        {
            var targetId = target.Character.VisualId;
            var casterId = caster.Character.VisualId;
            _buffs.Verify(b => b.InflictCardsAsync(
                It.Is<IAliveEntity>(e => e.VisualId == targetId),
                It.Is<IAliveEntity>(e => e.VisualId == casterId),
                It.Is<IReadOnlyList<BCardDto>>(cards => cards.Single().Effect() == BCardEffect.BuffChanceCausing
                    && cards.Single().FirstData == chance && cards.Single().SecondData == cardId)), Times.Once);
        }
    }
}
