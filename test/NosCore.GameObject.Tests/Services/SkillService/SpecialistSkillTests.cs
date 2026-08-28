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
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    [TestClass]
    public class SpecialistSkillTests
    {
        // Flame Druid.
        private const short MorphOfCard = 42;

        private const int ClassTheOldRuleWouldPick = 31 + MorphOfCard;

        private NosCore.GameObject.Services.SkillService.SkillService _service = null!;
        private ClientSession _session = null!;

        private static readonly List<SkillDto> Catalog = new()
        {
            // the adventurer's class skills
            new SkillDto { SkillVNum = 200, Class = 0, LevelMinimum = 1, CastId = 0 },
            new SkillDto { SkillVNum = 201, Class = 0, LevelMinimum = 1, CastId = 1 },
            // the Flame Druid's skills, spread over two classes as in the file
            new SkillDto { SkillVNum = 900, Class = 70, UpgradeType = MorphOfCard, LevelMinimum = 1, CastId = 0 },
            new SkillDto { SkillVNum = 901, Class = 71, UpgradeType = MorphOfCard, LevelMinimum = 5, CastId = 8 },
            new SkillDto { SkillVNum = 902, Class = 71, UpgradeType = MorphOfCard, LevelMinimum = 50, CastId = 9 },
            // the other card: its class is precisely the one 31 + 42 went for
            new SkillDto { SkillVNum = 950, Class = (byte)ClassTheOldRuleWouldPick, UpgradeType = 46, LevelMinimum = 1, CastId = 0 },
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
        public async Task TransformingGrantsTheCardSkills()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 10);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)900);
            CollectionAssert.Contains(learned, (short)901);
        }

        [TestMethod]
        public async Task SkillsAboveTheCardLevelAreNotGranted()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 10);

            CollectionAssert.DoesNotContain(_session.Character.Skills.Keys.ToList(), (short)902);
        }

        [TestMethod]
        public async Task RaisingTheCardLevelUnlocksTheRest()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 10);
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 60);

            CollectionAssert.Contains(_session.Character.Skills.Keys.ToList(), (short)902);
        }

        [TestMethod]
        public async Task AnotherCardSkillsNeverLeakIn()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.DoesNotContain(learned, (short)950);
            CollectionAssert.Contains(learned, (short)900);
            CollectionAssert.Contains(learned, (short)901);
        }

        // One card, two classes: taking only one means half a bar.
        [TestMethod]
        public async Task ACardSpreadOverTwoClassesGivesAllOfThem()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)900);
            CollectionAssert.Contains(learned, (short)901);
            CollectionAssert.Contains(learned, (short)902);
        }

        [TestMethod]
        public async Task SwappingCardReplacesTheSkillsInsteadOfPilingThemUp()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);
            await _service.LoadSpecialistSkillsAsync(_session.Character, morph: 46, spLevel: 99);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)950);
            CollectionAssert.DoesNotContain(learned, (short)900);
        }

        [TestMethod]
        public async Task RemovingTheCardTakesItsSkillsAway()
        {
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);
            await _service.UnloadSpecialistSkillsAsync(_session.Character);

            Assert.IsFalse(_session.Character.Skills.Keys.Any(v => v is 900 or 901 or 902));
        }

        [TestMethod]
        public async Task ClassSkillsSurviveTheTransformation()
        {
            _session.Character.Skills.TryAdd(200, new NosCore.GameObject.Services.BattleService.CharacterSkill
            {
                SkillVNum = 200,
                CharacterId = _session.Character.CharacterId,
                Skill = Catalog.First(s => s.SkillVNum == 200)
            });

            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);
            await _service.UnloadSpecialistSkillsAsync(_session.Character);

            CollectionAssert.Contains(_session.Character.Skills.Keys.ToList(), (short)200);
        }
    }
}
