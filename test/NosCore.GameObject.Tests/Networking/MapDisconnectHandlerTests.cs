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
using NosCore.GameObject.Networking.ClientSession.DisconnectHandlers;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class MapDisconnectHandlerTests
    {
        private ClientSession _session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public async Task LoggingOutTakesThePetOffTheMap()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[pet.MateTransportId] = pet;
            await pet.SpawnAsync(_session.Character, _session.Character.MapInstance, 4, 4);
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            await new MapDisconnectHandler().HandleDisconnectAsync(_session);

            Assert.IsTrue(map.LastPackets.OfType<OutPacket>()
                .Any(s => s.VisualType == VisualType.Npc && s.VisualId == pet.MateTransportId));
            Assert.IsNull(pet.Entity);
        }

        [TestMethod]
        public async Task APetLeftAtHomeSendsNothingOnLogout()
        {
            var pet = TestMates.Pet(_session.Character.CharacterId, isTeamMember: false);
            _session.Character.Mates[pet.MateTransportId] = pet;
            var map = _session.Character.MapInstance;
            map.LastPackets.Clear();

            await new MapDisconnectHandler().HandleDisconnectAsync(_session);

            Assert.IsFalse(map.LastPackets.OfType<OutPacket>().Any(s => s.VisualType == VisualType.Npc));
        }
    }
}
