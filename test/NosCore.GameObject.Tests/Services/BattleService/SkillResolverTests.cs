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
