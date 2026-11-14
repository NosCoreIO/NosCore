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
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
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
        private OnHitBuffService _service;

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
            _service = World.Create<OnHitBuffService>();
        }

        [TestMethod]
        public async Task AttackerGearCanInflictACardOnTheOpponent()
        {
            Wear(_attacker, BCardEffect.Type95ChanceCausingXBuffOnEnemyOnAttack, 3, 4424);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false);

            VerifyInflicted(_defender, _attacker, 3, 4424);
        }

        [TestMethod]
        public async Task AttackerGearCanGiveTheAttackerACard()
        {
            Wear(_attacker, BCardEffect.Type95ChanceReceivingXBuffOnAttack, 8, 4426);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false);

            VerifyInflicted(_attacker, _attacker, 8, 4426);
        }

        [TestMethod]
        public async Task DefenderGearCanInflictACardOnTheAttacker()
        {
            Wear(_defender, BCardEffect.Type95ChanceCausingXBuffOnEnemyOnDefence, 5, 100);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false);

            VerifyInflicted(_attacker, _defender, 5, 100);
        }

        [TestMethod]
        public async Task DefenderGearCanGiveTheDefenderACard()
        {
            Wear(_defender, BCardEffect.Type95ChanceReceivingXBuffOnDefence, 5, 101);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, false);

            VerifyInflicted(_defender, _defender, 5, 101);
        }

        [TestMethod]
        public async Task AKilledOpponentGetsNothing()
        {
            Wear(_attacker, BCardEffect.Type95ChanceCausingXBuffOnEnemyOnAttack, 3, 4424);

            await _service.TriggerAsync(_attacker.Character, _defender.Character, true);

            _buffs.Verify(b => b.InflictCardsAsync(It.IsAny<IAliveEntity>(), It.IsAny<IAliveEntity>(),
                It.IsAny<IReadOnlyList<BCardDto>>()), Times.Never);
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
