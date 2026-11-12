//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.MpService;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.SkillService;
using NosCore.GameObject.Tests.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    [TestClass]
    public class PassiveSkillBonusTests
    {
        private TestWorld World;
        private ClientSession _session;
        private PassiveSkillService _passives;
        private Mock<IBuffService> _buffs;
        private Mock<IEquipmentStatsService> _equipment;

        private static readonly SkillDto LifeExtension = new() { SkillVNum = 36, SkillType = 0, CastId = 4 };
        private static readonly SkillDto SelfDefence = new() { SkillVNum = 48, SkillType = 0, CastId = 7 };
        private static readonly SkillDto Slash = new() { SkillVNum = 220, SkillType = 1, CastId = 0, Class = 1 };

        private static BCardDto Card(short skillVNum, BCardEffect effect, short amount) => new()
        {
            SkillVNum = skillVNum, Type = effect.Type(), SubType = effect.SubType(), FirstData = amount
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            var catalog = new Mock<INpcCombatCatalog>();
            catalog.Setup(c => c.GetSkillBCards(It.IsAny<short>())).Returns(Array.Empty<BCardDto>());
            catalog.Setup(c => c.GetSkillBCards(36)).Returns(new[] { Card(36, BCardEffect.MaxHpmpMaximumHpIncreased, 50) });
            catalog.Setup(c => c.GetSkillBCards(48)).Returns(new[] { Card(48, BCardEffect.DefenceAllIncreased, 3) });
            catalog.Setup(c => c.GetSkillBCards(220)).Returns(new[] { Card(220, BCardEffect.DefenceAllIncreased, 99) });
            _passives = new PassiveSkillService(catalog.Object);

            _buffs = new Mock<IBuffService>();
            _buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            _equipment = new Mock<IEquipmentStatsService>();
            _equipment.Setup(e => e.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
        }

        private void Know(SkillDto skill) =>
            _session.Character.Skills[skill.SkillVNum] = new CharacterSkill { SkillVNum = skill.SkillVNum, Skill = skill };

        [TestMethod]
        public void OnlyKnownPassivesCount()
        {
            Know(SelfDefence);
            Know(Slash);

            CollectionAssert.AreEqual(new short?[] { 48 }, _passives.GetBCards(_session.Character).Select(c => c.SkillVNum).ToList());
        }

        [TestMethod]
        public void ADefenceCourseRaisesEveryDefence()
        {
            var provider = BattleStats.Provider(_buffs.Object, _equipment.Object, _passives);
            var before = provider.GetStats(_session.Character);

            Know(SelfDefence);
            var after = provider.GetStats(_session.Character);

            Assert.AreEqual(before.Defence + 3, after.Defence);
            Assert.AreEqual(before.DistanceDefence + 3, after.DistanceDefence);
            Assert.AreEqual(before.MagicDefence + 3, after.MagicDefence);
        }

        [TestMethod]
        public void ALifeCourseRaisesTheMaximumHp()
        {
            var vitality = new VitalityService(new HpService(), new MpService(), _equipment.Object, _buffs.Object, _passives);
            vitality.Refresh(_session.Character);
            var before = _session.Character.MaxHp;

            Know(LifeExtension);
            vitality.Refresh(_session.Character);

            Assert.AreEqual(before + 50, _session.Character.MaxHp);
        }
    }
}
