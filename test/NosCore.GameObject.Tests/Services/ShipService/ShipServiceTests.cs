using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using ShipTravelService = NosCore.GameObject.Services.ShipService.ShipService;

namespace NosCore.GameObject.Tests.Services.ShipService;

[TestClass]
public class ShipServiceTests
{
    private ClientSession _session = null!;
    private ShipTravelService _ships = null!;
    private Mock<IMapChangeService> _changes = null!;
    private List<ShipRouteDto> _routes = null!;
    private NpcComponentBundle _boardingNpc;
    private MapInstance _source = null!;
    private bool _failTransfer;

    [TestInitialize]
    public async Task SetupAsync()
    {
        await TestHelpers.ResetAsync();
        var helpers = TestHelpers.Instance;
        _session = await helpers.GenerateSessionAsync();
        _source = helpers.MapInstanceAccessorService.GetBaseMapById(0)!;
        _session.Character.Hp = 100;
        _source.LoadNpcs([new MapNpcDto { MapNpcId = 100, MapId = 0, VNum = 1, Dialog = 445 }],
            [new NpcMonsterDto { NpcMonsterVNum = 1 }]);
        _boardingNpc = _source.Npcs.Single();
        _routes = [Route(1)];
        _changes = new Mock<IMapChangeService>();
        _changes.Setup(m => m.ChangeMapInstanceAsync(_session, It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Callback<ClientSession, Guid, int?, int?>((session, id, x, y) =>
                TransferTo(helpers.MapInstanceRegistry.GetById(id)!));
        _changes.Setup(m => m.ChangeMapAsync(_session, It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()))
            .Callback<ClientSession, short?, short?, short?>((session, id, x, y) =>
                TransferTo(helpers.MapInstanceAccessorService.GetBaseMapById(id!.Value)!));
        var sessions = new Mock<ISessionRegistry>();
        sessions.Setup(s => s.GetSessions(null)).Returns([_session]);
        _ships = new ShipTravelService(_routes, [new NpcMonsterDto { NpcMonsterVNum = 1 }],
            helpers.MapInstanceAccessorService, helpers.MapInstanceRegistry, helpers.MapInstanceGeneratorService,
            _changes.Object, new Mock<IItemGenerationService>().Object, sessions.Object,
            helpers.Clock, NullLogger<ShipTravelService>.Instance);
        await _ships.InitializeAsync();
    }

    [TestMethod]
    public async Task BoardingEntersTheShipBeforeTheDestination() =>
        await new Spec("Boarding enters a ship and records its next scheduled departure")
            .WhenAsync(BoardAsync).Then(PassengerShouldBeAboard).And(DepartureShouldBeScheduled).And(GoldShouldBe_, 0L).ExecuteAsync();

    [TestMethod]
    public async Task DepartureWaitsUntilTheSharedBoundary() =>
        await new Spec("The ship waits until departure and takes its passenger to the destination exactly once")
            .GivenAsync(BoardAsync).WhenAsync(DepartAsync).Then(PassengerShouldBeAboard)
            .When(AdvanceToDeparture).WhenAsync(DepartAsync).WhenAsync(DepartAsync)
            .Then(TicketShouldBeCleared).And(DestinationShouldBeReachedOnce).ExecuteAsync();

    [TestMethod]
    public async Task LeavingViaTheCaptainCancelsDeparture() =>
        await new Spec("Leaving through the captain returns to port and cancels departure")
            .GivenAsync(BoardAsync).WhenAsync(CancelAsync).When(AdvancePastDeparture).WhenAsync(DepartAsync)
            .Then(TicketShouldBeCleared).And(PortShouldBeReachedOnce).And(DestinationShouldNotBeReached).ExecuteAsync();

    [TestMethod]
    public async Task AForgedCancellationCannotLeaveAnotherRoute() =>
        await new Spec("The cancellation runner must match the boarded route")
            .GivenAsync(BoardAsync).WhenAsync(SendAnotherCancellationAsync)
            .Then(PassengerShouldBeAboard).And(NoBaseMapChangeShouldOccur).ExecuteAsync();

    [TestMethod]
    public async Task LeavingByAnotherMeansCancelsDeparture() =>
        await new Spec("A passenger who already left cannot be teleported by a stale departure")
            .GivenAsync(BoardAsync).When(LeaveShip).When(AdvancePastDeparture).WhenAsync(DepartAsync)
            .Then(TicketShouldBeCleared).And(DestinationShouldNotBeReached).ExecuteAsync();

    [TestMethod]
    public async Task InsufficientFareKeepsThePassengerInPort() =>
        await new Spec("The fare is checked before moving the passenger")
            .Given(FareIs_, 100L).WhenAsync(BoardAsync).Then(TicketShouldBeCleared)
            .And(PassengerShouldBeTold_, Game18NConstString.NotEnoughGold).ExecuteAsync();

    [TestMethod]
    public async Task FailedBoardingDoesNotChargeOrIssueATicket() =>
        await new Spec("A rejected map change leaves gold and departure unchanged")
            .Given(FareIs_, 100L).And(GoldIs_, 100L).And(MapTransfersFail)
            .WhenAsync(BoardAsync).Then(GoldShouldBe_, 100L).And(TicketShouldBeCleared).ExecuteAsync();

    [TestMethod]
    public async Task PaidBoardingChargesOnlyOnce() =>
        await new Spec("Repeating boarding inside the ship cannot charge a second fare")
            .Given(FareIs_, 100L).And(GoldIs_, 200L).WhenAsync(BoardAsync).WhenAsync(BoardAsync)
            .Then(GoldShouldBe_, 100L).ExecuteAsync();

    [TestMethod]
    public async Task InvalidOptionsAndMenusCannotBoard() =>
        await new Spec("Only the configured menu and its first option can enter this ship")
            .WhenAsync(SendInvalidBoardingRequestsAsync).Then(TicketShouldBeCleared).And(NoShipMapChangeShouldOccur).ExecuteAsync();

    [TestMethod]
    public async Task NeutralPlayersCannotBoardAFactionShip() =>
        await new Spec("A faction ship requires its Power of Protection")
            .Given(TheShipRequiresAngels).WhenAsync(BoardAsync).Then(TicketShouldBeCleared)
            .And(PassengerShouldBeTold_, Game18NConstString.FactionRequired).ExecuteAsync();

    [TestMethod]
    public async Task ChangedFactionReturnsThePassengerToPort() =>
        await new Spec("Losing the required faction before departure returns the passenger to port")
            .Given(TheShipRequiresAngels).And(PassengerFactionIs_, FactionType.Angel).GivenAsync(BoardAsync)
            .When(PassengerFactionIs_, FactionType.Neutral).And(AdvancePastDeparture).WhenAsync(DepartAsync)
            .Then(PortShouldBeReachedOnce).ExecuteAsync();

    [TestMethod]
    public async Task FailedArrivalCanBeRetried() =>
        await new Spec("A failed arrival keeps the passenger aboard for the next attempt")
            .GivenAsync(BoardAsync).When(MapTransfersFail).And(AdvancePastDeparture).WhenAsync(DepartAsync)
            .Then(PassengerShouldBeAboard).When(MapTransfersSucceed).WhenAsync(DepartAsync).Then(TicketShouldBeCleared).ExecuteAsync();

    [TestMethod]
    public async Task DepartureBoundariesAreSharedAndStrictlyInTheFuture() =>
        await new Spec("Passengers in one interval share a departure; boarding on its boundary waits for the next one")
            .Then(DepartureAt_ShouldBe_, 1L, 120L).And(DepartureAt_ShouldBe_, 119L, 120L)
            .And(DepartureAt_ShouldBe_, 120L, 240L).ExecuteAsync();

    [TestMethod]
    public async Task AnErrorAfterEnteringTheShipStillRecordsThePassenger() =>
        await new Spec("A failure sending map packets after entering the ship cannot strand a passenger without a ticket")
            .Given(FareIs_, 100L).And(GoldIs_, 100L).And(BoardingFailsAfterMoving)
            .WhenAsync(BoardingShouldThrowAsync).Then(PassengerShouldBeAboard).And(GoldShouldBe_, 0L).ExecuteAsync();

    [TestMethod]
    public async Task FactionsBoardSeparateShipInstances() =>
        await new Spec("Two factions using the same boarding NPC travel in separate ships")
            .GivenAsync(ConfigureFactionShipsAsync).And(PassengerFactionIs_, FactionType.Demon)
            .WhenAsync(BoardAsync).Then(DemonShipShouldBeBoarded).ExecuteAsync();

    [TestMethod]
    public async Task DepartureDoesNotWaitForANonPassengerPacketLock()
    {
        using (await _session.AcquirePacketLockAsync())
        {
            await _ships.DepartAsync().WaitAsync(TimeSpan.FromSeconds(2));
        }
        NoBaseMapChangeShouldOccur();
    }

    [TestMethod]
    public async Task DepartureSkipsSessionsWithoutACharacter()
    {
        _session.ClearPlayerEntity();
        using (await _session.AcquirePacketLockAsync())
        {
            await _ships.DepartAsync().WaitAsync(TimeSpan.FromSeconds(2));
        }
        NoBaseMapChangeShouldOccur();
    }

    private async Task ConfigureFactionShipsAsync()
    {
        _routes.Clear();
        var angels = Route(2);
        angels.RequiredFaction = FactionType.Angel;
        var demons = Route(3);
        demons.RequiredFaction = FactionType.Demon;
        _routes.AddRange([angels, demons]);
        await _ships.InitializeAsync();
    }
    private void DemonShipShouldBeBoarded() => Assert.AreEqual((short)3, _session.Character.MapInstance.ShipRoute!.ShipRouteId);
    private void BoardingFailsAfterMoving() =>
        _changes.Setup(m => m.ChangeMapInstanceAsync(_session, It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Callback<ClientSession, Guid, int?, int?>((session, id, x, y) =>
            {
                TransferTo(TestHelpers.Instance.MapInstanceRegistry.GetById(id)!);
                throw new InvalidOperationException("Map packet send failed");
            });
    private async Task BoardingShouldThrowAsync() => await Assert.ThrowsExactlyAsync<InvalidOperationException>(BoardAsync);

    [TestMethod]
    public async Task RealMapTransfersPreserveTheTicketUntilArrival() =>
        await new Spec("Boarding migrates the ECS entity while preserving the saved port; arrival clears the migrated ticket")
            .Given(UseRealMapTransfers).WhenAsync(BoardAsync).Then(PassengerShouldBeAboard)
            .And(SavedMapShouldBe_, (short)0).When(AdvanceToDeparture).WhenAsync(DepartAsync)
            .Then(TicketShouldBeCleared).And(SavedMapShouldBe_, (short)1).ExecuteAsync();

    private void UseRealMapTransfers()
    {
        _session.Character.MapId = _source.Map.MapId;
        var helpers = TestHelpers.Instance;
        var sessions = new Mock<ISessionRegistry>();
        sessions.Setup(s => s.GetSessions(null)).Returns([_session]);
        _ships = new ShipTravelService(_routes, [new NpcMonsterDto { NpcMonsterVNum = 1 }],
            helpers.MapInstanceAccessorService, helpers.MapInstanceRegistry, helpers.MapInstanceGeneratorService,
            helpers.MapChangeService, new Mock<IItemGenerationService>().Object, sessions.Object,
            helpers.Clock, NullLogger<ShipTravelService>.Instance);
    }
    private void SavedMapShouldBe_(short id) => Assert.AreEqual(id, _session.Character.MapId);

    private Task DepartAsync() => _ships.DepartAsync();
    private Task CancelAsync() => _ships.CancelAsync(_session, _session.Character.MapInstance.Npcs.Single(), NrunRunnerType.CancelShipTravelMortazDesert, 0);
    private Task SendAnotherCancellationAsync() => _ships.CancelAsync(_session, _session.Character.MapInstance.Npcs.Single(), NrunRunnerType.CancelShipTravelGlacernon, 0);
    private async Task SendInvalidBoardingRequestsAsync()
    {
        await _ships.BoardAsync(_session, _boardingNpc, NrunRunnerType.GoToShipMortazDesert, 1);
        await _ships.BoardAsync(_session, _boardingNpc, NrunRunnerType.GoToShipGlacernon, 0);
    }
    private void LeaveShip() => TransferTo(_source);
    private void FareIs_(long value) => _routes[0].Fare = value;
    private void GoldIs_(long value) => _session.Character.Gold = value;
    private void MapTransfersFail() => _failTransfer = true;
    private void MapTransfersSucceed() => _failTransfer = false;
    private void TheShipRequiresAngels() => _routes[0].RequiredFaction = FactionType.Angel;
    private void PassengerFactionIs_(FactionType faction) => _session.Character.CharacterDto.Faction = (byte)faction;
    private void AdvancePastDeparture() => TestHelpers.Instance.Clock.Advance(Duration.FromMinutes(3));
    private void AdvanceToDeparture() => TestHelpers.Instance.Clock.Advance(_session.Character.ShipDepartureAt - TestHelpers.Instance.Clock.GetCurrentInstant());
    private void PassengerShouldBeAboard()
    {
        Assert.IsNotNull(_session.Character.MapInstance.ShipRoute);
        Assert.AreEqual(_session.Character.MapInstanceId, _session.Character.BoardedShipId);
    }
    private void TicketShouldBeCleared() => Assert.IsNull(_session.Character.BoardedShipId);
    private void GoldShouldBe_(long value) => Assert.AreEqual(value, _session.Character.Gold);
    private void DepartureShouldBeScheduled() => Assert.AreEqual(ShipTravelService.NextDeparture(TestHelpers.Instance.Clock.GetCurrentInstant(), 120), _session.Character.ShipDepartureAt);
    private void DestinationShouldBeReachedOnce() => _changes.Verify(m => m.ChangeMapAsync(_session, 1, 4, 4), Times.Once);
    private void PortShouldBeReachedOnce() => _changes.Verify(m => m.ChangeMapAsync(_session, 0, 4, 4), Times.Once);
    private void DestinationShouldNotBeReached() => _changes.Verify(m => m.ChangeMapAsync(_session, 1, 4, 4), Times.Never);
    private void NoBaseMapChangeShouldOccur() => _changes.Verify(m => m.ChangeMapAsync(_session, It.IsAny<short?>(), It.IsAny<short?>(), It.IsAny<short?>()), Times.Never);
    private void NoShipMapChangeShouldOccur() => _changes.Verify(m => m.ChangeMapInstanceAsync(_session, It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    private void PassengerShouldBeTold_(Game18NConstString message) => Assert.IsTrue(_session.LastPackets.OfType<SayiPacket>().Any(p => p.Message == message));
    private void DepartureAt_ShouldBe_(long now, long expected) => Assert.AreEqual(Instant.FromUnixTimeSeconds(expected), ShipTravelService.NextDeparture(Instant.FromUnixTimeSeconds(now), 120));

    private Task BoardAsync() => _ships.BoardAsync(_session, _boardingNpc, NrunRunnerType.GoToShipMortazDesert, 0);

    private void TransferTo(MapInstance map)
    {
        if (_failTransfer) return;
        _session.Character.MapInstance = map;
        _session.Character.MapInstanceId = map.MapInstanceId;
    }

    private static ShipRouteDto Route(short id) => new()
    {
        ShipRouteId = id,
        Runner = NrunRunnerType.GoToShipMortazDesert,
        CancelRunner = NrunRunnerType.CancelShipTravelMortazDesert,
        SourceMapId = 0, BoardingNpcVNum = 1, BoardingDialog = 445,
        ShipMapId = 1, ShipX = 4, ShipY = 4,
        DestinationMapId = 1, DestinationX = 4, DestinationY = 4,
        ReturnX = 4, ReturnY = 4,
        CaptainVNum = 1, CaptainDialog = 447, CaptainX = 5, CaptainY = 4,
        DepartureIntervalSeconds = 120,
    };
}
