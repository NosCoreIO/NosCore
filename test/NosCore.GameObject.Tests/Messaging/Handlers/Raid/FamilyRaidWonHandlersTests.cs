//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Family;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using Act4RaidType = NosCore.Packets.Enumerations.Act4RaidType;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Raid
{
    [TestClass]
    public class FamilyRaidWonHandlersTests
    {
        private TestWorld World;
        private Mock<IFamilyContributionService> _contributions;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _contributions = World.Freeze<Mock<IFamilyContributionService>>();
        }

        [TestMethod]
        public async Task EachFamilyIsCreditedOnceWithItsOwnWinners()
        {
            await World.Create<FamilyRaidWonHandler>().Handle(new RaidWonEvent(
                [new RaidWinner(1, 10), new RaidWinner(2, 20), new RaidWinner(1, 11)], 7, true));

            _contributions.Verify(s => s.RaidWonAsync(1, It.Is<IReadOnlyCollection<long>>(ids =>
                ids.SequenceEqual(new long[] { 10, 11 })), 7, true), Times.Once);
            _contributions.Verify(s => s.RaidWonAsync(2, It.Is<IReadOnlyCollection<long>>(ids =>
                ids.SequenceEqual(new long[] { 20 })), 7, true), Times.Once);
            _contributions.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ARaidWonWithNoFamilyMemberCreditsNobody()
        {
            await World.Create<FamilyRaidWonHandler>().Handle(new RaidWonEvent([], 7, false));

            _contributions.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task AGlacernonRaidCreditsTheFamilyWithHowItWasWon()
        {
            await World.Create<FamilyGlacernonRaidWonHandler>().Handle(
                new GlacernonRaidWonEvent(3, Act4RaidType.Calvina, true, false));

            _contributions.Verify(s => s.GlacernonRaidWonAsync(3, Act4RaidType.Calvina, true, false), Times.Once);
        }
    }
}
