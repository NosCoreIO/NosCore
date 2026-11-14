//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using SpecLight;
using LandOfDeath = NosCore.GameObject.Services.LandOfDeathService.LandOfDeathService;

namespace NosCore.GameObject.Tests.Services.LandOfDeathService
{
    [TestClass]
    public class LandOfDeathServiceTests
    {
        private TestWorld World;
        private LandOfDeath _service;
        private ClientSession _session;
        private bool _canEnter;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _service = World.Create<LandOfDeath>();
            _session = await World.CreateSessionAsync();
            _session.Character.MapInstance = World.GetBaseMap(0);
            _session.Character.Level = 60;
        }

        [TestMethod]
        public async Task OnlyFamilyMembersMayEnter()
        {
            await new Spec("A player without a family is told only family members can enter")
                .Given(TheLandOfDeathIsOpen)
                .WhenAsync(CheckingTheEntrance)
                .Then(TheyShouldBeTurnedAwayWith_, Game18NConstString.NeedFamilyForLandOfDeath)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task NobodyEntersWhileTheGateIsClosed()
        {
            await new Spec("A family member cannot enter while the Land of Death is closed")
                .Given(TheCharacterIsInAFamily)
                .WhenAsync(CheckingTheEntrance)
                .Then(TheyShouldBeTurnedAwayWith_, Game18NConstString.NotAuthorised)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PlayersUnderLevelFiftyFiveCannotEnter()
        {
            await new Spec("A family member under level 55 is told the level they need")
                .Given(TheCharacterIsInAFamily)
                .And(TheCharacterIsLevel_, (byte)54)
                .And(TheLandOfDeathIsOpen)
                .WhenAsync(CheckingTheEntrance)
                .Then(TheyShouldBeTurnedAwayWith_, Game18NConstString.TooLowLevelToEntry)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFamilyMemberOfLevelFiftyFiveEnters()
        {
            await new Spec("A level 55 family member may enter while the gate is open")
                .Given(TheCharacterIsInAFamily)
                .And(TheCharacterIsLevel_, (byte)55)
                .And(TheLandOfDeathIsOpen)
                .WhenAsync(CheckingTheEntrance)
                .Then(TheyShouldBeAllowedIn)
                .ExecuteAsync();
        }

        private void TheCharacterIsInAFamily() => _session.Character.Family = new Family { FamilyId = 1, Name = "Testers" };

        private void TheCharacterIsLevel_(byte level) => _session.Character.Level = level;

        private void TheLandOfDeathIsOpen()
        {
            Assert.IsTrue(_service.TryStart());
            Assert.IsTrue(SpinWait.SpinUntil(() => _service.IsGateOpen, TimeSpan.FromSeconds(5)));
        }

        private async Task CheckingTheEntrance() => _canEnter = await _service.CanEnterAsync(_session);

        private void TheyShouldBeTurnedAwayWith_(Game18NConstString message)
        {
            Assert.IsFalse(_canEnter);
            Assert.IsTrue(_session.LastPackets.OfType<MsgiPacket>().Any(p => p.Message == message));
        }

        private void TheyShouldBeAllowedIn()
        {
            Assert.IsTrue(_canEnter);
            Assert.IsFalse(_session.LastPackets.OfType<MsgiPacket>().Any());
        }
    }
}
