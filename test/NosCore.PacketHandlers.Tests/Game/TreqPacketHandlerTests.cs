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
using NosCore.Packets.ClientPackets.Event;
using NosCore.Tests.Shared;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Game
{
    [TestClass]
    public class TreqPacketHandlerTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IScriptedInstanceService> Instances;
        private Mock<ITimeSpaceEntryService> Entry;
        private ScriptedInstance Stone;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            Instances = World.Freeze<Mock<IScriptedInstanceService>>();
            Entry = World.Freeze<Mock<ITimeSpaceEntryService>>();
            Stone = new ScriptedInstance
            {
                MapId = Session.Character.MapId,
                PositionX = Session.Character.PositionX,
                PositionY = Session.Character.PositionY,
                Type = ScriptedInstanceType.TimeSpace,
            };
        }

        [TestMethod]
        public async Task ATreqOnAStoneShowsItsCard()
        {
            await new Spec("A treq on a stone shows its card")
                .Given(AStoneIsUnderTheCharacter)
                .WhenAsync(SendingATreqAtTheCharacter)
                .Then(TheCardIsShown)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PressingStartEntersTheTimeSpace()
        {
            await new Spec("Pressing start enters the time-space")
                .Given(AStoneIsUnderTheCharacter)
                .WhenAsync(PressingStartAtTheCharacter)
                .Then(TheTimeSpaceIsEntered)
                .And(NoCardIsShown)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATreqFarFromTheCharacterIsIgnored()
        {
            await new Spec("A treq far from the character is ignored")
                .Given(AStoneIsUnderTheCharacter)
                .WhenAsync(SendingATreqFarAway)
                .Then(NoCardIsShown)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATreqOnARaidEntranceIsIgnored()
        {
            await new Spec("A treq on a raid entrance is ignored")
                .Given(ARaidEntranceIsUnderTheCharacter)
                .WhenAsync(PressingStartAtTheCharacter)
                .Then(NoCardIsShown)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ATreqOnNothingIsIgnored()
        {
            await new Spec("A treq on nothing is ignored")
                .WhenAsync(SendingATreqAtTheCharacter)
                .Then(NoCardIsShown)
                .And(TheTimeSpaceIsNotEntered)
                .ExecuteAsync();
        }

        private void AStoneIsUnderTheCharacter() =>
            Instances.Setup(s => s.GetAt(Stone.MapId, Stone.PositionX, Stone.PositionY)).Returns(Stone);

        private void ARaidEntranceIsUnderTheCharacter()
        {
            Stone.Type = ScriptedInstanceType.Raid;
            AStoneIsUnderTheCharacter();
        }

        private Task SendingATreqAtTheCharacter() => SendAsync(new TreqPacket { X = Stone.PositionX, Y = Stone.PositionY });

        private Task PressingStartAtTheCharacter() =>
            SendAsync(new TreqPacket { X = Stone.PositionX, Y = Stone.PositionY, StartPress = true });

        private Task SendingATreqFarAway() =>
            SendAsync(new TreqPacket { X = Stone.PositionX + 5, Y = Stone.PositionY, StartPress = true });

        private Task SendAsync(TreqPacket packet) => World.Create<TreqPacketHandler>().ExecuteAsync(packet, Session);

        private void TheCardIsShown() => Entry.Verify(s => s.ShowAsync(Session, Stone), Times.Once);

        private void NoCardIsShown() =>
            Entry.Verify(s => s.ShowAsync(It.IsAny<ClientSession>(), It.IsAny<ScriptedInstance>()), Times.Never);

        private void TheTimeSpaceIsEntered() => Entry.Verify(s => s.EnterAsync(Session, Stone, false), Times.Once);

        private void TheTimeSpaceIsNotEntered() =>
            Entry.Verify(s => s.EnterAsync(It.IsAny<ClientSession>(), It.IsAny<ScriptedInstance>(), It.IsAny<bool>()), Times.Never);
    }
}
