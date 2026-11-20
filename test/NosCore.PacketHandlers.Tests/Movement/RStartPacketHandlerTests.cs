//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MovementService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Movement;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Movement
{
    [TestClass]
    public class RStartPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IMovementService> MovementService;
        private Mock<IScriptedInstanceService> ScriptedInstances;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            MovementService = World.Freeze<Mock<IMovementService>>();
            ScriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
        }

        [TestMethod]
        public async Task ConfirmingTheStartGoesThroughTheWayOut()
        {
            await new Spec("Yes to the start question starts the run and goes through the way out of the first room")
                .Given(TheRunWillTakeTheConfirmation_, true)
                .WhenAsync(Answering_, RStartPacketType.TimeSpaceInstanceEntry)
                .Then(ThePortalIsUsed_Times, 1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AConfirmationTheRunRefusesMovesNobody()
        {
            await new Spec("A confirmation from outside a first room moves nobody")
                .Given(TheRunWillTakeTheConfirmation_, false)
                .WhenAsync(Answering_, RStartPacketType.TimeSpaceInstanceEntry)
                .Then(ThePortalIsUsed_Times, 0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SayingNoLeavesThePlayerWhereTheyStand()
        {
            await new Spec("No to the start question gets no answer and moves nobody")
                .Given(TheRunWillTakeTheConfirmation_, true)
                .WhenAsync(SayingNo)
                .Then(ThePortalIsUsed_Times, 0)
                .And(TheRunIsNotAsked)
                .ExecuteAsync();
        }

        private void TheRunWillTakeTheConfirmation_(bool taken) =>
            ScriptedInstances.Setup(s => s.ConfirmStartAsync(Session)).ReturnsAsync(taken);

        private Task Answering_(RStartPacketType type) =>
            World.Create<RStartPacketHandler>().ExecuteAsync(new RStartPacket { Type = type }, Session);

        private Task SayingNo() =>
            World.Create<RStartPacketHandler>().ExecuteAsync(new RStartPacket(), Session);

        private void ThePortalIsUsed_Times(int times) =>
            MovementService.Verify(s => s.UsePortalAsync(Session), Times.Exactly(times));

        private void TheRunIsNotAsked() =>
            ScriptedInstances.Verify(s => s.ConfirmStartAsync(It.IsAny<ClientSession>()), Times.Never);
    }
}
