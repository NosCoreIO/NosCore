//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.QuestService;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Quest
{
    [TestClass]
    public class OnEntityDiedHandlerTests
    {
        private TestWorld World;
        private ClientSession _killer;
        private ClientSession _member;
        private ClientSession _away;
        private ClientSession _stranger;
        private MonsterComponentBundle _monster;
        private Mock<IQuestService> _questService;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _killer = await World.CreateSessionAsync();
            _member = await World.CreateSessionAsync();
            _away = await World.CreateSessionAsync();
            _stranger = await World.CreateSessionAsync();
            _questService = new Mock<IQuestService>();
            var map = World.GetBaseMap(TestWorld.TestMapId);
            map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 200, MapId = 0, MapX = 5, MapY = 4, VNum = 340 } },
                new NpcMonsterCatalog(new List<NpcMonsterDto> { new() { NpcMonsterVNum = 340, Level = 10, MaxHp = 100 } }));
            _monster = map.EnumerateMonsters().Single();
        }

        [TestMethod]
        public async Task AKillCountsForThePartyMembersOnTheMap()
        {
            await new Spec("A kill counts towards the quests of every party member on the monster's map, whether they hit it or not")
                .GivenAsync(TheKillerIsInAPartyWithOneMemberHereAndOneElsewhere)
                .WhenAsync(TheKillerKillsTheMonsterAlone)
                .Then(TheKillShouldCountFor_, _killer)
                .And(TheKillShouldCountFor_, _member)
                .And(TheKillShouldNotCountFor_, _away)
                .And(TheKillShouldNotCountFor_, _stranger)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillCountsOnceForAMemberWhoAlsoHitIt()
        {
            await new Spec("A party member who hit the monster too is counted once")
                .GivenAsync(TheKillerIsInAPartyWithOneMemberHereAndOneElsewhere)
                .WhenAsync(TheKillerAndTheMemberKillTheMonster)
                .Then(TheKillShouldCountFor_, _member)
                .And(TheKillShouldCountFor_, _killer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKillWithoutAPartyCountsOnlyForWhoHitIt()
        {
            await new Spec("Without a party, a kill counts only for the players who hit the monster")
                .WhenAsync(TheKillerKillsTheMonsterAlone)
                .Then(TheKillShouldCountFor_, _killer)
                .And(TheKillShouldNotCountFor_, _member)
                .ExecuteAsync();
        }

        private async Task TheKillerIsInAPartyWithOneMemberHereAndOneElsewhere()
        {
            await World.MapChangeService.ChangeMapInstanceAsync(_away, World.GetBaseMap(TestWorld.ShopMapId).MapInstanceId, 1, 1);
            var party = new Group(GroupType.Group, World.SessionGroupFactory);
            foreach (var member in new[] { _killer, _member, _away })
            {
                member.Character.JoinGroup(party);
            }
        }

        private Task TheKillerKillsTheMonsterAlone() =>
            Kill(new Dictionary<Entity, int> { [_killer.Character.Handle] = 100 });

        private Task TheKillerAndTheMemberKillTheMonster() =>
            Kill(new Dictionary<Entity, int> { [_killer.Character.Handle] = 60, [_member.Character.Handle] = 40 });

        private Task Kill(Dictionary<Entity, int> hits) =>
            new OnEntityDiedHandler(_questService.Object, NullLogger<OnEntityDiedHandler>.Instance,
                    new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object)
                .Handle(new EntityDiedEvent(_monster, _killer.Character, hits));

        private void TheKillShouldCountFor_(ClientSession session)
        {
            var id = session.Character.VisualId;
            _questService.Verify(q => q.OnMonsterKilledAsync(
                It.Is<PlayerComponentBundle>(p => p.VisualId == id),
                It.Is<NpcMonsterDto>(m => m.NpcMonsterVNum == 340)), Times.Once);
        }

        private void TheKillShouldNotCountFor_(ClientSession session)
        {
            var id = session.Character.VisualId;
            _questService.Verify(q => q.OnMonsterKilledAsync(
                It.Is<PlayerComponentBundle>(p => p.VisualId == id), It.IsAny<NpcMonsterDto>()), Times.Never);
        }
    }
}
