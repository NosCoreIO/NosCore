//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Battle
{
    [TestClass]
    public class SummonOnDeathHandlerTests
    {
        private const short PodVnum = 45;
        private const short PiiVnum = 36;

        private TestWorld World;
        private MapInstance _map;
        private readonly Mock<INpcCombatCatalog> _catalog = new();
        private readonly Mock<IRandomProvider> _random = new();
        private readonly List<NpcMonsterDto> _npcMonsters = new()
        {
            new NpcMonsterDto { NpcMonsterVNum = PodVnum, MaxHp = 100, Level = 5, IsHostile = true, CanWalk = true },
            new NpcMonsterDto { NpcMonsterVNum = PiiVnum, MaxHp = 50, Level = 3, IsHostile = true, CanWalk = true },
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _map = World.GetBaseMap(0);
            _map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 4, MapY = 2, VNum = PodVnum } },
                _npcMonsters);
        }

        [TestMethod]
        public async Task ADeathSummonCardSpawnsItsMonstersWhereTheBearerDied()
        {
            DeathCards(new BCardDto { Type = (byte)BCardType.CardType.Summons, SubType = 11, FirstData = 2, SecondData = PiiVnum });

            await Handler().Handle(new EntityDiedEvent(Pod(), null));

            var summoned = _map.EnumerateMonsters().Where(m => m.NpcMonster?.NpcMonsterVNum == PiiVnum).ToList();
            Assert.AreEqual(2, summoned.Count);
            Assert.IsTrue(summoned.All(m => m.PositionX == 4 && m.PositionY == 2));
        }

        [TestMethod]
        public async Task AChanceSummonOnlySpawnsWhenTheRollIsUnderTheChance()
        {
            DeathCards(new BCardDto { Type = (byte)BCardType.CardType.Summons, SubType = 12, FirstData = 20, SecondData = PiiVnum });

            _random.Setup(r => r.Next(0, 100)).Returns(20);
            await Handler().Handle(new EntityDiedEvent(Pod(), null));
            Assert.AreEqual(1, _map.EnumerateMonsters().Count());

            _random.Setup(r => r.Next(0, 100)).Returns(19);
            await Handler().Handle(new EntityDiedEvent(Pod(), null));
            Assert.AreEqual(2, _map.EnumerateMonsters().Count());
        }

        [TestMethod]
        public async Task OtherDeathCardsSummonNothing()
        {
            DeathCards(new BCardDto { Type = (byte)BCardType.CardType.SpecialEffects, SubType = 11, FirstData = 10 });

            await Handler().Handle(new EntityDiedEvent(Pod(), null));

            Assert.AreEqual(1, _map.EnumerateMonsters().Count());
        }

        private SummonOnDeathHandler Handler() => new(_catalog.Object, _npcMonsters, _random.Object);

        private MonsterComponentBundle Pod() => _map.EnumerateMonsters().Single(m => m.NpcMonster?.NpcMonsterVNum == PodVnum);

        private void DeathCards(params BCardDto[] cards) =>
            _catalog.Setup(c => c.GetDeathBCards(PodVnum)).Returns(cards);
    }
}
