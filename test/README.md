# Writing a test

Every test gets its own `TestWorld`. It has a fresh in-memory database, a fake clock, a session
registry and three loaded maps (`TestWorld.TestMapId`, `ShopMapId` and `MinilandMapId`).
Nothing is shared between tests, so there is no reset to call.

```csharp
[TestClass]
public class FamilyTitlePacketHandlerTests
{
    private TestWorld World;
    private ClientSession Session;
    private Mock<IFamilyService> FamilyService;

    [TestInitialize]
    public async Task SetupAsync()
    {
        World = await TestWorld.CreateAsync();
        Session = await World.CreateSessionAsync();
        FamilyService = World.Freeze<Mock<IFamilyService>>();
    }

    [TestMethod]
    public async Task OnlyTheHeadCanChangeATitle()
    {
        await new Spec("Only the head can change a title")
            .Given(TheCharacterIsAMember)
            .WhenAsync(ChangingATitle)
            .Then(NothingIsSaved)
            .ExecuteAsync();
    }

    private Task ChangingATitle() =>
        World.Create<FamilyTitlePacketHandler>().ExecuteAsync(new FamilyTitlePacket { ... }, Session);
}
```

## Building the class under test

`World.Create<T>()` builds any handler or service. Its constructor arguments come from these
places:

| Argument | What it gets |
|---|---|
| Clock, session registry, localizers, `IOptions<WorldConfiguration>` | The world's own, so `World.Clock.Advance(...)` is seen by the handler |
| A NosCore.Algorithm formula (`IExperienceService`, `IHpService`, ...) | The real formula |
| Map services, the item generator, the NPC dialog service | The world's real ones |
| `IDao<AccountDto>`, `IDao<CharacterDto>` and the other DAOs listed on `TestWorld` | A DAO on the world's in-memory database |
| `List<ItemDto>` | `World.ItemList`, the items every session can hold |
| `IIdService<T>` | A real one starting at 1 |
| Any other interface | A Moq mock that returns null, zero or an empty collection |
| Any other `List<T>` or array | Empty |

To set up or verify a dependency, freeze it **before** calling `Create`:

```csharp
var bazaar = World.Freeze<Mock<IBazaarHub>>();
bazaar.Setup(s => s.AddBazaarAsync(It.IsAny<BazaarRequest>())).ReturnsAsync(LanguageKey.OBJECT_IN_BAZAAR);
var handler = World.Create<CRegPacketHandler>();
```

A frozen mock also replaces anything the world had registered for that interface. Freezing
`Mock<ISessionRegistry>` replaces the real registry, for example.

Other ways to control a dependency:

- `World.UseReal<IFamilyManagementService, FamilyManagementService>()` uses the real
  implementation, which is itself built from the world.
- `World.Inject(instance)` hands over a specific object, such as a list of items or skills.

## Sessions and players

- `World.CreateSessionAsync()` creates an account and a character, puts them on map 0 and
  returns the `ClientSession`. `session.Character` is the player entity.
- `session.LastPackets` holds what the server sent to that session.
- `session.World()` gets back to the world from a BDD step that only has the session.

## Without a world

A test that needs no session, map or database can use `new NosCoreFixture()` directly. It
follows the same rules. `TestLocalizers.Log` and `TestLocalizers.Game` echo the key they are
asked for.

## End-to-end scenarios

`NosCore.EndToEnd.Tests` starts the real master, login and world servers and talks to them
over sockets the way the game client does. Nothing is mocked: a scenario covers the path from
the socket through the decoder, the session, the handler and the service back to what the other
player receives.

```csharp
[TestMethod]
public async Task AStepIsSeenByTheOtherPlayer()
{
    await using var bob = await Harness.EnterAsync("Bob", x: 82, y: 116);
    await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116);

    await alice.WalkAsync(81, 117);

    var step = await bob.ExpectAsync(p => p.StartsWith($"mv 1 {alice.CharacterId} "), "mv for Alice");
    Assert.AreEqual($"mv 1 {alice.CharacterId} 81 117 {alice.Speed}", step);
}
```

What it needs and does:

- **A PostgreSQL**, on `localhost` with `postgres` / `password` or wherever `DB_HOST`,
  `DB_USER` and `DB_PASSWORD` point. Without one the scenarios report themselves as not run.
  Set `NOSCORE_E2E_REQUIRED=true` to make that a failure instead, as CI does.
- **The built servers.** Build the solution first; the servers are started from
  `build/net11.0` on free ports, with a copy of `configuration/`.
- **Its own database**, created for the run, migrated, seeded by `WorldData` and dropped at
  the end. No client files are involved: the world is two open maps joined by portals, a
  miniland, a shopkeeper with one item to sell, and what a new adventurer is given. Add to `WorldData` what a new scenario
  needs; static data is read when the servers start.

Writing one:

- `Harness.EnterAsync("Name", map, x, y)` adds a character on an account of its own and brings
  it into the world. Every scenario makes its own characters, so scenarios never share one.
- `client.SendAsync("line")` sends a packet as text. `client.ExpectAsync("header")` returns the
  next packet with that header and fails the test with the last packets received when none
  comes. Each `ExpectAsync` starts after the packet the previous one returned, so a chain of
  them also asserts the order.
- When two packets answer one action and their order is not the point, take
  `var mark = client.Mark()` before the action and use `client.ExpectSinceAsync(mark, ...)`.
- To assert that something is not sent, take a mark before the action and check
  `await client.NoneSinceAsync(mark, match, TimeSpan.FromSeconds(1))`.
- `Harness.GiveAsync(player, vnum, amount)` puts items in a bag before the character enters;
  `configure: c => c.Gold = 1000` on `EnterAsync` or `AddPlayerAsync` sets anything on the row.
- `Harness.StoredAsync(characterId)` and `Harness.Database.CreateContext()` read what was
  stored.
- A failing start prints the server's last lines, and an expectation that times out adds the
  world servers' error lines to its message. `Harness.Stack.LogTail("NosCore.WorldServer")`
  gives a scenario the rest.

The stack has two channels: two world servers on one master and one login server.
`Harness.EnterAsync(..., channel: 1)` puts a character on the second one.

The servers are shared by the whole run, so a scenario must not depend on being alone on a
map: assert on packets about its own characters.

