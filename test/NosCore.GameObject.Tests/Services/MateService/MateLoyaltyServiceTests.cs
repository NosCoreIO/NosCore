//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Mates;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateLoyaltyServiceTests
    {
        private TestWorld World;
        private ClientSession _session;
        private GameObject.Services.MateService.Mate _pet;
        private Mock<IRandomProvider> _random;
        private Mock<IMinilandRegistry> _minilands;
        private MateLoyaltyService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            _random = World.Freeze<Mock<IRandomProvider>>();
            _minilands = World.Freeze<Mock<IMinilandRegistry>>();
            _service = World.Create<MateLoyaltyService>();
            _session.LastPackets.Clear();
        }

        [TestMethod]
        public async Task AnUnluckyHitCostsSomeLoyalty()
        {
            await new Spec("A hit that rolls under the loss chance costs the rolled loyalty")
                .Given(ThePetHas_Loyalty, (short)1000)
                .And(TheRollsAre_And_, 1, 4)
                .WhenAsync(ThePetIsHitAsync)
                .Then(ThePetShouldHave_Loyalty, (short)996)
                .And(TheOwnerSeesTheLoyalty_, (short)996)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MostHitsCostNothing()
        {
            await new Spec("A hit that rolls over the loss chance leaves loyalty alone")
                .Given(ThePetHas_Loyalty, (short)1000)
                .And(TheRollsAre_And_, 2, 4)
                .WhenAsync(ThePetIsHitAsync)
                .Then(ThePetShouldHave_Loyalty, (short)1000)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AKnockOutEmptiesLowLoyaltyAndWarnsTheOwner()
        {
            await new Spec("A knock-out costs 50 loyalty, stops at 0 and tells the owner to feed the pet")
                .Given(ThePetHas_Loyalty, (short)30)
                .WhenAsync(ThePetIsKnockedOutAsync)
                .Then(ThePetShouldHave_Loyalty, (short)0)
                .And(TheOwnerWasTold_, Game18NConstString.PetLoyaltyIsEmpty)
                .And(TheOwnerWasTold_, Game18NConstString.FeedPetOrGoToMiniland)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheOwnersMinilandWinsLoyaltyBack()
        {
            await new Spec("While the owner is in their own Miniland, pets win back 100 loyalty every 5 seconds")
                .Given(ThePetHas_Loyalty, (short)0)
                .And(TheOwnerStandsInTheirMiniland)
                .WhenAsync(TheMapTicksAsync)
                .AndAsync(TheMapTicksAsync)
                .Then(ThePetShouldHave_Loyalty, (short)100)
                .WhenAsync(TheMapTicksAfter_SecondsAsync, 5)
                .Then(ThePetShouldHave_Loyalty, (short)200)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OtherMapsWinNothingBack()
        {
            await new Spec("Away from the owner's Miniland loyalty does not come back")
                .Given(ThePetHas_Loyalty, (short)0)
                .WhenAsync(TheMapTicksAsync)
                .Then(ThePetShouldHave_Loyalty, (short)0)
                .ExecuteAsync();
        }

        private void ThePetHas_Loyalty(short loyalty) => _pet.Loyalty = loyalty;

        private void TheRollsAre_And_(int chance, int amount)
        {
            _random.Setup(r => r.Next(0, 100)).Returns(chance);
            _random.Setup(r => r.Next(MateLoyaltyService.MinHitLoss, MateLoyaltyService.MaxHitLoss + 1)).Returns(amount);
        }

        private void TheOwnerStandsInTheirMiniland()
        {
            var mapInstanceId = _session.Character.MapInstanceId;
            _minilands.Setup(m => m.GetByMapInstanceId(mapInstanceId))
                .Returns(new Miniland { OwnerId = _session.Character.CharacterId, MapInstanceId = mapInstanceId });
        }

        private Task ThePetIsHitAsync() => _service.HitAsync(_session.Character, _pet);

        private Task ThePetIsKnockedOutAsync() => _service.KnockedOutAsync(_session.Character, _pet);

        private Task TheMapTicksAsync() => _service.TickAsync(_session.Character.MapInstance);

        private Task TheMapTicksAfter_SecondsAsync(int seconds)
        {
            World.Clock.AdvanceSeconds(seconds);
            return _service.TickAsync(_session.Character.MapInstance);
        }

        private void ThePetShouldHave_Loyalty(short loyalty) => Assert.AreEqual(loyalty, _pet.Loyalty);

        private void TheOwnerSeesTheLoyalty_(short loyalty) =>
            Assert.AreEqual(loyalty, _session.LastPackets.OfType<ScpPacket>().Last().Loyalty);

        private void TheOwnerWasTold_(Game18NConstString message) =>
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == message));
    }
}
