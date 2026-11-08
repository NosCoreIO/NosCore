//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MateService;
using NosCore.PacketHandlers.Mates;
using NosCore.Packets.ClientPackets.Mates;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Mates
{
    [TestClass]
    public class SayPPacketHandlerTests
    {
        private SayPPacketHandler _handler = null!;
        private ClientSession _session = null!;
        private ClientSession _otherSession = null!;
        private MapInstance _map = null!;
        private Mate _pet = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _otherSession = await TestHelpers.Instance.GenerateSessionAsync();
            _map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            _session.Character.MapInstance = _map;
            _otherSession.Character.MapInstance = _map;
            _handler = new SayPPacketHandler(MateControl.Service());
        }

        [TestMethod]
        public async Task AnOwnPetSaysTheLine()
        {
            await new Spec("An own pet out on the map says what its owner typed")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerMakesItSay_, "good boy")
                .Then(TheMapShouldSeeThePetSay_, "good boy")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherPlayersPetStaysQuiet()
        {
            await new Spec("A say_p naming another player's pet says nothing")
                .GivenAsync(APetOf_IsOut, "other player")
                .WhenAsync(TheOwnerMakesItSay_, "good boy")
                .Then(NothingShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnEmptyLineIsIgnored()
        {
            await new Spec("A pet given an empty line says nothing")
                .GivenAsync(APetOf_IsOut, "owner")
                .WhenAsync(TheOwnerMakesItSay_, " ")
                .Then(NothingShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AMateKeptInTheMinilandStaysQuiet()
        {
            await new Spec("A mate that is not in the team says nothing")
                .Given(APetKeptInTheMiniland)
                .WhenAsync(TheOwnerMakesItSay_, "good boy")
                .Then(NothingShouldBeSaid)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHiddenOwnersPetOnlyTalksToTheOwner()
        {
            await new Spec("An invisible owner's pet speaks to the owner alone")
                .GivenAsync(APetOf_IsOut, "owner")
                .And(TheOwnerIsInvisible)
                .WhenAsync(TheOwnerMakesItSay_, "good boy")
                .Then(NothingShouldBeSaid)
                .And(TheOwnerShouldSeeThePetSay)
                .ExecuteAsync();
        }

        private async Task APetOf_IsOut(string whose)
        {
            var owner = whose == "owner" ? _session : _otherSession;
            _pet = TestMates.Pet(owner.Character.CharacterId);
            owner.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(owner.Character, _map, 5, 5);
            _map.LastPackets.Clear();
            _session.LastPackets.Clear();
        }

        private void APetKeptInTheMiniland()
        {
            _pet = TestMates.Pet(_session.Character.CharacterId, isTeamMember: false);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
        }

        private void TheOwnerIsInvisible()
        {
            _session.Character.Invisible = true;
        }

        private Task TheOwnerMakesItSay_(string message) =>
            _handler.ExecuteAsync(new SayPPacket { PetId = _pet.MateTransportId, Message = message }, _session);

        private void TheMapShouldSeeThePetSay_(string message)
        {
            var say = _map.LastPackets.OfType<SayPacket>().Single();
            Assert.AreEqual((VisualType.Npc, _pet.MateTransportId, message), (say.VisualType, say.VisualId, say.Message));
        }

        private void NothingShouldBeSaid()
        {
            Assert.IsFalse(_map.LastPackets.OfType<SayPacket>().Any());
        }

        private void TheOwnerShouldSeeThePetSay()
        {
            Assert.IsTrue(_session.LastPackets.OfType<SayPacket>().Any(s => s.VisualId == _pet.MateTransportId));
        }
    }
}
