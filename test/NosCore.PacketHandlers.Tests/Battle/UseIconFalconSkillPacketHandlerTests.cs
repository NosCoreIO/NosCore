//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Tests.Shared;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class UseIconFalconSkillPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IFalconService> Falcon;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Falcon = World.Freeze<Mock<IFalconService>>();
        }

        [TestMethod]
        public async Task TheHuntingFalconIconSendsTheFalconToAttack()
        {
            long? striker = null;
            Falcon.Setup(f => f.StrikeAsync(It.IsAny<PlayerComponentBundle>()))
                .Callback<PlayerComponentBundle>(p => striker = p.VisualId)
                .ReturnsAsync(true);

            await World.Create<UseIconFalconSkillPacketHandler>().ExecuteAsync(new UseIconFalconSkillPacket(), Session);

            Assert.AreEqual(Session.Character.VisualId, striker);
        }
    }
}
