//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.PacketHandlers.Game;
using NosCore.Packets.ClientPackets.Instance;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class WreqPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IScriptedInstanceService> Instances;
        private Mock<ITimeSpaceEntryService> Entry;
        private readonly ScriptedInstance Stone = new() { MapId = 1, PositionX = 5, PositionY = 6, Type = ScriptedInstanceType.TimeSpace };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Instances = World.Freeze<Mock<IScriptedInstanceService>>();
            Entry = World.Freeze<Mock<ITimeSpaceEntryService>>();
        }

        [TestMethod]
        public async Task SteppingOntoAStoneOpensItsCard()
        {
            await new Spec("Stepping onto a stone opens its card")
                .Given(TheCharacterStandsOnAStone)
                .WhenAsync(SendingAWreqWithNoValue)
                .Then(TheCardIsShown)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task SteppingOntoNothingOpensNoCard()
        {
            await new Spec("Stepping onto nothing opens no card")
                .WhenAsync(SendingAWreqWithNoValue)
                .Then(NoCardIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AWreqWithInstanceEntryEntersTheTimeSpace()
        {
            await new Spec("A wreq with instance entry enters the time-space")
                .Given(TheCharacterStandsOnAStone)
                .WhenAsync(SendingAWreqToEnter)
                .Then(TheTimeSpaceIsEntered)
                .And(NoCardIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheRecordButtonAsksTheRecordsCost()
        {
            await new Spec("A wreq to enter with the record button asks the record's cost instead of entering")
                .Given(TheCharacterStandsOnAStone)
                .WhenAsync(PressingTheRecordButton)
                .Then(TheRecordsCostIsAsked)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        private void TheCharacterStandsOnAStone()
        {
            var (mapId, x, y) = (Session.Character.MapId, Session.Character.PositionX, Session.Character.PositionY);
            Instances.Setup(s => s.GetNear(mapId, x, y)).Returns(Stone);
        }

        private Task SendingAWreqWithNoValue() =>
            World.Create<WreqPacketHandler>().ExecuteAsync(new WreqPacket(), Session);

        private Task SendingAWreqToEnter() =>
            World.Create<WreqPacketHandler>().ExecuteAsync(new WreqPacket { Value = WreqPacketType.InstanceEntry }, Session);

        private Task PressingTheRecordButton() =>
            World.Create<WreqPacketHandler>().ExecuteAsync(new WreqPacket { Value = WreqPacketType.InstanceEntry, Param = 1 }, Session);

        private void TheRecordsCostIsAsked() => Entry.Verify(s => s.AskRecordChallengeAsync(Session, Stone), Times.Once);

        private void TheCardIsShown() => Entry.Verify(s => s.ShowAsync(Session, Stone), Times.Once);

        private void NoCardIsShown() =>
            Entry.Verify(s => s.ShowAsync(It.IsAny<ClientSession>(), It.IsAny<ScriptedInstance>()), Times.Never);

        private void TheTimeSpaceIsEntered() => Entry.Verify(s => s.EnterAsync(Session, Stone, false, false), Times.Once);

        private void TheTimeSpaceIsNotEntered() =>
            Entry.Verify(s => s.EnterAsync(It.IsAny<ClientSession>(), It.IsAny<ScriptedInstance>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }
}
