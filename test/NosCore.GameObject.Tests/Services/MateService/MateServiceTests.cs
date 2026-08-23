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
using System.Linq;
using System.Threading.Tasks;
using Mate = NosCore.GameObject.Services.MateService.Mate;
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
    }
}
