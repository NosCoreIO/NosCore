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
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MonsterChatterServiceTests
    {
        private TestWorld World;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            World.Inject(new List<ChatterLineDto>
            {
                new() { ChatterLineId = 1, ChatterGroup = 1, Message = (short)Game18NConstString.Grrrrrrrrrrr },
                new() { ChatterLineId = 2, ChatterGroup = 1, Message = (short)Game18NConstString.KeeeKeeee },
                new() { ChatterLineId = 3, ChatterGroup = 2, Text = nameof(LanguageKey.CHATTER_GRR) },
                new() { ChatterLineId = 4, ChatterGroup = 2, Text = nameof(LanguageKey.CHATTER_IMPROVE_WORKING_CONDITIONS) },
            });
            World.Freeze<Mock<IRandomProvider>>().Setup(r => r.Next(0, 2)).Returns(1);
        }

        [TestMethod]
        public async Task AMonsterShoutsOneOfItsGroupsLines()
        {
            var monster = SpawnMonster(1);

            await World.Create<MonsterChatterService>().ShoutAsync(monster);

            var say = Session.Character.MapInstance.LastPackets.OfType<SayiPacket>().Single();
            Assert.AreEqual(VisualType.Monster, say.VisualType);
            Assert.AreEqual(monster.VisualId, say.VisualId);
            Assert.AreEqual(Game18NConstString.KeeeKeeee, say.Message);
        }

        [TestMethod]
        public async Task ALineTheClientLacksIsSentInTheListenersLanguage()
        {
            var monster = SpawnMonster(2);

            await World.Create<MonsterChatterService>().ShoutAsync(monster);

            var say = Session.LastPackets.OfType<SayPacket>().Single();
            Assert.AreEqual(VisualType.Monster, say.VisualType);
            Assert.AreEqual(monster.VisualId, say.VisualId);
            Assert.AreEqual(World.GameLanguageLocalizer[LanguageKey.CHATTER_IMPROVE_WORKING_CONDITIONS, Session.Character.AccountLanguage],
                say.Message);
        }

        [TestMethod]
        public async Task AMonsterWithoutAGroupStaysSilent()
        {
            var monster = SpawnMonster(0);

            await World.Create<MonsterChatterService>().ShoutAsync(monster);

            Assert.IsFalse(Session.Character.MapInstance.LastPackets.OfType<SayiPacket>().Any());
        }

        private MonsterComponentBundle SpawnMonster(byte chatterGroup)
        {
            var map = Session.Character.MapInstance;
            map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 300, MapId = map.Map.MapId, MapX = 1, MapY = 1, VNum = 1 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 1, MaxHp = 100, ChatterGroup = chatterGroup } });
            return map.EnumerateMonsters().Single();
        }
    }
}
