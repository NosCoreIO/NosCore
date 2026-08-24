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
        private NosCore.GameObject.Services.SkillService.SkillService _service = null!;
        private ClientSession _session = null!;

        private static readonly List<SkillDto> Catalog = new()
        {
            // the Adventurer's real skills, listed one by one from the original game
            new SkillDto { SkillVNum = 200, Class = 0, LevelMinimum = 0, CastId = 0 },
            new SkillDto { SkillVNum = 201, Class = 0, LevelMinimum = 0, CastId = 1 },
            new SkillDto { SkillVNum = 208, Class = 0, LevelMinimum = 0, CastId = 8 },
            new SkillDto { SkillVNum = 210, Class = 0, LevelMinimum = 0, CastId = 10 },
            // 209 does NOT exist as an Adventurer skill
            new SkillDto { SkillVNum = 209, Class = 0, LevelMinimum = 0, CastId = 9 },
            // scrap: the same class 0, but they are passives and monster skills
            new SkillDto { SkillVNum = 1, Class = 0, LevelMinimum = 0, CastId = 4, SkillType = 3 },
            new SkillDto { SkillVNum = 17, Class = 0, LevelMinimum = 0, CastId = 0, SkillType = 3 },
            new SkillDto { SkillVNum = 999, Class = 0, LevelMinimum = 0, CastId = 4, SkillType = 3 },
            // a real job's skill
            new SkillDto { SkillVNum = 220, Class = 1, LevelMinimum = 0, CastId = 0 },
            new SkillDto { SkillVNum = 221, Class = 1, LevelMinimum = 5, CastId = 1 },
            new SkillDto { SkillVNum = 222, Class = 1, LevelMinimum = 50, CastId = 2 },
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _service = new NosCore.GameObject.Services.SkillService.SkillService(
                new Mock<IDao<CharacterSkillDto, Guid>>().Object, Catalog);
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

        [TestMethod]
        public async Task TwoHundredAndNineIsNotAnAdventurerSkill()
        {
            // A hole in the numbering that the original game skips explicitly. The
            // test character had it, because the class filter did not exclude it.
            _session.Character.Class = CharacterClassType.Adventurer;
            _session.Character.JobLevel = 20;

            await _service.LearnClassSkillsAsync(_session.Character);

            CollectionAssert.DoesNotContain(_session.Character.Skills.Keys.ToList(), (short)209);
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
            CollectionAssert.DoesNotContain(learned, (short)222);   // oltre il livello di lavoro
            CollectionAssert.DoesNotContain(learned, (short)200);   // e niente roba di classe 0
        }

        [TestMethod]
        public async Task LearningTwiceDoesNotDuplicate()
        {
            _session.Character.Class = CharacterClassType.Swordsman;
            _session.Character.JobLevel = 10;

            await _service.LearnClassSkillsAsync(_session.Character);
            var afterFirst = _session.Character.Skills.Count;
            await _service.LearnClassSkillsAsync(_session.Character);

            Assert.AreEqual(afterFirst, _session.Character.Skills.Count);
        }
    }
}
