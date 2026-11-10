using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Parser;
using NosCore.Parser.Parsers;
using NosCore.Packets.Enumerations;
using SpecLight;

namespace NosCore.Parser.Tests;

[TestClass]
public class ShipRouteParserTests
{
    private ShipRouteParser _parser = null!;
    private Mock<IDao<ShipRouteDto, short>> _dao = null!;
    private List<ShipRouteDto> _routes = null!;
    private List<ShipRouteDto> _saved = null!;
    private string _path = null!;

    [TestInitialize]
    public void Setup()
    {
        _path = Path.GetTempFileName();
        _saved = [];
        _routes = [new ShipRouteDto
        {
            ShipRouteId = 1, Runner = NrunRunnerType.GoToShipMortazDesert,
            CancelRunner = NrunRunnerType.CancelShipTravelMortazDesert,
            BoardingNpcVNum = 1, BoardingDialog = 445, CaptainVNum = 1, CaptainDialog = 447,
            DepartureIntervalSeconds = 120,
        }];
        _dao = new Mock<IDao<ShipRouteDto, short>>();
        _dao.Setup(d => d.LoadAll()).Returns([]);
        _dao.Setup(d => d.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShipRouteDto>>()))
            .Callback<IEnumerable<ShipRouteDto>>(routes => _saved.AddRange(routes)).ReturnsAsync(true);
        var maps = new Mock<IDao<MapDto, short>>();
        maps.Setup(d => d.LoadAll()).Returns([new MapDto { MapId = 0, Data = [2, 0, 2, 0, 0, 1, 0, 0] }]);
        var npcs = new Mock<IDao<MapNpcDto, int>>();
        npcs.Setup(d => d.LoadAll()).Returns([new MapNpcDto { MapId = 0, VNum = 1, Dialog = 445 }]);
        var monsters = new Mock<IDao<NpcMonsterDto, short>>();
        monsters.Setup(d => d.LoadAll()).Returns([new NpcMonsterDto { NpcMonsterVNum = 1 }]);
        var dialogs = new Mock<IDao<NpcTalkDto, short>>();
        dialogs.Setup(d => d.LoadAll()).Returns([new NpcTalkDto { DialogId = 447 }]);
        _parser = new ShipRouteParser(_dao.Object, maps.Object, npcs.Object, monsters.Object, dialogs.Object);
    }

    [TestCleanup]
    public void Cleanup() => File.Delete(_path);

    [TestMethod]
    public async Task ValidRoutesAreImported() =>
        await new Spec("Reviewed route data is saved with its original IDs and timetable")
            .WhenAsync(ImportAsync).Then(OneRouteShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task InvalidRoutesAreRejectedBeforeWritingAnyRows() =>
        await new Spec("A bad route anywhere in the file prevents every write")
            .Given(AddAnInvalidRoute).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task BlockedLandingCellsAreRejected() =>
        await new Spec("A route cannot land on a blocked map cell")
            .Given(DestinationIsBlocked).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task DuplicateBoardingMenusAreRejected() =>
        await new Spec("Two routes cannot claim the same NPC menu and faction")
            .Given(AddADuplicateMenu).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task InvalidTimetablesAreRejected() =>
        await new Spec("A ship cannot have a zero departure interval")
            .Given(IntervalIsZero).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task CancellationMustMatchTheBoardingRoute() =>
        await new Spec("A Mortaz ship cannot use a Glacernon cancellation menu")
            .Given(CancellationIsForAnotherShip).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task UnknownFieldsAreRejected() =>
        await new Spec("A misspelled route property cannot silently become a default value")
            .GivenAsync(WriteUnknownPropertyAsync).WhenAsync(UnknownPropertyImportShouldFailAsync)
            .Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task RouteImportCanRunWithoutAFullReimport() =>
        await new Spec("The targeted ship route input is separate from a full client data import")
            .Then(TargetedInputShouldBeRecognized).And(MixedInputsShouldBeRejected).ExecuteAsync();

    [TestMethod]
    public async Task WildcardMenusCannotMaskAFactionRoute() =>
        await new Spec("An unrestricted menu cannot mask a faction-specific ship")
            .Given(AddAFactionMenu).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    public async Task NegativeFaresAreRejected() =>
        await new Spec("Boarding cannot create gold through a negative fare")
            .Given(FareIsNegative).WhenAsync(ImportShouldFailAsync).Then(NoRoutesShouldBeSaved).ExecuteAsync();

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void EmptyShipRoutePathsAreRejected(string path)
    {
        Assert.ThrowsExactly<ArgumentException>(() => ParserCliOptions.Parse(["--ship-routes=" + path]));
        Assert.ThrowsExactly<ArgumentException>(() => ParserCliOptions.Parse(["--ship-routes", path]));
    }

    [TestMethod]
    public void InlineShipRoutePathsArePreserved()
    {
        var cli = ParserCliOptions.Parse(["--ship-routes=" + _path]);
        Assert.IsTrue(cli.HasShipRoutes);
        Assert.AreEqual(_path, cli.ShipRoutes);
    }

    private void FareIsNegative() => _routes[0].Fare = -1;
    private void AddAFactionMenu()
    {
        AddADuplicateMenu();
        _routes[1].RequiredFaction = FactionType.Angel;
    }

    private async Task ImportAsync()
    {
        await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(_routes));
        await _parser.ImportAsync(_path);
    }
    private async Task ImportShouldFailAsync() => await Assert.ThrowsExactlyAsync<InvalidDataException>(ImportAsync);
    private async Task UnknownPropertyImportShouldFailAsync() => await Assert.ThrowsExactlyAsync<JsonException>(() => _parser.ImportAsync(_path));
    private Task WriteUnknownPropertyAsync() => File.WriteAllTextAsync(_path, "[{\"ShipRouteId\":1,\"Typo\":120}]");
    private void DestinationIsBlocked() => _routes[0].DestinationX = 1;
    private void IntervalIsZero() => _routes[0].DepartureIntervalSeconds = 0;
    private void CancellationIsForAnotherShip() => _routes[0].CancelRunner = NrunRunnerType.CancelShipTravelGlacernon;
    private void AddAnInvalidRoute() => _routes.Add(new ShipRouteDto { ShipRouteId = 2 });
    private void AddADuplicateMenu()
    {
        var copy = JsonSerializer.Deserialize<ShipRouteDto>(JsonSerializer.Serialize(_routes[0]))!;
        copy.ShipRouteId = 2;
        _routes.Add(copy);
    }
    private void OneRouteShouldBeSaved()
    {
        Assert.AreEqual(1, _saved.Count);
        Assert.AreEqual(120, _saved.Single().DepartureIntervalSeconds);
        Assert.AreEqual(1, _saved.Single().ShipRouteId);
    }
    private void NoRoutesShouldBeSaved() => _dao.Verify(d => d.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<ShipRouteDto>>()), Times.Never);
    private void TargetedInputShouldBeRecognized()
    {
        var cli = ParserCliOptions.Parse(["--ship-routes", _path]);
        Assert.IsTrue(cli.HasShipRoutes);
        Assert.IsFalse(cli.HasFolder);
        Assert.AreEqual(_path, cli.ShipRoutes);
    }
    private void MixedInputsShouldBeRejected() => Assert.ThrowsExactly<ArgumentException>(() => ParserCliOptions.Parse(["--ship-routes", _path, "--folder", "client"]));
}
