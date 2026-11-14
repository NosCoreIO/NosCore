//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class LevPacketTests
    {
        private TestWorld World;
        private ClientSession _session;
        private readonly SpExperienceService _spExperience = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _session.Character.JobLevel = 20;
            _session.Character.JobLevelXp = 7;
            var card = _session.Character.InventoryService
                .AddItemToPocket(InventoryItemInstance.Create(World.GenerateItemProvider().Create(912, 1), _session.Character.CharacterId)).NotNull()
                .First();
            card.Type = NoscorePocketType.Wear;
            card.Slot = (byte)EquipmentType.Sp;
            var sp = (SpecialistInstance)card.ItemInstance;
            sp.SpLevel = 45;
            sp.Xp = 1234;
        }

        [TestMethod]
        public void ATransformedPlayerSeesTheSpecialistLevelInTheJobSlots()
        {
            _session.Character.UseSp = true;

            var lev = Lev();

            Assert.AreEqual((byte)45, lev.JobLevel);
            Assert.AreEqual(1234, lev.JobLevelXp);
            Assert.AreEqual(_spExperience.GetSpExperience(45, isSecondarySp: false), lev.JobXpLoad);
        }

        [TestMethod]
        public void AnUntransformedPlayerSeesTheJobLevel()
        {
            var lev = Lev();

            Assert.AreEqual((byte)20, lev.JobLevel);
            Assert.AreEqual(7, lev.JobLevelXp);
        }

        private LevPacket Lev() => _session.Character.GenerateLev(new ExperienceService(), new JobExperienceService(),
            new HeroExperienceService(), _spExperience, new SkillPointService());
    }
}
