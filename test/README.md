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
