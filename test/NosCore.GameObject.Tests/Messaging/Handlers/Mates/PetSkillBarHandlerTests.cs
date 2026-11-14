//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Mate;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Mates
{
    [TestClass]
    public class PetSkillBarHandlerTests
    {
        private const short PetSkill = 664;

        private TestWorld World;
        private ClientSession _session;
        private PetSkillBarHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.Inject(new List<SkillDto> { new() { SkillVNum = PetSkill, SkillType = 5 } });
            World.Inject(new List<NpcMonsterSkillDto> { new() { NpcMonsterVNum = 333, SkillVNum = PetSkill } });
            _handler = World.UseReal<INpcCombatCatalog, NpcCombatCatalog>()
                .UseReal<IPetSkillBarService, PetSkillBarService>()
                .Create<PetSkillBarHandler>();

            var pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 5, 5);
        }

        [TestMethod]
        public async Task EnteringAMapWithATeamPetShowsItsSkillBar()
        {
            await _handler.Handle(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance, World.GetBaseMap(1)));

            Assert.AreEqual(PetSkill, _session.LastPackets.OfType<PetskiPacket>().Single().FirstSkillVNum);
        }

        [TestMethod]
        public async Task ACaptureShowsTheSkillBarOfThePetNowInTheTeam()
        {
            var map = _session.Character.MapInstance;
            map.LoadMonsters(
                new List<MapMonsterDto> { new() { MapMonsterId = 9100, MapId = 0, MapX = 6, MapY = 5, VNum = 2 } },
                new List<NpcMonsterDto> { new() { NpcMonsterVNum = 2, MaxHp = 100, Level = 1 } });

            await _handler.Handle(new EntityCapturedEvent(map.EnumerateMonsters().Single(m => m.VisualId == 9100), _session.Character));

            Assert.AreEqual(PetSkill, _session.LastPackets.OfType<PetskiPacket>().Single().FirstSkillVNum);
        }
    }
}
