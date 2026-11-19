//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Arch.Core;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Tests.Shared;
using Moq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.QuestService;
using NosCore.GameObject.Services.ShopService;
using NosCore.Networking;
using NosCore.Packets.Interfaces;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillResolverTests
    {
        private INpcCombatCatalog _catalog;
        private Mock<ICardCatalog> _cards;
        private IReadOnlyDictionary<short, SkillDto> _skills;

        [TestInitialize]
        public void Setup()
        {
            _skills = new Dictionary<short, SkillDto>
            {
                [100] = new SkillDto { SkillVNum = 100, CastId = 1, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.SingleTargetHit, SkillType = 1 },
                [200] = new SkillDto { SkillVNum = 200, CastId = 2, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.AoeTargetHit, SkillType = 1 },
            };
            _catalog = new NpcCombatCatalog(new(), new(), new());
            _cards = new Mock<ICardCatalog>();
            _cards.Setup(c => c.GetCard(It.IsAny<short>())).Returns<short>(id => new CardDto { CardId = id, GroupId = (short)(id / 10) });
        }

        private static async Task<PlayerComponentBundle> NewCharacterAsync() =>
            (await (await TestWorld.CreateAsync()).CreateSessionAsync()).Character;

        [TestMethod]
        public void AMonstersBasicAttackIsNotItsSkillThatSharesCastIdZero()
        {
            var skills = new Dictionary<short, SkillDto>(_skills) { [300] = new SkillDto { SkillVNum = 300, CastId = 0, Cooldown = 400, SkillType = 1 } };
            var catalog = new NpcCombatCatalog(new() { new NpcMonsterSkillDto { NpcMonsterVNum = 111, SkillVNum = 300, Rate = 100 } }, new(), new());
            var monster = new Mock<INonPlayableEntity>();
            monster.Setup(m => m.NpcMonster).Returns(new NpcMonsterDto { NpcMonsterVNum = 111, BasicCooldown = 20 });
            var resolver = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(skills.Values.ToList()), catalog, _cards.Object);

            Assert.AreEqual((short?)300, resolver.Resolve(monster.Object, 0)?.SkillVnum);
            Assert.AreEqual((short?)0, resolver.ResolveBasic(monster.Object)?.SkillVnum);
        }

        [TestMethod]
        public void UnknownCasterReturnsNull()
        {
            var resolver = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object);
            Assert.IsNull(resolver.Resolve(new Mock<IAliveEntity>().Object, 1));
        }

        [TestMethod]
        public async Task CharacterResolvesByCastId()
        {
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            skills.TryAdd(200, new CharacterSkill { SkillVNum = 200, Skill = _skills[200] });

            var resolver = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object);
            var resolved = resolver.Resolve(character, 2);

            Assert.IsNotNull(resolved);
            Assert.AreEqual((short)200, resolved.SkillVnum);
            Assert.AreEqual(TargetHitType.AoeTargetHit, resolved.HitType);
        }

        [TestMethod]
        public async Task CaptureResolvesThoughItsDataNamesAnUpgradeField()
        {
            // Every class's Capture skill carries a non-zero UpgradeSkill without being an upgrade.
            var capture = new SkillDto { SkillVNum = 209, CastId = 16, UpgradeSkill = 3, SkillType = 1 };
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            skills.TryAdd(209, new CharacterSkill { SkillVNum = 209, Skill = capture });

            var resolved = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object).Resolve(character, 16);

            Assert.AreEqual((short)209, resolved?.SkillVnum);
        }

        [TestMethod]
        public async Task AnUpgradeAloneDoesNotStandInForItsBaseSkill()
        {
            var upgrade = new SkillDto { SkillVNum = 101, CastId = 1, UpgradeSkill = 100, SkillType = 2, Effect = 5 };
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(101, new CharacterSkill { SkillVNum = 101, Skill = upgrade });

            Assert.IsNull(new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object).Resolve(character, 1));
        }

        [TestMethod]
        public async Task AnOwnedUpgradeGivesTheSkillItsEffect()
        {
            var baseSkill = new SkillDto { SkillVNum = 100, CastId = 1, SkillType = 1, Effect = 515 };
            var upgrade = new SkillDto { SkillVNum = 101, CastId = 1, UpgradeSkill = 100, SkillType = 2, Effect = 539 };
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = baseSkill });
            skills.TryAdd(101, new CharacterSkill { SkillVNum = 101, Skill = upgrade });

            Assert.AreEqual((short)539, new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object).Resolve(character, 1)?.Effect);
        }

        private const short BaseSkill = 100;
        private const short FirstUpgrade = 101;
        private const short SecondUpgrade = 102;

        // Cards 10 to 19 share a card group, as do 20 to 29.
        private static BCardDto Causes(short skill, short card, int percent = 100) => new()
        {
            SkillVNum = skill, Type = BCardType.CardType.Buff, SubType = 11, FirstData = percent, SecondData = card
        };

        private static BCardDto MeleeAttack(short skill, int value) => new()
        {
            SkillVNum = skill, Type = BCardType.CardType.AttackPower, SubType = 21, FirstData = value
        };

        private async Task<List<BCardDto>> CastWithUpgradesAsync(List<BCardDto> bCards, params short[] upgrades)
        {
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(BaseSkill, new CharacterSkill { SkillVNum = BaseSkill, Skill = _skills[BaseSkill] });
            foreach (var upgrade in upgrades)
            {
                skills.TryAdd(upgrade, new CharacterSkill
                {
                    SkillVNum = upgrade, Skill = new SkillDto { SkillVNum = upgrade, CastId = 1, UpgradeSkill = BaseSkill, SkillType = 2, Effect = -1 }
                });
            }

            var resolved = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), new NpcCombatCatalog(new(), new(), bCards), _cards.Object).Resolve(character, 1);
            return resolved?.BCards.ToList() ?? throw new InvalidOperationException();
        }

        [TestMethod]
        public async Task ALearnedUpgradeAddsItsEffectsToTheSkill()
        {
            var bCards = await CastWithUpgradesAsync([MeleeAttack(BaseSkill, 150), MeleeAttack(FirstUpgrade, 100), MeleeAttack(SecondUpgrade, 150)],
                FirstUpgrade, SecondUpgrade);

            CollectionAssert.AreEqual(new[] { 150, 100, 150 }, bCards.Select(b => b.FirstData).ToArray());
        }

        [TestMethod]
        public async Task AnUpgradeNotLearnedAddsNothing()
        {
            var bCards = await CastWithUpgradesAsync([MeleeAttack(BaseSkill, 150), MeleeAttack(FirstUpgrade, 100)]);

            CollectionAssert.AreEqual(new[] { 150 }, bCards.Select(b => b.FirstData).ToArray());
        }

        [TestMethod]
        public async Task AnUpgradesCardTakesThePlaceOfTheSkillsCardOfTheSameGroup()
        {
            var bCards = await CastWithUpgradesAsync([Causes(BaseSkill, 17), Causes(FirstUpgrade, 18)], FirstUpgrade);

            CollectionAssert.AreEqual(new[] { 18 }, bCards.Select(b => b.SecondData).ToArray());
        }

        [TestMethod]
        public async Task AnUpgradeCausingTheSameCardReplacesItsChance()
        {
            var bCards = await CastWithUpgradesAsync([Causes(BaseSkill, 7, 60), Causes(FirstUpgrade, 7, 90)], FirstUpgrade);

            CollectionAssert.AreEqual(new[] { 90 }, bCards.Select(b => b.FirstData).ToArray());
        }

        [TestMethod]
        public async Task TheLastLearnedUpgradeOfACardGroupWins()
        {
            var bCards = await CastWithUpgradesAsync([Causes(BaseSkill, 39), Causes(FirstUpgrade, 30), Causes(SecondUpgrade, 31)],
                FirstUpgrade, SecondUpgrade);

            CollectionAssert.AreEqual(new[] { 31 }, bCards.Select(b => b.SecondData).ToArray());
        }

        [TestMethod]
        public async Task AnUpgradesCardOfAnotherGroupIsAdded()
        {
            var bCards = await CastWithUpgradesAsync([Causes(BaseSkill, 17), Causes(FirstUpgrade, 25)], FirstUpgrade);

            CollectionAssert.AreEqual(new[] { 17, 25 }, bCards.Select(b => b.SecondData).ToArray());
        }

        [TestMethod]
        public async Task CardsWithoutAGroupAreNotReplacedByOneAnother()
        {
            _cards.Setup(c => c.GetCard(It.IsAny<short>())).Returns<short>(id => new CardDto { CardId = id, GroupId = 0 });

            var bCards = await CastWithUpgradesAsync([Causes(BaseSkill, 1), Causes(FirstUpgrade, 2)], FirstUpgrade);

            CollectionAssert.AreEqual(new[] { 1, 2 }, bCards.Select(b => b.SecondData).ToArray());
        }

        [TestMethod]
        public async Task CharacterWithoutMatchingCastIdReturnsNull()
        {
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });

            var resolver = new SkillResolver(new NosCore.GameObject.Services.SkillService.SkillCatalog(_skills.Values.ToList()), _catalog, _cards.Object);
            Assert.IsNull(resolver.Resolve(character, 999));
        }
    }
}
