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
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    // This table's trap: **class 0 does not mean "Adventurer"**. It is the scrap
    // container where 193 entries end up - passives, monster skills, things with no cost and no cast.
    //
    // Filtering by class as is done for the other jobs, an Adventurer got all of them:
    // the bar filled with icons the client will not cast. Trying it with the client in hand the
    // symptom was "the skills arrive but do not work", and it was exactly this.
    [TestClass]
    public class ClassSkillLearningTests
    {
        private TestWorld World;
        private NosCore.GameObject.Services.SkillService.SkillService _service;
        private ClientSession _session;
        private RememberingSkillDao _dao;

        private static readonly List<SkillDto> Catalog = new()
        {
            // the Adventurer's real skills, listed one by one from the original game
            new SkillDto { SkillVNum = 200, Class = 0, LevelMinimum = 0, CastId = 0, SkillType = 1 },
            new SkillDto { SkillVNum = 201, Class = 0, LevelMinimum = 0, CastId = 1, SkillType = 1 },
            new SkillDto { SkillVNum = 208, Class = 0, LevelMinimum = 0, CastId = 8, SkillType = 1 },
            new SkillDto { SkillVNum = 210, Class = 0, LevelMinimum = 0, CastId = 9, SkillType = 1 },
            // 209 is the Adventurer's Capture: class 0, cast id 16, LevelMinimum 1 in Skill.dat.
            new SkillDto { SkillVNum = 209, Class = 0, LevelMinimum = 1, CastId = 16, SkillType = 1 },
            // A cheat skill, class 0 like the rest of the bucket.
            new SkillDto { SkillVNum = 211, Class = 0, LevelMinimum = 0, CastId = 10, SkillType = 1 },
            // scrap: the same class 0, but they are passives and monster skills
            new SkillDto { SkillVNum = 1, Class = 0, LevelMinimum = 0, CastId = 4, SkillType = 3 },
            new SkillDto { SkillVNum = 17, Class = 0, LevelMinimum = 0, CastId = 0, SkillType = 3 },
            new SkillDto { SkillVNum = 999, Class = 0, LevelMinimum = 0, CastId = 4, SkillType = 3 },
            // a real job's skill
            new SkillDto { SkillVNum = 220, Class = 1, LevelMinimum = 0, CastId = 0, SkillType = 1 },
            new SkillDto { SkillVNum = 221, Class = 1, LevelMinimum = 5, CastId = 1, SkillType = 1 },
            new SkillDto { SkillVNum = 222, Class = 1, LevelMinimum = 50, CastId = 2, SkillType = 1 },
            new SkillDto { SkillVNum = 20, Class = 0, LevelMinimum = 0, CastId = 0, SkillType = 0 },
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _dao = new RememberingSkillDao();
            _service = new NosCore.GameObject.Services.SkillService.SkillService(_dao, Catalog,
                new Mock<IVitalityService>().Object, new Mock<IBattleStatsProvider>().Object,
                new NosCore.Algorithm.ExperienceService.ExperienceService(), new NosCore.Algorithm.JobExperienceService.JobExperienceService(),
                new NosCore.Algorithm.HeroExperienceService.HeroExperienceService(), new NosCore.Algorithm.SpExperienceService.SpExperienceService(), new NosCore.Algorithm.SkillPointService.SkillPointService());
        }

        [TestMethod]
        public async Task AdventurerGetsOnlyItsOwnSkills()
        {
            _session.Character.Class = CharacterClassType.Adventurer;
            _session.Character.JobLevel = 20;

            await _service.LearnClassSkillsAsync(_session.Character);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)200);
            CollectionAssert.Contains(learned, (short)210);
        }

        [TestMethod]
        public async Task AdventurerDoesNotGetTheJunkBucket()
        {
            // It is the heart of the defect: 193 scrap entries share class 0.
            _session.Character.Class = CharacterClassType.Adventurer;
            _session.Character.JobLevel = 20;

            await _service.LearnClassSkillsAsync(_session.Character);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.DoesNotContain(learned, (short)1);
            CollectionAssert.DoesNotContain(learned, (short)17);
            CollectionAssert.DoesNotContain(learned, (short)999);
        }

        // 209 is the pet catcher. Skill.dat gives it class 0, cast id 16 and LevelMinimum 1, and
        // CharNewPacketHandler hands it to every new character - its comment there says a missing
        // one makes `u_s 16` answer `cancel 2`, and 16 is exactly this skill's cast id.
        //
        // The first version of this list left it out, and the test asserted the omission. Losing
        // it raises nothing: the Adventurer simply cannot catch anything any more.
        [TestMethod]
        public async Task TwoHundredAndNineIsTheAdventurersCapture()
        {
            _session.Character.Class = CharacterClassType.Adventurer;
            _session.Character.JobLevel = 20;

            await _service.LearnClassSkillsAsync(_session.Character);

            CollectionAssert.Contains(_session.Character.Skills.Keys.ToList(), (short)209);
        }

        // 211 and 212 are what the file calls "Ultra Super Cheating Skill" and "Admin Cheating
        // Skill". They sit in class 0 with everything else.
        [TestMethod]
        public async Task TheCheatSkillsAreNotHandedOut()
        {
            _session.Character.Class = CharacterClassType.Adventurer;
            _session.Character.JobLevel = 20;

            await _service.LearnClassSkillsAsync(_session.Character);

            CollectionAssert.DoesNotContain(_session.Character.Skills.Keys.ToList(), (short)211);
        }

        [TestMethod]
        public async Task OtherClassesStillFilterByClass()
        {
            _session.Character.Class = CharacterClassType.Swordsman;
            _session.Character.JobLevel = 10;

            await _service.LearnClassSkillsAsync(_session.Character);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)220);
            CollectionAssert.Contains(learned, (short)221);
            CollectionAssert.DoesNotContain(learned, (short)222);   // above the job level
            CollectionAssert.DoesNotContain(learned, (short)200);   // and nothing from class 0
        }

        // The in-memory dictionary is keyed by skill number and collapses duplicates, so counting
        // it proves nothing: the rows are what piled up. This watches the store.
        [TestMethod]
        public async Task LearningTwiceReusesTheRowInsteadOfAddingOne()
        {
            _session.Character.Class = CharacterClassType.Swordsman;
            _session.Character.JobLevel = 10;

            await _service.LearnClassSkillsAsync(_session.Character);
            var rowsAfterFirst = _dao.Rows.Count;
            var idOf220 = _dao.Rows.Single(r => r.SkillVNum == 220).Id;

            // Only the persisted side survives a relog, so this is the state the second call
            // meets in production.
            _session.Character.Skills.Clear();
            await _service.LearnClassSkillsAsync(_session.Character);

            Assert.AreEqual(rowsAfterFirst, _dao.Rows.Count);
            Assert.AreEqual(idOf220, _dao.Rows.Single(r => r.SkillVNum == 220).Id);
        }

        // A class change puts the job level back to 1, so a row for a skill of the destination
        // class needing job 50 is as unusable as one of the class left behind. Left in place it
        // came back at the next login, past any level check.
        [TestMethod]
        public async Task ARowAboveTheJobLevelIsForgottenToo()
        {
            _session.Character.Class = CharacterClassType.Swordsman;
            _session.Character.JobLevel = 1;
            _dao.Rows.Add(new CharacterSkillDto
            {
                Id = Guid.NewGuid(),
                CharacterId = _session.Character.VisualId,
                SkillVNum = 222
            });

            await _service.ForgetUnlearnableSkillsAsync(_session.Character);

            Assert.IsFalse(_dao.Rows.Any(r => r.SkillVNum == 222));
        }

        // Passives are bought, not tied to a class, so neither a class change nor a login drops them.
        [TestMethod]
        public async Task APassiveIsKept()
        {
            _session.Character.Class = CharacterClassType.Swordsman;
            _dao.Rows.Add(new CharacterSkillDto
            {
                Id = Guid.NewGuid(),
                CharacterId = _session.Character.VisualId,
                SkillVNum = 20
            });

            await _service.ForgetUnlearnableSkillsAsync(_session.Character);

            Assert.IsTrue(_dao.Rows.Any(r => r.SkillVNum == 20));
        }

        // Passive 20 shares cast id 0 with the basic attack; a cast must never pick it.
        [TestMethod]
        public void APassiveIsNeverTheSkillACastResolvesTo()
        {
            _session.Character.Skills.Clear();
            _session.Character.Skills[20] = new CharacterSkill { SkillVNum = 20, Skill = Catalog.Single(s => s.SkillVNum == 20) };
            _session.Character.Skills[220] = new CharacterSkill { SkillVNum = 220, Skill = Catalog.Single(s => s.SkillVNum == 220) };

            Assert.AreEqual((short)220, SkillResolver.FindLearned(_session.Character, 0)?.SkillVNum);
        }
    }
}
