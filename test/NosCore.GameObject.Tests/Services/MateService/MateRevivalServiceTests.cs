//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateRevivalServiceTests
    {
        private const short SeedOfPowerVNum = 1012;
        private TestWorld World;
        private ClientSession _session;
        private GameObject.Services.MateService.Mate _pet;
        private MateRevivalService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 7, 9);
            World.Inject(UpgradeMaterials.Catalog());
            _service = World.Create<MateRevivalService>();
            _session.LastPackets.Clear();
            _session.Character.MapInstance.LastPackets.Clear();
        }

        [TestMethod]
        public async Task FiveSeedsBringTheNosMateBackAfterThreeMinutes()
        {
            await new Spec("Five seeds bring the NosMate back after three minutes")
                .Given(TheOwnerHas_Seeds, (short)7)
                .WhenAsync(TheNosMateDies)
                .Then(TheNosMateLeftTheMap)
                .And(TheOwnerHas_SeedsLeft, 2)
                .And(TheOwnerWasTold_, Game18NConstString.NosmateDeadThreeMinutes)
                .And(TheNosMateIsStillInTheTeam)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNosMateComesBackWithFullHealthOnceTheDelayIsOver()
        {
            await new Spec("The NosMate comes back with full health once the delay is over")
                .Given(TheOwnerHas_Seeds, (short)5)
                .AndAsync(TheNosMateDies)
                .WhenAsync(TheMapTicksAfter_Seconds, 180)
                .Then(TheNosMateIsBackWithFullHealth)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheNosMateStaysAwayBeforeTheDelayIsOver()
        {
            await new Spec("The NosMate stays away before the delay is over")
                .Given(TheOwnerHas_Seeds, (short)5)
                .AndAsync(TheNosMateDies)
                .WhenAsync(TheMapTicksAfter_Seconds, 179)
                .Then(TheNosMateLeftTheMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutFiveSeedsTheNosMateGoesHome()
        {
            await new Spec("Without five seeds the NosMate goes home")
                .Given(TheOwnerHas_Seeds, (short)4)
                .WhenAsync(TheNosMateDies)
                .Then(TheNosMateLeftTheMap)
                .And(TheOwnerHas_SeedsLeft, 4)
                .And(TheOwnerWasTold_, Game18NConstString.NosMateDeadNoMorePowerSeed)
                .And(TheNosMateIsNoLongerInTheTeam)
                .ExecuteAsync();
        }

        private void TheOwnerHas_Seeds(short amount)
        {
            var item = new Item
            {
                VNum = SeedOfPowerVNum,
                Type = NoscorePocketType.Main,
                ItemType = ItemType.Main,
                Material = MaterialType.SeedOfPower,
            };
            var instance = new ItemInstance(item) { Amount = amount };
            var stack = InventoryItemInstance.Create(instance, _session.Character.CharacterId);
            stack.Slot = 0;
            stack.Type = NoscorePocketType.Main;
            _session.Character.InventoryService[stack.ItemInstanceId] = stack;
        }

        private Task TheNosMateDies() => _service.KnockOutAsync(_session.Character, _pet);

        private Task TheMapTicksAfter_Seconds(int seconds)
        {
            World.Clock.AdvanceSeconds(seconds);
            return _service.TickAsync(_session.Character.MapInstance);
        }

        private void TheNosMateLeftTheMap()
        {
            Assert.IsNull(_pet.Entity);
            Assert.IsTrue(_session.Character.MapInstance.LastPackets.OfType<OutPacket>().Any(s => s.VisualId == _pet.MateTransportId));
        }

        private void TheOwnerHas_SeedsLeft(int amount) =>
            Assert.AreEqual(amount, _session.Character.InventoryService.CountItem(SeedOfPowerVNum));

        private void TheOwnerWasTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(s => s.Message == message));

        private void TheNosMateIsStillInTheTeam() => Assert.IsTrue(_pet.IsTeamMember);

        private void TheNosMateIsNoLongerInTheTeam()
        {
            Assert.IsFalse(_pet.IsTeamMember);
            Assert.IsNull(_pet.RevivesAt);
        }

        private void TheNosMateIsBackWithFullHealth()
        {
            Assert.IsNotNull(_pet.Entity);
            Assert.IsNull(_pet.RevivesAt);
            Assert.AreEqual(_pet.MaxHp, _pet.Entity.NotNull().Hp);
        }
    }
}
