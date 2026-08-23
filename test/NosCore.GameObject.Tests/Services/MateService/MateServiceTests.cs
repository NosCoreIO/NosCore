//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using NosCore.Packets.ServerPackets.Visibility;
using System.Linq;
using System.Threading.Tasks;
using Mate = NosCore.GameObject.Services.MateService.Mate;
using MatePlacement = NosCore.GameObject.Services.MateService.MatePlacement;
using MateServiceImpl = NosCore.GameObject.Services.MateService.MateService;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateServiceTests
    {
        private const short ChickenVNum = 333;
        private const short PartnerVNum = 317;
        private const long CharacterId = 42;

        private static NpcMonsterDto Creature(short vNum, string name, int maxHp, int maxMp)
        {
            var i18N = new I18NString();
            i18N[RegionType.EN] = name;
            return new NpcMonsterDto
            {
                NpcMonsterVNum = vNum,
                Name = i18N,
                Level = 1,
                MaxHp = maxHp,
                MaxMp = maxMp
            };
        }

        private static MateServiceImpl Build(IEnumerable<MateDto> rows, params NpcMonsterDto[] creatures)
        {
            var dao = new Mock<IDao<MateDto, long>>();
            dao.Setup(s => s.Where(It.IsAny<System.Linq.Expressions.Expression<System.Func<MateDto, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<System.Func<MateDto, bool>> predicate) =>
                    rows.Where(predicate.Compile()));

            return new MateServiceImpl(dao.Object, creatures.ToList(),
                new IdService<Mate>(2000000), NullLogger<MateServiceImpl>.Instance);
        }

        [TestMethod]
        public async Task LoadingAttachesTheCreatureAndGivesEachMateItsOwnTransportIdAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, Level = 1 },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, Level = 1 }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            var mates = await service.LoadAsync(CharacterId);

            Assert.AreEqual(2, mates.Count);
            Assert.IsTrue(mates.All(s => s.NpcMonster.NpcMonsterVNum == ChickenVNum),
                "a mate without its creature attached cannot say its own name");
            Assert.AreNotEqual(mates[0].MateTransportId, mates[1].MateTransportId,
                "two mates sharing a transport id means the client addresses the wrong one");
        }

        [TestMethod]
        public async Task PetsAndPartnersAreNumberedSeparatelyFromZeroAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet },
                new MateDto { MateId = 3, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet }
            }, Creature(ChickenVNum, "Chicken", 157, 10), Creature(PartnerVNum, "Bob", 870, 200));

            var mates = await service.LoadAsync(CharacterId);

            CollectionAssert.AreEqual(new byte[] { 0, 1 },
                mates.Where(s => s.MateType == MateType.Pet).Select(s => s.PetSlot).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0 },
                mates.Where(s => s.MateType == MateType.Partner).Select(s => s.PetSlot).ToArray());
        }

        [TestMethod]
        public async Task ARowPointingAtAnUnknownCreatureIsSkippedRatherThanSentAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = 9999, MateType = MateType.Pet }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            Assert.AreEqual(0, (await service.LoadAsync(CharacterId)).Count);
        }

        [TestMethod]
        public async Task AnotherCharactersMatesAreNotLoadedAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet },
                new MateDto { MateId = 2, CharacterId = CharacterId + 1, VNum = ChickenVNum, MateType = MateType.Pet }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            var mates = await service.LoadAsync(CharacterId);

            Assert.AreEqual(1, mates.Count);
            Assert.AreEqual(1L, mates[0].MateId);
        }

        [TestMethod]
        public async Task PetsGetScpAndPartnersGetScnAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner }
            }, Creature(ChickenVNum, "Chicken", 157, 10), Creature(PartnerVNum, "Bob", 870, 200));

            var packets = MateServiceImpl
                .GenerateScPackets(await service.LoadAsync(CharacterId), RegionType.EN).ToList();

            Assert.AreEqual(1, packets.OfType<ScpPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ScnPacket>().Count());
        }

        [TestMethod]
        public async Task TheCreatureNameIsUsedWhenTheMateWasNeverRenamedAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet }
            }, Creature(ChickenVNum, "Joyeux Mouton", 157, 10));

            var packet = (await service.LoadAsync(CharacterId))[0].GenerateScp(RegionType.EN);

            // The serializer turns the space into a caret on the way out; the packet itself
            // carries the name as it is.
            Assert.AreEqual("Joyeux Mouton", packet.Name);
        }

        [TestMethod]
        public async Task ARenamedMateKeepsItsOwnNameAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, Name = "Poule" }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            Assert.AreEqual("Poule", (await service.LoadAsync(CharacterId))[0].GenerateScp(RegionType.EN).Name);
        }

        [TestMethod]
        public async Task ScpReportsTheExperienceTheCaptureReportsAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, Level = 3 }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            var packet = (await service.LoadAsync(CharacterId))[0].GenerateScp(RegionType.EN);

            Assert.AreEqual(90L, packet.XpLoad);
        }

        [TestMethod]
        public async Task TheSpawnPacketMarksTheMateAsBelongingToItsOwnerAsync()
        {
            // in 2 1506 445562 26 26 2 100 100 0 0 3 626114 1 0 -1 Ratufu^pirate^(Feu) 0 -1 ...
            // Owner and GroupEffect are what separate a mate from a map npc; without them the
            // client draws it as scenery and will not let the owner command it.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, Hp = 78, Mp = 5 }
            }, Creature(ChickenVNum, "Chicken", 156, 10));

            var mate = (await service.LoadAsync(CharacterId))[0];
            mate.PositionX = 26;
            mate.PositionY = 26;
            var packet = mate.GenerateIn(RegionType.EN);

            Assert.AreEqual(VisualType.Npc, packet.VisualType);
            Assert.AreEqual(CharacterId, packet.InNonPlayerSubPacket!.Owner);
            Assert.AreEqual(3, packet.InNonPlayerSubPacket.GroupEffect);
            Assert.AreEqual(mate.MateTransportId, packet.VisualId);
            Assert.AreEqual(26, packet.PositionX);
            Assert.AreEqual(50, packet.InNonPlayerSubPacket.InAliveSubPacket!.Hp,
                "the spawn carries health as a percentage, not as points");
        }

        [TestMethod]
        public async Task APartnerIsFlaggedDifferentlyFromAPetOnSpawnAsync()
        {
            // Both partners in the capture carry 1 after the name where every pet carries 0.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner }
            }, Creature(ChickenVNum, "Chicken", 157, 10), Creature(PartnerVNum, "Bob", 870, 200));

            var mates = await service.LoadAsync(CharacterId);

            Assert.AreEqual(0, mates.Single(s => s.MateType == MateType.Pet)
                .GenerateIn(RegionType.EN).InNonPlayerSubPacket!.Unknow1);
            Assert.AreEqual(1, mates.Single(s => s.MateType == MateType.Partner)
                .GenerateIn(RegionType.EN).InNonPlayerSubPacket!.Unknow1);
        }

        [TestMethod]
        public async Task TheHealthBarCarriesTheMateTypeWhereAPlayerCarriesAPartyPositionAsync()
        {
            // pst 2 22687 0 100 100 24471 3100 0 0 0 — the third field is the mate type.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner, Hp = 435, Mp = 100 }
            }, Creature(PartnerVNum, "Bob", 870, 200));

            var packet = (await service.LoadAsync(CharacterId))[0].GeneratePst();

            Assert.AreEqual(VisualType.Npc, packet.Type);
            Assert.AreEqual((int)MateType.Partner, packet.GroupOrder);
            Assert.AreEqual(50, packet.HpLeft);
            Assert.AreEqual(870, packet.HpLoad);
        }

        [TestMethod]
        public async Task ADespawnNamesTheSameIdTheSpawnDidAsync()
        {
            // A mismatch here leaves the pet drawn on everybody else's screen for ever, and
            // nothing throws.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            var mate = (await service.LoadAsync(CharacterId))[0];

            Assert.AreEqual(mate.GenerateIn(RegionType.EN).VisualId, mate.GenerateOut().VisualId);
        }

        [TestMethod]
        public async Task OnlyOneMateOfEachTypeIsEverOutAsync()
        {
            // Two rows can claim the slot — two captures racing, or a database edited by hand —
            // and the second would spawn on top of the first with nothing raised anywhere.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true },
                new MateDto { MateId = 3, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner, IsTeamMember = true }
            }, Creature(ChickenVNum, "Chicken", 157, 10), Creature(PartnerVNum, "Bob", 870, 200));

            var mates = await service.LoadAsync(CharacterId);

            Assert.AreEqual(1, mates.Count(s => s.MateType == MateType.Pet && s.IsTeamMember));
            Assert.AreEqual(1, mates.Count(s => s.MateType == MateType.Partner && s.IsTeamMember),
                "a pet and a partner are two different slots and both may be out");
            Assert.AreEqual(1L, mates.Single(s => s.MateType == MateType.Pet && s.IsTeamMember).MateId,
                "the first row keeps the slot, so which mate is out does not change between logins");
        }

        [TestMethod]
        public async Task AMateThatIsNotOutStaysInTheListAsync()
        {
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            Assert.AreEqual(2, (await service.LoadAsync(CharacterId)).Count);
        }

        private static GameObject.Map.Map OpenGround()
        {
            return new GameObject.Map.Map
            {
                MapId = 1,
                NameI18NKey = "openGround",
                Data = [8, 0, 8, 0, .. new byte[64]]
            };
        }

        [TestMethod]
        public async Task TwoMatesNeverStandOnTheSameSquareAsync()
        {
            // A character can have a pet and a partner out at once. Giving both the same offset
            // stacks them, and nothing complains.
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true },
                new MateDto { MateId = 2, CharacterId = CharacterId, VNum = PartnerVNum, MateType = MateType.Partner, IsTeamMember = true }
            }, Creature(ChickenVNum, "Chicken", 157, 10), Creature(PartnerVNum, "Bob", 870, 200));

            var mates = await service.LoadAsync(CharacterId);
            MatePlacement.Arrange(4, 4, OpenGround(), mates);

            Assert.AreEqual(2, mates.Select(s => (s.PositionX, s.PositionY)).Distinct().Count());
        }

        [TestMethod]
        public async Task AMateIsNeverPlacedInsideAWallAsync()
        {
            var walled = new GameObject.Map.Map
            {
                MapId = 1,
                NameI18NKey = "walled",
                // Four by four, everything solid but the two squares on the top row.
                Data = [4, 0, 4, 0,
                    0, 0, 1, 1,
                    1, 1, 1, 1,
                    1, 1, 1, 1,
                    1, 1, 1, 1]
            };
            var service = Build(new[]
            {
                new MateDto { MateId = 1, CharacterId = CharacterId, VNum = ChickenVNum, MateType = MateType.Pet, IsTeamMember = true }
            }, Creature(ChickenVNum, "Chicken", 157, 10));

            var mates = await service.LoadAsync(CharacterId);
            MatePlacement.Arrange(0, 0, walled, mates);

            var mate = mates[0];
            Assert.IsTrue(walled.IsWalkable(mate.PositionX, mate.PositionY),
                $"placed at {mate.PositionX},{mate.PositionY}, which is not walkable");
        }
    }
}
