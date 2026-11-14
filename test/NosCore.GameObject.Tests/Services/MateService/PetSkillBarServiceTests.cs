//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class PetSkillBarServiceTests
    {
        private const short Otter = 683;
        private const short Hoarder = 700;
        private const short Wolf = 12;
        private const short Bob = 317;
        private const long Owner = 1;

        private TestWorld World;
        private PetSkillBarService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject(new List<SkillDto>
            {
                new() { SkillVNum = 663, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 664, SkillType = 5, CastId = 2 },
                new() { SkillVNum = 665, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 666, SkillType = 5, CastId = 3 },
                new() { SkillVNum = 1100, SkillType = 4, CastId = 1 }
            });
            World.Inject(new List<NpcMonsterSkillDto>
            {
                new() { NpcMonsterSkillId = 12, NpcMonsterVNum = Otter, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 11, NpcMonsterVNum = Otter, SkillVNum = 663 },
                new() { NpcMonsterSkillId = 13, NpcMonsterVNum = Otter, SkillVNum = 1100 },
                new() { NpcMonsterSkillId = 20, NpcMonsterVNum = Wolf, SkillVNum = 1100 },
                new() { NpcMonsterSkillId = 30, NpcMonsterVNum = Bob, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 41, NpcMonsterVNum = Hoarder, SkillVNum = 663 },
                new() { NpcMonsterSkillId = 42, NpcMonsterVNum = Hoarder, SkillVNum = 664 },
                new() { NpcMonsterSkillId = 43, NpcMonsterVNum = Hoarder, SkillVNum = 665 },
                new() { NpcMonsterSkillId = 44, NpcMonsterVNum = Hoarder, SkillVNum = 666 }
            });
            _service = World.UseReal<INpcCombatCatalog, NpcCombatCatalog>().Create<PetSkillBarService>();
        }

        [TestMethod]
        public void APetsBarHoldsItsCreaturesOwnSkillsInTheOrderOfItsData()
        {
            CollectionAssert.AreEqual(new short[] { 663, 664 }, Bar(TestMates.Pet(Owner, vnum: Otter)),
                "the skill an ordinary monster fights with has no place on the bar");
        }

        [TestMethod]
        public void APetWhoseCreatureHasNoSkillOfItsOwnHasAnEmptyBar()
        {
            Assert.AreEqual(0, Bar(TestMates.Pet(Owner, vnum: Wolf)).Length);
        }

        [TestMethod]
        public void TheBarHasThreeSlots()
        {
            CollectionAssert.AreEqual(new short[] { 663, 664, 665 }, Bar(TestMates.Pet(Owner, vnum: Hoarder)));
        }

        [TestMethod]
        public void APartnerHasNoPetSkillBar()
        {
            Assert.AreEqual(0, Bar(TestMates.Partner(Owner)).Length);
        }

        [TestMethod]
        public async Task TheOwnerIsShownTheBarOfTheTeamPetAtTheirSide()
        {
            var owner = await World.CreateSessionAsync();
            await PetOutAsync(owner, TestMates.Pet(owner.Character.CharacterId, vnum: Otter));

            await _service.RefreshAsync(owner.Character);

            Assert.AreEqual((663, 664, -1), Shown(owner));
            Assert.AreEqual((short)-1, owner.LastPackets.OfType<PetsrPacket>().Single().Slot, "nothing of a new bar is cooling down");
        }

        [TestMethod]
        public async Task ASlotThatIsCoolingDownIsNotCalledReady()
        {
            var owner = await World.CreateSessionAsync();
            var pet = await PetOutAsync(owner, TestMates.Pet(owner.Character.CharacterId, vnum: Otter));
            pet.Entity.NotNull().PendingResets[1] = World.Clock.GetCurrentInstant().Plus(Duration.FromSeconds(35));

            await _service.RefreshAsync(owner.Character);

            Assert.AreEqual((663, 664, -1), Shown(owner));
            Assert.IsFalse(owner.LastPackets.OfType<PetsrPacket>().Any());
        }

        [TestMethod]
        public async Task WithoutATeamPetAtTheirSideTheOwnerHasNoBar()
        {
            var owner = await World.CreateSessionAsync();
            await PetOutAsync(owner, TestMates.Pet(owner.Character.CharacterId, isTeamMember: false, vnum: Otter));
            await PetOutAsync(owner, TestMates.Partner(owner.Character.CharacterId));

            await _service.RefreshAsync(owner.Character);

            Assert.AreEqual((-1, -1, -1), Shown(owner));
            Assert.IsFalse(owner.LastPackets.OfType<PetsrPacket>().Any());
        }

        [TestMethod]
        public async Task ATeamPetWithoutSkillsOfItsOwnHasNoBar()
        {
            var owner = await World.CreateSessionAsync();
            await PetOutAsync(owner, TestMates.Pet(owner.Character.CharacterId, vnum: Wolf));

            await _service.RefreshAsync(owner.Character);

            Assert.AreEqual((-1, -1, -1), Shown(owner));
            Assert.IsFalse(owner.LastPackets.OfType<PetsrPacket>().Any());
        }

        private static async Task<Mate> PetOutAsync(ClientSession owner, Mate mate)
        {
            owner.Character.Mates[mate.MateTransportId] = mate;
            await mate.SpawnAsync(owner.Character, owner.Character.MapInstance, 5, 5);
            return mate;
        }

        private static (int, int, int) Shown(ClientSession owner)
        {
            var bar = owner.LastPackets.OfType<PetskiPacket>().Last();
            return (bar.FirstSkillVNum, bar.SecondSkillVNum, bar.ThirdSkillVNum);
        }

        private short[] Bar(Mate mate) => _service.Skills(mate).Select(s => s.SkillVNum).ToArray();
    }
}
