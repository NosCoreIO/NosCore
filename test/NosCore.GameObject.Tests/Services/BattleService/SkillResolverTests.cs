//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Arch.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Battle;
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
        private INpcCombatCatalog _catalog = null!;
        private IReadOnlyDictionary<short, SkillDto> _skills = null!;

        [TestInitialize]
        public void Setup()
        {
            _skills = new Dictionary<short, SkillDto>
            {
                [100] = new SkillDto { SkillVNum = 100, CastId = 1, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.SingleTargetHit },
                [200] = new SkillDto { SkillVNum = 200, CastId = 2, UpgradeSkill = 0, Cooldown = 50, HitType = (byte)TargetHitType.AoeTargetHit },
            };
            _catalog = new NpcCombatCatalog(new(), new(), new());
        }

        [TestMethod]
        public void UnknownCasterReturnsNull()
        {
            var resolver = new SkillResolver(_skills, _catalog);
            Assert.IsNull(resolver.Resolve(new Mock<IAliveEntity>().Object, 1));
        }

        [TestMethod]
        public void CharacterResolvesByCastId()
        {
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            skills.TryAdd(200, new CharacterSkill { SkillVNum = 200, Skill = _skills[200] });
            character.SetupGet(c => c.Skills).Returns(skills);

            var resolver = new SkillResolver(_skills, _catalog);
            var resolved = resolver.Resolve(character.Object, 2);

            Assert.IsNotNull(resolved);
            Assert.AreEqual((short)200, resolved!.SkillVnum);
            Assert.AreEqual(TargetHitType.AoeTargetHit, resolved.HitType);
        }

        [TestMethod]
        public void CaptureResolvesThoughItsDataNamesAnUpgradeField()
        {
            // Every class's Capture skill carries a non-zero UpgradeSkill without being an upgrade.
            var capture = new SkillDto { SkillVNum = 209, CastId = 16, UpgradeSkill = 3, SkillType = 1 };
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            skills.TryAdd(209, new CharacterSkill { SkillVNum = 209, Skill = capture });
            character.SetupGet(c => c.Skills).Returns(skills);

            var resolved = new SkillResolver(_skills, _catalog).Resolve(character.Object, 16);

            Assert.AreEqual((short)209, resolved?.SkillVnum);
        }

        [TestMethod]
        public void AnUpgradeAloneDoesNotStandInForItsBaseSkill()
        {
            var upgrade = new SkillDto { SkillVNum = 101, CastId = 1, UpgradeSkill = 100, SkillType = 2, Effect = 5 };
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(101, new CharacterSkill { SkillVNum = 101, Skill = upgrade });
            character.SetupGet(c => c.Skills).Returns(skills);

            Assert.IsNull(new SkillResolver(_skills, _catalog).Resolve(character.Object, 1));
        }

        [TestMethod]
        public void CharacterWithoutMatchingCastIdReturnsNull()
        {
            var character = new Mock<ICharacterEntity>();
            var skills = new ConcurrentDictionary<short, CharacterSkill>();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });
            character.SetupGet(c => c.Skills).Returns(skills);

            var resolver = new SkillResolver(_skills, _catalog);
            Assert.IsNull(resolver.Resolve(character.Object, 999));
        }
    }
}
