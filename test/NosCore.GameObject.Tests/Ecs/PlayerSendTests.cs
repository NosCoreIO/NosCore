//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Ecs
{
    [TestClass]
    public class PlayerSendTests
    {
        private TestWorld World;

        [TestInitialize]
        public async Task SetupAsync() => World = await TestWorld.CreateAsync();

        [TestMethod]
        public async Task APacketForAPlayerWhoseEntityIsGoneIsDropped()
        {
            var session = await World.CreateSessionAsync();
            var player = session.Character;
            player.World.DestroyEntity(player.Entity);

            await player.SendPacketAsync(new InfoPacket { Message = "gone" });
            await player.SendPacketsAsync([new InfoPacket { Message = "gone" }]);
        }

        [TestMethod]
        public async Task APacketForAPlayerStillInTheWorldIsSent()
        {
            var session = await World.CreateSessionAsync();

            await session.Character.SendPacketAsync(new InfoPacket { Message = "here" });

            Assert.IsTrue(session.LastPackets.OfType<InfoPacket>().Any(p => p.Message == "here"));
        }
    }
}
