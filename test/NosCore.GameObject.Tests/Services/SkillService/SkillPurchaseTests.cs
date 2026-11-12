//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.BDD.Steps;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    [TestClass]
    public class SkillPurchaseTests
    {
        private TestWorld World;
        private NosCore.GameObject.Services.SkillService.SkillService _service;
        private ClientSession _session;
        private RememberingSkillDao _dao;
        private Mock<IVitalityService> Vitality;

        private static readonly SkillDto Beginner = new()
            { SkillVNum = 20, SkillType = 0, CastId = 0, Price = 480, MinimumAdventurerLevel = 4 };
        private static readonly SkillDto Advanced = new()
            { SkillVNum = 21, SkillType = 0, CastId = 0, Price = 22500, MinimumAdventurerLevel = 25 };
        private static readonly SkillDto SwordsmanOnly = new()
            { SkillVNum = 70, SkillType = 0, CastId = 3, Price = 100, MinimumSwordmanLevel = 10 };
        private static readonly SkillDto PvpDamageBoost = new()
            { SkillVNum = 88, SkillType = 0, CastId = 19, Price = 500, MinimumAdventurerLevel = 10, MinimumLevelIsChampionLevel = true };
        private static readonly SkillDto Slash = new()
            { SkillVNum = 220, SkillType = 1, Class = 1, CastId = 0, LevelMinimum = 0 };
        private static readonly SkillDto StrongSlash = new()
            { SkillVNum = 310, SkillType = 2, Class = 1, CastId = 0, UpgradeSkill = 220, LevelMinimum = 10, Price = 11700 };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Class = CharacterClassType.Swordsman;
            _session.Character.Level = 30;
            _session.Character.JobLevel = 20;
            _session.Character.Gold = 100_000;
            _dao = new RememberingSkillDao();
            Vitality = new Mock<IVitalityService>();
            _service = new NosCore.GameObject.Services.SkillService.SkillService(_dao,
                new List<SkillDto> { Beginner, Advanced, SwordsmanOnly, PvpDamageBoost, Slash, StrongSlash },
                Vitality.Object, new Mock<IBattleStatsProvider>().Object,
                new NosCore.Algorithm.ExperienceService.ExperienceService(), new NosCore.Algorithm.JobExperienceService.JobExperienceService(),
                new NosCore.Algorithm.HeroExperienceService.HeroExperienceService(), new NosCore.Algorithm.SkillPointService.SkillPointService());
        }

        private void Know(SkillDto skill) =>
            _session.Character.Skills[skill.SkillVNum] = new CharacterSkill { SkillVNum = skill.SkillVNum, Skill = skill };

        private Game18NConstString? LastMessage() => _session.GetLastPacket<MsgiPacket>()?.Message;

        [TestMethod]
        public async Task APassiveIsLearnedAndPaidFor()
        {
            await _service.BuyAsync(_session.Character, Beginner);

            Assert.IsTrue(_session.Character.Skills.ContainsKey(20));
            Assert.AreEqual(100_000 - 480, _session.Character.Gold);
            Assert.AreEqual(1, _dao.Rows.Count(r => r.SkillVNum == 20));
            Assert.AreEqual(Game18NConstString.SkillLearned, LastMessage());
        }

        [TestMethod]
        public async Task ALearnedPassiveTakesEffectAtOnce()
        {
            await _service.BuyAsync(_session.Character, Beginner);

            Vitality.Verify(v => v.RefreshAndNotifyAsync(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>()), Times.Once);
        }

        [TestMethod]
        public async Task ARefusedPassiveChangesNoStat()
        {
            _session.Character.Gold = 0;

            await _service.BuyAsync(_session.Character, Beginner);

            Vitality.Verify(v => v.RefreshAndNotifyAsync(It.IsAny<NosCore.GameObject.Ecs.PlayerComponentBundle>()), Times.Never);
        }

        [TestMethod]
        public async Task AStrongerPassiveReplacesTheWeakerOfItsKind()
        {
            await _service.BuyAsync(_session.Character, Beginner);
            await _service.BuyAsync(_session.Character, Advanced);

            CollectionAssert.AreEqual(new short[] { 21 }, _session.Character.Skills.Keys.Where(k => k < 200).ToList());
            CollectionAssert.AreEqual(new short[] { 21 }, _dao.Rows.Select(r => r.SkillVNum).ToList());
        }

        [TestMethod]
        public async Task AWeakerPassiveIsRefusedOnceTheStrongerIsKnown()
        {
            Know(Advanced);

            await _service.BuyAsync(_session.Character, Beginner);

            Assert.IsFalse(_session.Character.Skills.ContainsKey(20));
            Assert.AreEqual(Game18NConstString.YouAlreadyKnowABetterSkill, LastMessage());
        }

        [TestMethod]
        public async Task APassiveNeedsItsLevel()
        {
            _session.Character.Level = 24;

            await _service.BuyAsync(_session.Character, Advanced);

            Assert.IsFalse(_session.Character.Skills.ContainsKey(21));
            Assert.AreEqual(100_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.YouCanNotLearnThat, LastMessage());
        }

        [TestMethod]
        public async Task AChampionPassiveNeedsTheChampionLevel()
        {
            _session.Character.HeroLevel = 9;

            await _service.BuyAsync(_session.Character, PvpDamageBoost);

            Assert.IsFalse(_session.Character.Skills.ContainsKey(88));
            Assert.AreEqual(100_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.YouCanNotLearnThat, LastMessage());
        }

        [TestMethod]
        public async Task AChampionPassiveIsLearnedAtTheChampionLevel()
        {
            _session.Character.Level = 1;
            _session.Character.HeroLevel = 10;

            await _service.BuyAsync(_session.Character, PvpDamageBoost);

            Assert.IsTrue(_session.Character.Skills.ContainsKey(88));
            Assert.AreEqual(99_500, _session.Character.Gold);
        }

        [TestMethod]
        public async Task ABookTeachesItsPassiveForFree()
        {
            _session.Character.HeroLevel = 10;

            Assert.IsTrue(await _service.LearnFromBookAsync(_session.Character, 88));

            Assert.IsTrue(_session.Character.Skills.ContainsKey(88));
            Assert.AreEqual(100_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.SkillLearned, LastMessage());
        }

        [TestMethod]
        public async Task ABookIsRefusedBelowTheChampionLevel()
        {
            _session.Character.HeroLevel = 9;

            Assert.IsFalse(await _service.LearnFromBookAsync(_session.Character, 88));

            Assert.IsFalse(_session.Character.Skills.ContainsKey(88));
            Assert.AreEqual(Game18NConstString.YouCanNotLearnThat, LastMessage());
        }

        [TestMethod]
        public async Task ABookCannotTeachAClassSkill()
        {
            Assert.IsFalse(await _service.LearnFromBookAsync(_session.Character, 220));

            Assert.IsFalse(_session.Character.Skills.ContainsKey(220));
        }

        [TestMethod]
        public async Task AClassPassiveUsesItsClassLevel()
        {
            await _service.BuyAsync(_session.Character, SwordsmanOnly);
            Assert.IsTrue(_session.Character.Skills.ContainsKey(70));

            _session.Character.Skills.Clear();
            _session.Character.Class = CharacterClassType.Archer;
            await _service.BuyAsync(_session.Character, SwordsmanOnly);
            Assert.IsFalse(_session.Character.Skills.ContainsKey(70));
        }

        [TestMethod]
        public async Task ASkillCostsItsPrice()
        {
            _session.Character.Gold = 479;

            await _service.BuyAsync(_session.Character, Beginner);

            Assert.IsFalse(_session.Character.Skills.ContainsKey(20));
            Assert.AreEqual(479, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.NotEnoughGold, LastMessage());
        }

        [TestMethod]
        public async Task AKnownSkillIsNotSoldTwice()
        {
            Know(Beginner);

            await _service.BuyAsync(_session.Character, Beginner);

            Assert.AreEqual(100_000, _session.Character.Gold);
            Assert.AreEqual(Game18NConstString.YouAlreadyKnowThisSkill, LastMessage());
        }

        [TestMethod]
        public async Task AnUpgradeNeedsItsBaseSkill()
        {
            await _service.BuyAsync(_session.Character, StrongSlash);
            Assert.AreEqual(Game18NConstString.YouNeedBaseSkill, LastMessage());

            Know(Slash);
            await _service.BuyAsync(_session.Character, StrongSlash);
            Assert.IsTrue(_session.Character.Skills.ContainsKey(310));
        }

        [TestMethod]
        public async Task AClassSkillNeedsTheCpLeft()
        {
            // A job level 20 Swordsman has 80 CP; the known Slash spends 10 of them.
            Know(new SkillDto { SkillVNum = 220, SkillType = 1, Class = 1, CastId = 0, CpCost = 10 });

            await _service.BuyAsync(_session.Character, Costing(71));
            Assert.IsFalse(_session.Character.Skills.ContainsKey(310));
            Assert.AreEqual(Game18NConstString.NotEnoughCp, LastMessage());

            await _service.BuyAsync(_session.Character, Costing(70));
            Assert.IsTrue(_session.Character.Skills.ContainsKey(310));
        }

        private static SkillDto Costing(byte cp) => new()
            { SkillVNum = 310, SkillType = 2, Class = 1, CastId = 0, UpgradeSkill = 220, LevelMinimum = 10, Price = 11700, CpCost = cp };

        [TestMethod]
        public async Task APassiveCostsNoCp()
        {
            _session.Character.JobLevel = 1;

            await _service.BuyAsync(_session.Character,
                new SkillDto { SkillVNum = 20, SkillType = 0, CastId = 0, Price = 480, MinimumAdventurerLevel = 4, CpCost = 50 });

            Assert.IsTrue(_session.Character.Skills.ContainsKey(20));
        }

        [TestMethod]
        public async Task AnotherClassSkillCannotBeBought()
        {
            _session.Character.Class = CharacterClassType.Archer;

            await _service.BuyAsync(_session.Character, Slash);

            Assert.IsFalse(_session.Character.Skills.ContainsKey(220));
            Assert.AreEqual(Game18NConstString.YouCanNotLearnThat, LastMessage());
        }
    }
}
