//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.CharacterSelectionScreen;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.Handlers.Guri
{
    [TestClass]
    public class ActSceneReplayHandlerTests
    {
        private TestWorld World;
        private ClientSession _session;
        private ActSceneReplayHandler _handler;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handler = World.Create<ActSceneReplayHandler>();
        }

        [TestMethod]
        public async Task TheFirstActButtonReplaysTheFirstActScene()
        {
            await new Spec("The story window's act 1 button replays the act 1 scene")
                .WhenAsync(TheClientSends_, GuriPacketType.ReplayAct1Scene)
                .Then(TheServerShouldPlayScene_, (byte)40)
                .ExecuteAsync();
        }

        [TestMethod]
        [DataRow(GuriPacketType.ReplayAct2Scene)]
        [DataRow(GuriPacketType.ReplayAct5Scene)]
        public async Task AnActThePlayerHasNotReachedPlaysNothing(GuriPacketType button)
        {
            await new Spec("An act button the story window does not show to an act 1 player plays nothing")
                .WhenAsync(TheClientSends_, button)
                .Then(NoSceneShouldBePlayed)
                .ExecuteAsync();
        }

        private Task TheClientSends_(GuriPacketType type) =>
            _handler.Handle(new GuriPacketReceivedEvent(_session, new GuriPacket { Type = type }));

        private void TheServerShouldPlayScene_(byte sceneId) =>
            PacketAssert.AreEqual(new ScenePacket { SceneId = sceneId }, _session.LastPackets.OfType<ScenePacket>().Single());

        private void NoSceneShouldBePlayed() =>
            Assert.IsFalse(_session.LastPackets.OfType<ScenePacket>().Any());
    }
}
