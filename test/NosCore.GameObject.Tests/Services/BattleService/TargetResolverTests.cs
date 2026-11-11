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
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class TargetResolverTests
    {
        private TestWorld World;
        private ClientSession _caster;
        private ClientSession _bystander;
        private MapInstance _map;
        private TargetResolver _resolver;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _caster = await World.CreateSessionAsync();
            _bystander = await World.CreateSessionAsync();
            _map = World.GetBaseMap(0);
            _caster.Character.MapInstance = _map;
            _bystander.Character.MapInstance = _map;
            _caster.Character.PositionX = 0;
            _caster.Character.PositionY = 0;
            _bystander.Character.PositionX = 5;
            _bystander.Character.PositionY = 5;
            _resolver = new TargetResolver(World.SessionRegistry);
        }

        private static SkillInfo AreaSkill() => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.AoeTargetHit, Range: 0, TargetRange: 3, TargetType: 0,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        private IReadOnlyList<MonsterComponentBundle> SpawnMonsters(params (short X, short Y)[] cells)
        {
            var template = new NpcMonsterDto { NpcMonsterVNum = 1, MaxHp = 100, MaxMp = 50, Level = 5 };
            _map.LoadMonsters(
                cells.Select((cell, i) => new MapMonsterDto { MapMonsterId = 200 + i, MapId = 0, MapX = cell.X, MapY = cell.Y, VNum = 1 }).ToList(),
                new List<NpcMonsterDto> { template });
            return _map.Monsters.OrderBy(m => m.VisualId).ToList();
        }

        [TestMethod]
        public void APlayersAreaSkillHitsTheMonstersAroundTheTarget()
        {
            var monsters = SpawnMonsters((5, 5), (5, 4), (0, 5));

            var targets = _resolver.Resolve(_caster.Character, monsters[0], AreaSkill());

            CollectionAssert.AreEquivalent(
                new[] { monsters[0].VisualId, monsters[1].VisualId },
                targets.Where(t => t.VisualType == VisualType.Monster).Select(t => t.VisualId).ToArray());
        }

        [TestMethod]
        public void APlayersAreaSkillNeverHitsAnotherPlayerStandingInTheArea()
        {
            var monsters = SpawnMonsters((5, 5));

            var targets = _resolver.Resolve(_caster.Character, monsters[0], AreaSkill());

            Assert.IsFalse(targets.Any(t => t.VisualType == VisualType.Player));
        }

        [TestMethod]
        public void AMonstersAreaSkillHitsEveryPlayerInTheArea()
        {
            var monsters = SpawnMonsters((5, 5));

            var targets = _resolver.Resolve(monsters[0], _bystander.Character, AreaSkill());

            CollectionAssert.AreEquivalent(
                new[] { _bystander.Character.VisualId },
                targets.Where(t => t.VisualType == VisualType.Player).Select(t => t.VisualId).ToArray());
        }

        [TestMethod]
        public void ACasterCentredAreaSkillHitsAroundTheCasterNotTheTarget()
        {
            var monsters = SpawnMonsters((2, 2), (2, 1), (5, 5), (5, 4));
            var aroundCaster = AreaSkill() with { TargetType = 1 };

            var targets = _resolver.Resolve(_caster.Character, monsters[0], aroundCaster);

            CollectionAssert.AreEquivalent(
                new[] { monsters[0].VisualId, monsters[1].VisualId },
                targets.Where(t => t.VisualType == VisualType.Monster).Select(t => t.VisualId).ToArray());
        }

        [TestMethod]
        public void ASingleTargetSkillOnlyHitsItsTarget()
        {
            var monsters = SpawnMonsters((5, 5), (5, 4));
            var single = AreaSkill() with { HitType = TargetHitType.SingleTargetHit };

            var targets = _resolver.Resolve(_caster.Character, monsters[0], single);

            Assert.AreEqual(1, targets.Count);
        }
    }
}
