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
using System.Threading;
using Arch.Core;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Tests.Shared;
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
        private INpcCombatCatalog _catalog;
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
        }

        private static async Task<PlayerComponentBundle> NewCharacterAsync() =>
            (await (await TestWorld.CreateAsync()).CreateSessionAsync()).Character;

        [TestMethod]
        public void UnknownCasterReturnsNull()
        {
            var resolver = new SkillResolver(_skills, _catalog);
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

            var resolver = new SkillResolver(_skills, _catalog);
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

            var resolved = new SkillResolver(_skills, _catalog).Resolve(character, 16);

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

            Assert.IsNull(new SkillResolver(_skills, _catalog).Resolve(character, 1));
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

            Assert.AreEqual((short)539, new SkillResolver(_skills, _catalog).Resolve(character, 1)?.Effect);
        }

        [TestMethod]
        public async Task CharacterWithoutMatchingCastIdReturnsNull()
        {
            var character = await NewCharacterAsync();
            var skills = character.Skills;
            skills.Clear();
            skills.TryAdd(100, new CharacterSkill { SkillVNum = 100, Skill = _skills[100] });

            var resolver = new SkillResolver(_skills, _catalog);
            Assert.IsNull(resolver.Resolve(character, 999));
        }
    }
}
