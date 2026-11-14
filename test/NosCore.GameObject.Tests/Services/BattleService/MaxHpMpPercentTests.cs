//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MaxHpMpPercentTests
    {
        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private VitalityService _vitality;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(_buffs);
            var gear = new Mock<IEquipmentStatsService>();
            gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            World.Inject(gear.Object);
            var passives = new Mock<IPassiveSkillService>();
            passives.Setup(p => p.GetBCards(It.IsAny<PlayerComponentBundle>())).Returns([]);
            World.Inject(passives.Object);
            _vitality = World.Create<VitalityService>();
            _vitality.Refresh(_session.Character);
        }

        [TestMethod]
        public async Task ABuffRaisingTheMaximumHpAndMpByAPercentRaisesBoth()
        {
            var (hp, mp) = (_session.Character.MaxHp, _session.Character.MaxMp);
            await _buffs.ApplyPermanentAsync(_session.Character, new CardDto { CardId = 1539 },
            [
                new BCardDto { Type = 32, SubType = 41, FirstData = 25, CardId = 1539 },
                new BCardDto { Type = 32, SubType = 51, FirstData = 10, CardId = 1539 },
            ]);

            _vitality.Refresh(_session.Character);

            Assert.AreEqual(hp + hp * 25 / 100, _session.Character.MaxHp);
            Assert.AreEqual(mp + mp * 10 / 100, _session.Character.MaxMp);
        }

        [TestMethod]
        public async Task ABuffLoweringTheMaximumHpByAPercentLowersIt()
        {
            var hp = _session.Character.MaxHp;
            await _buffs.ApplyPermanentAsync(_session.Character, new CardDto { CardId = 2 },
                [new BCardDto { Type = 32, SubType = 42, FirstData = 20, CardId = 2 }]);

            _vitality.Refresh(_session.Character);

            Assert.AreEqual(hp - hp * 20 / 100, _session.Character.MaxHp);
        }
    }
}
