//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.MateExperienceService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Groups;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Mate = NosCore.GameObject.Services.MateService.Mate;
using MateServiceImpl = NosCore.GameObject.Services.MateService.MateService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CaptureServiceTests
    {
        private ClientSession _session = null!;
        private MonsterComponentBundle _monster;
        private Mock<IRandomProvider> _random = null!;
        private CaptureService _service = null!;

        private static readonly SkillInfo Capture = new(209, 16, 150, 15, -1, 0, 0, default, 1, 0, 0, 0, 0, 0,
            new List<BCardDto>
            {
                new() { Type = BCardEffect.CaptureCaptureAnimal.Type(), SubType = BCardEffect.CaptureCaptureAnimal.SubType() }
            });

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            var map = _session.Character.MapInstance;
            var name = new I18NString();
            name[RegionType.EN] = "Chicken";
            var creature = new NpcMonsterDto { NpcMonsterVNum = 333, Level = 1, MaxHp = 100, MaxMp = 10, Name = name };
            _monster = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9001, creature, map, 12, 14, 2, 12, 14, false, true, false), map.EcsWorld);
            _monster.Hp = 40;

            _random = new Mock<IRandomProvider>();
            _random.Setup(r => r.Next(0, 100)).Returns(0);
            var mateService = new MateServiceImpl(TestHelpers.Instance.MateDao, new List<NpcMonsterDto> { creature },
                new IdService<Mate>(2000000), new MateExperienceService(), NullLogger<MateServiceImpl>.Instance);
            _service = new CaptureService(TestHelpers.Instance.MateDao, mateService,
                new Mock<Wolverine.IMessageBus>().Object, TestHelpers.Instance.Clock, _random.Object);

            map.LastPackets.Clear();
            _session.LastPackets.Clear();
        }

        private Task Catching() => _service.TryCaptureAsync(_session.Character, _monster, Capture);

        private Mate? Caught => _session.Character.Mates.Values.SingleOrDefault(s => s.VNum == 333);

        [TestMethod]
        public async Task TheFirstPetComesOutWhereTheMonsterStood()
        {
            await Catching();

            Assert.IsNotNull(Caught);
            Assert.IsTrue(Caught!.IsTeamMember);
            Assert.AreEqual(3000, Caught.Loyalty, "every fresh catch in the recording starts at 3000");
            Assert.IsTrue(Caught.MateId > 0, "the live pet keeps the row id so a save updates it");
            var spawn = _session.Character.MapInstance.LastPackets.OfType<InPacket>().Single(s => s.VisualId == Caught.MateTransportId);
            Assert.AreEqual(((short)12, (short)14), (spawn.PositionX, spawn.PositionY));
        }

        [TestMethod]
        public async Task ASuccessfulCaptureTellsTheOwnerInTheRecordedOrder()
        {
            await Catching();

            var order = _session.LastPackets
                .Where(p => p is CtlPacket or MsgiPacket or PclearPacket or PinitPacket)
                .Select(p => p!.GetType().Name)
                .ToArray();
            CollectionAssert.AreEqual(
                new[] { nameof(CtlPacket), nameof(MsgiPacket), nameof(PclearPacket), nameof(PinitPacket), nameof(PclearPacket) },
                order);
            Assert.AreEqual(Game18NConstString.CaptureSuccessful, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
            Assert.IsTrue(_session.Character.MapInstance.LastPackets.OfType<EffectPacket>().Any(e => e.Id == 197));
        }

        [TestMethod]
        public async Task WithAPetAlreadyOutTheNewOneStaysInTheList()
        {
            var alreadyOut = TestMates.Pet(_session.Character.CharacterId, vnum: 200);
            _session.Character.Mates[alreadyOut.MateTransportId] = alreadyOut;

            await Catching();

            Assert.IsFalse(Caught!.IsTeamMember);
            Assert.IsFalse(_session.LastPackets.OfType<PinitPacket>().Any());
            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<InPacket>().Any());
        }

        [TestMethod]
        public async Task ASecondCaptureLeavesTheFirstPetOut()
        {
            await Catching();
            var map = _session.Character.MapInstance;
            var second = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9002, _monster.NpcMonster!, map, 3, 3, 2, 3, 3, false, true, false), map.EcsWorld);
            second.Hp = 40;

            await _service.TryCaptureAsync(_session.Character, second, Capture);

            Assert.AreEqual(2, _session.Character.Mates.Count);
            Assert.AreEqual(1, _session.Character.Mates.Values.Count(s => s.IsTeamMember));
        }

        [TestMethod]
        public async Task AFailedRollSaysSoAndCatchesNothing()
        {
            _random.Setup(r => r.Next(0, 100)).Returns(95);

            await Catching();

            Assert.IsNull(Caught);
            Assert.AreEqual(Game18NConstString.CaptureFailed, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task AMonsterAboveTheOwnersLevelIsRefused()
        {
            _session.Character.Level = 1;
            var map = _session.Character.MapInstance;
            var strong = new NpcMonsterDto { NpcMonsterVNum = 334, Level = 5, MaxHp = 100, Name = _monster.NpcMonster!.Name };
            var monster = new MonsterComponentBundle(
                map.EcsWorld.CreateMonster(9003, strong, map, 3, 3, 2, 3, 3, false, true, false), map.EcsWorld);
            monster.Hp = 10;

            await _service.TryCaptureAsync(_session.Character, monster, Capture);

            Assert.AreEqual(Game18NConstString.CaptureToHighLevel, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }

        [TestMethod]
        public async Task AHealthyMonsterIsRefused()
        {
            _monster.Hp = 60;

            await Catching();

            Assert.IsNull(Caught);
            Assert.AreEqual(Game18NConstString.CaptureTooMuchLife, _session.LastPackets.OfType<MsgiPacket>().Single().Message);
        }
    }
}
