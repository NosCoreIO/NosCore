//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillCastServiceTests
    {
        private TestWorld World;
        private ClientSession _session;
        private BuffService _buffs;
        private readonly Mock<IEquipmentStatsService> _gear = new();
        private SkillCastService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Mp = 1000;
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);
            _buffs = World.Create<BuffService>();
            _service = new SkillCastService(World.Clock, _gear.Object, _buffs);
        }

        [TestMethod]
        public void WithoutBonusesTheCooldownIsTheSkillsOwn()
        {
            Assert.AreEqual(Duration.FromSeconds(10), _service.Cooldown(_session.Character, 100));
        }

        [TestMethod]
        public async Task ABuffThatShortensCooldownsShortensIt()
        {
            await _buffs.ApplyPermanentAsync(_session.Character, new CardDto { CardId = 1 },
                [new BCardDto { Type = 17, SubType = 42, FirstData = 20, CardId = 1 }]);

            Assert.AreEqual(Duration.FromSeconds(8), _service.Cooldown(_session.Character, 100));
        }

        [TestMethod]
        public void WornGearThatLengthensCooldownsLengthensIt()
        {
            WearBCards(new BCardDto { Type = 17, SubType = 41, FirstData = 50 });

            Assert.AreEqual(Duration.FromSeconds(15), _service.Cooldown(_session.Character, 100));
        }

        [TestMethod]
        public void AShortenedCooldownLetsTheSkillBeCastSooner()
        {
            WearBCards(new BCardDto { Type = 17, SubType = 42, FirstData = 50 });
            var skill = Skill(mpCost: 0);
            _service.Spend(_session.Character, skill, 0);

            World.Clock.Advance(Duration.FromSeconds(6));

            Assert.AreEqual(SkillCastRefusal.None, _service.Check(_session.Character, skill, out _));
        }

        [TestMethod]
        public void AManaCostIncreaseRaisesTheMpSpent()
        {
            WearBCards(new BCardDto { Type = 18, SubType = 21, FirstData = 50 });

            _service.Check(_session.Character, Skill(mpCost: 20), out var mpCost);

            Assert.AreEqual(30, mpCost);
        }

        [TestMethod]
        public void AManaCostDecreaseLowersTheMpSpent()
        {
            WearBCards(new BCardDto { Type = 18, SubType = 22, FirstData = 25 });

            _service.Check(_session.Character, Skill(mpCost: 20), out var mpCost);

            Assert.AreEqual(15, mpCost);
        }

        private void WearBCards(params BCardDto[] bCards) =>
            _gear.Setup(g => g.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None with { BCards = bCards });

        private CharacterSkill Skill(short mpCost) => new()
        {
            CharacterId = _session.Character.CharacterId,
            SkillVNum = 1,
            Skill = new SkillDto { SkillVNum = 1, CastId = 1, MpCost = mpCost, Cooldown = 100 },
        };
    }
}
