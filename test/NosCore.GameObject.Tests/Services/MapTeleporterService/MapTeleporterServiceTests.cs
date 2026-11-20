//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Guri;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapTeleporterService;
using NosCore.GameObject.Services.MateService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Tests.Services.MapTeleporterService
{
    [TestClass]
    public class MapTeleporterServiceTests
    {
        private const int StoneId = 7282;
        private const short TeleportationStone = 856;
        private const short DestinationX = 22;
        private const short DestinationY = 72;

        private TestWorld World;
        private ClientSession _session;
        private Mate _pet;
        private bool _started;
        private readonly List<TeleporterDto> _teleporters = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            World.Inject(_teleporters);
        }

        [TestMethod]
        public async Task TalkingToAStoneStartsItsBar()
        {
            await new Spec("Talking to a teleportation stone starts a five-second bar naming its destination")
                .Given(AStoneLeadingTo__, DestinationX, DestinationY)
                .WhenAsync(TalkingToTheStone)
                .Then(TheStoneBarIsStarted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnNpcWithoutADestinationIsLeftToItsTalk()
        {
            await new Spec("An NPC with no destination across its map starts no bar")
                .Given(AnNpcWithoutADestination)
                .WhenAsync(TalkingToTheStone)
                .Then(NoBarIsStarted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBarRunningOutCarriesThePlayerAndTheirMates()
        {
            await new Spec("Once the bar has run the player and their pet are carried to the destination")
                .Given(AStoneLeadingTo__, DestinationX, DestinationY)
                .AndAsync(ThePlayerHasAPetOut)
                .AndAsync(TalkingToTheStone)
                .And(TheBarRunsOut)
                .WhenAsync(TheBarSendsItsFollowUpFor__, DestinationX, DestinationY)
                .Then(ThePlayerIsAt__, DestinationX, DestinationY)
                .And(ThePetStandsNextToThePlayer)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFollowUpNamingAnotherCellMovesNobody()
        {
            await new Spec("A follow-up naming a cell the stone does not lead to moves nobody")
                .Given(AStoneLeadingTo__, DestinationX, DestinationY)
                .AndAsync(TalkingToTheStone)
                .And(TheBarRunsOut)
                .WhenAsync(TheBarSendsItsFollowUpFor__, (short)80, (short)80)
                .Then(NoOneIsCarried)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFollowUpBeforeTheBarRanOutMovesNobody()
        {
            await new Spec("The follow-up sent before the bar has run moves nobody")
                .Given(AStoneLeadingTo__, DestinationX, DestinationY)
                .AndAsync(TalkingToTheStone)
                .WhenAsync(TheBarSendsItsFollowUpFor__, DestinationX, DestinationY)
                .Then(NoOneIsCarried)
                .ExecuteAsync();
        }

        private void AStone()
        {
            var character = _session.Character;
            character.MapInstance.LoadNpcs(
                new List<MapNpcDto>
                {
                    new() { MapNpcId = StoneId, MapId = character.MapInstance.Map.MapId, MapX = character.PositionX, MapY = character.PositionY, VNum = TeleportationStone }
                },
                new NosCore.GameObject.Services.BattleService.NpcMonsterCatalog(new List<NpcMonsterDto> { new() { NpcMonsterVNum = TeleportationStone } }));
        }

        private void AStoneLeadingTo__(short x, short y)
        {
            AStone();
            _teleporters.Add(new TeleporterDto
            {
                MapNpcId = StoneId, MapId = _session.Character.MapInstance.Map.MapId, MapX = x, MapY = y,
                Type = TeleporterType.TeleporterOnMapAfterBar
            });
        }

        private void AnNpcWithoutADestination() => AStone();

        private async Task ThePlayerHasAPetOut()
        {
            _pet = TestMates.Pet(_session.Character.CharacterId);
            _session.Character.Mates[_pet.MateTransportId] = _pet;
            await _pet.SpawnAsync(_session.Character, _session.Character.MapInstance,
                _session.Character.PositionX, _session.Character.PositionY);
        }

        private async Task TalkingToTheStone()
        {
            _session.LastPackets.Clear();
            _started = await World.Create<GameObject.Services.MapTeleporterService.MapTeleporterService>()
                .StartAsync(_session.Character, _session.Character.MapInstance.GetNpcById(StoneId).NotNull());
        }

        private void TheBarRunsOut() =>
            World.Clock.AdvanceMilliseconds(GameObject.Services.MapTeleporterService.MapTeleporterService.Delay);

        private Task TheBarSendsItsFollowUpFor__(short x, short y)
        {
            _session.Character.MapInstance.LastPackets.Clear();
            return new MapTeleporterHandler(World.Create<GameObject.Services.MapTeleporterService.MapTeleporterService>())
                .Handle(new GuriPacketReceivedEvent(_session, new ClientGuriPacket
                {
                    Type = GuriPacketType.MapTeleporter, Argument = x, VisualId = y, Data = StoneId
                }));
        }

        private void TheStoneBarIsStarted()
        {
            Assert.IsTrue(_started);
            PacketAssert.AreEqual(new DelayPacket
            {
                Delay = GameObject.Services.MapTeleporterService.MapTeleporterService.Delay,
                Type = DelayPacketType.Button,
                Packet = new ClientGuriPacket
                {
                    Type = GuriPacketType.MapTeleporter, Argument = DestinationX, VisualId = DestinationY, Data = StoneId
                }
            }, _session.LastPackets.OfType<DelayPacket>().Single());
        }

        private void NoBarIsStarted()
        {
            Assert.IsFalse(_started);
            Assert.IsFalse(_session.LastPackets.OfType<DelayPacket>().Any());
        }

        private void ThePlayerIsAt__(short x, short y)
        {
            Assert.AreEqual((x, y), (_session.Character.PositionX, _session.Character.PositionY));
            PacketAssert.AreEqual(new TpPacket { VisualType = VisualType.Player, VisualId = _session.Character.CharacterId, X = x, Y = y },
                _session.Character.MapInstance.LastPackets.OfType<TpPacket>().First());
        }

        private void ThePetStandsNextToThePlayer()
        {
            var pet = _pet.Entity.NotNull();
            var tp = _session.Character.MapInstance.LastPackets.OfType<TpPacket>().Single(p => p.VisualId == pet.VisualId);
            Assert.AreEqual((pet.PositionX, pet.PositionY), (tp.X, tp.Y));
            Assert.IsTrue(System.Math.Abs(tp.X - DestinationX) <= 1 && System.Math.Abs(tp.Y - DestinationY) <= 1);
        }

        private void NoOneIsCarried() =>
            Assert.IsFalse(_session.Character.MapInstance.LastPackets.OfType<TpPacket>().Any());
    }
}
