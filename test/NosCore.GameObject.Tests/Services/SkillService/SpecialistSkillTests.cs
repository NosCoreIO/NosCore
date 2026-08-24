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
    // Transforming changes the skill kit: the class ones leave the bar and
    // the card's arrive. Without this step you transform and keep the
    // previous skills - the opposite of what a specialist is for.
    //
    // WHICH SKILL BELONGS TO WHICH CARD is told by Skill.dat, DATA section, second field: it
    // carries the card's "design", the same number the card item exposes in INDEX[5] and that
    // arrives here as `morph`. The column is still called UpgradeType for historical reasons.
    //
    // The obvious rule is arithmetic, "class == 31 + morph". The scene below is the real one of
    // the Flame Druid, and it is why that rule is not enough:
    //
    //   * its 22 skills sit on TWO classes, 70 and 71, with complementary cast ids 0 to 21 - no
    //     single class holds them;
    //   * 31 + 42 makes 73, and class 73 exists: it belongs to ANOTHER specialist. So the old
    //     rule does not leave the bar empty, which would be noticed - it hands out the wrong
    //     skills.
    //
    // 283 of the 627 specialist skills behave this way, across 23 cards, and for 18 of those the
    // class the arithmetic picks exists and belongs to somebody else.
    [TestClass]
    public class SpecialistSkillTests
    {
        // Flame Druid.
        private const short MorphOfCard = 42;

        // The class the old arithmetic rule would have picked - and which belongs to
        // another card.
        private const int ClassTheOldRuleWouldPick = 31 + MorphOfCard;

        private NosCore.GameObject.Services.SkillService.SkillService _service = null!;
        private ClientSession _session = null!;

        private static readonly List<SkillDto> Catalog = new()
        {
            // abilita' di classe (avventuriero)
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
            // The card gains levels and unlocks skills as it goes: handing them all over at
            // once would make levelling it pointless.
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

        // The test that defends the real bug: 950 is the skill on class 73, that is
        // exactly the one the old "31 + morph" rule picked for this card.
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
            // Changing specialist without clearing would leave the kit of every
            // card ever worn, and a bar full of icons the client refuses to cast.
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);
            await _service.LoadSpecialistSkillsAsync(_session.Character, morph: 46, spLevel: 99);

            var learned = _session.Character.Skills.Keys.ToList();
            CollectionAssert.Contains(learned, (short)950);
            CollectionAssert.DoesNotContain(learned, (short)900);
        }

        [TestMethod]
        public async Task RemovingTheCardTakesItsSkillsAway()
        {
            // Leaving them would mean casting them untransformed, which is the shortest way to
            // make transforming pointless.
            await _service.LoadSpecialistSkillsAsync(_session.Character, MorphOfCard, spLevel: 99);
            await _service.UnloadSpecialistSkillsAsync(_session.Character);

            Assert.IsFalse(_session.Character.Skills.Keys.Any(v => v is 900 or 901 or 902));
        }

        [TestMethod]
        public async Task ClassSkillsSurviveTheTransformation()
        {
            // The two lists live in the same collection: what changes is which of the two
            // ends up on the bar. Deleting the class skills on transformation
            // significherebbe perderle per sempre.
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
