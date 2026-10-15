//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MatePartyPanelTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public void AloneWithoutAPetThePanelIsEmpty()
        {
            var pinit = _session.Character.GeneratePinit();

            Assert.AreEqual(0, pinit.GroupSize);
            Assert.AreEqual(0, pinit.PinitSubPackets!.Count, "the recording sends a bare pinit 0 when alone");
        }

        [TestMethod]
        public void AnActivePetIsListedTheWayTheRecordingShows()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId, level: 2);
            _session.Character.Mates[pet.MateTransportId] = pet;

            var pinit = _session.Character.GeneratePinit();

            Assert.AreEqual(1, pinit.GroupSize);
            var entry = pinit.PinitSubPackets!.Single()!;
            Assert.AreEqual(VisualType.Npc, entry.VisualType);
            Assert.AreEqual(pet.MateTransportId, entry.VisualId);
            Assert.AreEqual(1, entry.GroupPosition);
            Assert.AreEqual(2, entry.Level);
            Assert.AreEqual(-1, entry.Unknown);
            Assert.AreEqual(333, entry.Unknown2, "the field after -1 carries the creature's vnum");
        }

        [TestMethod]
        public void APetLeftAtHomeIsNotListed()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId, isTeamMember: false);
            _session.Character.Mates[pet.MateTransportId] = pet;

            Assert.AreEqual(0, _session.Character.GeneratePinit().PinitSubPackets!.Count);
        }

        [TestMethod]
        public async Task InAGroupThePetComesAfterThePlayers()
        {
            var other = await TestHelpers.Instance.GenerateSessionAsync();
            var group = new Group(GroupType.Group, TestHelpers.Instance.SessionGroupFactory);
            _session.Character.JoinGroup(group);
            other.Character.JoinGroup(group);
            var pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;

            var pinit = _session.Character.GeneratePinit();

            Assert.AreEqual(3, pinit.GroupSize);
            CollectionAssert.AreEqual(
                new[] { VisualType.Player, VisualType.Player, VisualType.Npc },
                pinit.PinitSubPackets!.Select(s => s!.VisualType).ToArray());
        }
    }
}
