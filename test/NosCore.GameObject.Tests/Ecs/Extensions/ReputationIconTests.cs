//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    // The guard choosing between the two ladders tested the dignity icon against 0, which it
    // can never be, so the reputation icon was never sent and a normal player drew (byte)-1.
    [TestClass]
    public class ReputationIconTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public void UntouchedDignityDrawsTheReputationIcon()
        {
            _session.Character.Dignity = 100;
            _session.Character.Reputation = 5_000;

            Assert.AreEqual((byte)ReputationType.RedExperienced, _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco);
        }

        [TestMethod]
        public void TheIconFollowsReputationAcrossABand()
        {
            _session.Character.Dignity = 100;

            _session.Character.Reputation = 250;
            var beginner = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco;

            _session.Character.Reputation = 251;
            var trainee = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco;

            Assert.AreEqual((byte)ReputationType.RedBeginner, beginner);
            Assert.AreEqual((byte)ReputationType.GreenTrainee, trainee);
        }

        [TestMethod]
        public void LostDignityDrawsTheNegatedDignityIcon()
        {
            _session.Character.Reputation = 5_000;
            _session.Character.Dignity = -300;

            Assert.AreEqual(unchecked((byte)-(int)DignityType.Dreadful), _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco);
        }

        [TestMethod]
        public void TheDignityIconTakesOverExactlyAtTheFirstPenaltyBand()
        {
            _session.Character.Reputation = 5_000;

            _session.Character.Dignity = -99;
            var stillReputation = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco;

            _session.Character.Dignity = -100;
            var nowDignity = _session.Character.GenerateIn(string.Empty).InCharacterSubPacket!.ReputIco;

            Assert.AreEqual((byte)ReputationType.RedExperienced, stillReputation);
            Assert.AreEqual(unchecked((byte)-(int)DignityType.Dubious), nowDignity);
        }
    }
}
