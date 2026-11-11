//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class InvisibleCommandPacketHandlerTests
    {
        private TestWorld World;
        private InvisibleCommandPacketHandler Handler;
        private ClientSession Session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Session.Character.MapInstance = World.GetBaseMap(1);
            Handler = World.Create<InvisibleCommandPacketHandler>();
        }

        [TestMethod]
        public async Task InvisibleCommandTogglesBothCamouflageAndInvisible()
        {
            await new Spec("Invisible command flips both Camouflage and Invisible together")
                .Given(CharacterIsVisible)
                .WhenAsync(RunningInvisibleCommand)
                .Then(CamouflageShouldBe_, true)
                .And(InvisibleShouldBe_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RunningInvisibleTwiceRestoresVisibility()
        {
            await new Spec("Running the invisible command a second time flips back to visible")
                .Given(CharacterIsVisible)
                .WhenAsync(RunningInvisibleCommand)
                .AndAsync(RunningInvisibleCommand)
                .Then(CamouflageShouldBe_, false)
                .And(InvisibleShouldBe_, false)
                .ExecuteAsync();
        }

        private void CharacterIsVisible()
        {
            Session.Character.Camouflage = false;
            Session.Character.Invisible = false;
        }

        private async Task RunningInvisibleCommand()
        {
            await Handler.ExecuteAsync(new InvisibleCommandPacket(), Session);
        }

        private void CamouflageShouldBe_(bool expected) =>
            Assert.AreEqual(expected, Session.Character.Camouflage);

        private void InvisibleShouldBe_(bool expected) =>
            Assert.AreEqual(expected, Session.Character.Invisible);
    }
}
