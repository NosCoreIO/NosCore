//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.MateExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateProgressionServiceTests
    {
        private TestWorld World;
        private readonly MateExperienceService _curve = new();
        private MateProgressionService _service;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.Level = 30;
            _service = new MateProgressionService(_curve);
        }

        private Mate AddPet(bool isTeamMember = true, byte level = 10)
        {
            var pet = TestMates.Pet(_session.Character.CharacterId, isTeamMember, level);
            _session.Character.Mates[pet.MateTransportId] = pet;
            return pet;
        }

        [TestMethod]
        public async Task ATeamPetGainsTheExperience()
        {
            var pet = AddPet();

            await _service.AddExperienceAsync(_session.Character, 5);

            Assert.AreEqual(5, pet.Experience);
            Assert.AreEqual(10, pet.Level);
        }

        [TestMethod]
        public async Task APetLeftAtHomeGainsNothing()
        {
            var pet = AddPet(isTeamMember: false);

            await _service.AddExperienceAsync(_session.Character, 5);

            Assert.AreEqual(0, pet.Experience);
        }

        [TestMethod]
        public async Task FillingTheBarLevelsUpAndRefills()
        {
            var pet = AddPet();
            pet.Hp = 1;
            var bar = _curve.GetPetExperience(10);

            await _service.AddExperienceAsync(_session.Character, bar + 3);

            Assert.AreEqual(11, pet.Level);
            Assert.AreEqual(3, pet.Experience);
            Assert.AreEqual(_curve.GetPetExperience(11), pet.XpLoad);
            Assert.AreEqual(pet.MaxHp, pet.Hp);
        }

        [TestMethod]
        public async Task APartnerUsesThePartnerCurve()
        {
            var partner = TestMates.Partner(_session.Character.CharacterId);
            partner.Level = 10;
            _session.Character.Mates[partner.MateTransportId] = partner;

            await _service.AddExperienceAsync(_session.Character, _curve.GetPetExperience(10));

            Assert.AreEqual(10, partner.Level, "a partner's bar is four times a pet's");
        }

        [TestMethod]
        public async Task AMateStaysBelowItsOwner()
        {
            var pet = AddPet(level: 29);

            await _service.AddExperienceAsync(_session.Character, _curve.GetPetExperience(29) * 3);

            Assert.AreEqual(29, pet.Level);
            Assert.AreEqual(_curve.GetPetExperience(29), pet.Experience, "the bar stays full until the owner levels");
        }

        [TestMethod]
        public async Task TheWindowIsRefreshed()
        {
            var pet = AddPet();

            await _service.AddExperienceAsync(_session.Character, 5);

            Assert.AreEqual(5, _session.LastPackets.OfType<ScpPacket>().Last().Experience);
            Assert.AreEqual(pet.MateTransportId, _session.LastPackets.OfType<ScpPacket>().Last().TransportId);
        }
    }
}
