# Writing a time-space script

A time-space is a C# script in `instances/`, compiled when it is first entered. A script
derives from `TimeSpaceScript` and declares everything in its constructor: the time-space, its
rooms, what each room holds and what happens when something happens to it. It overrides
nothing and never tests a room, dialog or button id: every event hangs off the object it belongs
to, and the library runs it for that object only.

One script object serves every run of its file. Anything a run changes (a room clock, a lever
pulled, a monster down) lives in the run, so a script holds no fields of its own.

## The shape

From `instances/timespace-09.csx`:

```csharp
public class TimeSpace9 : TimeSpaceScript
{
    public TimeSpace9() : base(9, "The Kovolt Riot")
    {
        Levels(18, 99);
        Start(8, 8);
        Requires(1012, 1);
        Lives(4);
        Clock(TimeSpan.FromSeconds(500));
        Objective(ObjectiveKind.KillMonster, 109, 1);
        Draw(162, 1);
        Draw(168, 1);
        Draw(174, 1);
        Special(1016, 1);
        Reward(721, 1);

        var entrance = Room(4014, 3, 2);
        var picket = Room(4004, 4, 2);
        var hall = Room(4004, 5, 2);

        entrance.Portal(new(17, 9), picket, new(0, 9), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var picketToHall = picket.Portal(new(17, 9), hall, new(0, 9));
        picket.OnEnter(r => r.Dialog(6175).Say(LanguageKey.TIMESPACE_MINERS_PROTESTING))
            .OnDialogClosed(6175, r => r
                .Spawn(Monster(105).At(6, 8), Monster(105).At(3, 10))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(picketToHall)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        hall.Spawn(Monster(105).At(15, 10), Monster(108).At(9, 9), Monster(108).At(1, 8));
        // ...
        eastHall.Spawn(
            Monster(105).At(7, 7).OnDeath(r => r.Spawn(Monster(105).At(14, 9))),
            Monster(105).At(3, 9).OnDeath(r => r.Spawn(Monster(108).At(10, 9).Bonus(), Monster(108).At(12, 8))));
    }
}

return new TimeSpace9();
```

Rooms are numbered by the order they are declared, which is also the order the run sets them up
in: portals, monsters, NPCs, levers and exits come in as they are declared.

## Declaring the time-space

| Call | Meaning |
|---|---|
| `Levels(min, max)`, `Start(x, y)`, `Requires(vnum, n)`, `Lives(n)` | Who may enter, where they land, the stone's price, deaths the party can afford |
| `Clock(length)` | The run's clock, started as the first player enters |
| `Objective(kind, vnum, n)` | A goal in the mission window; the exit stays shut until every one is met |
| `Reward`, `Draw`, `Special` | The reward chest |
| `Room(map, x, y)` | A room, at its place on the mission map |
| `After(elapsed, step)` | A step that long after the run started |
| `OnTimeout(step)` | Steps before the run fails for running out of time |

## Rooms and what they hold

| On a room | Meaning |
|---|---|
| `Spawn(monsters…)`, `Spawn(npcs…)` | What the room holds from the start |
| `Portal(at, to, arrival, locked)` | A one-way portal, kept as a handle to unlock later; one per pair of rooms |
| `Lever(at)` | A lever, with `OnUse(step)`; `Lever(at, off, on)` draws it with other vnums, such as a button |
| `Sculpture(at, off, on)` | A sculpture, moved without a word, with `OnUse(step)` |
| `Exit(at, locked)` | The way out, at most one per room, with `OnReached(step)`; reaching it wins the run |
| `OnEnter(step)` | The first time anyone enters |
| `OnCleared(step)` | When its last monster falls |
| `OnDialogClosed(dialog, step)` | The first time that dialog is closed |
| `Every(interval, step)` | While the room's clock runs: as it starts, then every interval, whenever someone is inside |
| `OnClockOut(step)` | When the room's clock runs out; the clock is taken down after the step |

## Monsters

`Monster(vnum)` is a value: each call returns a new monster, so one can be the base of several.

| On a monster | Meaning |
|---|---|
| `At(x, y)` | A fixed cell |
| `Anywhere()` | A walkable cell picked on each spawn, away from the doors |
| `Around(x, y, radius)` | A walkable cell picked on each spawn, within radius of the cell |
| `Count(n)` | That many copies, each placed on its own, so two may share a cell |
| `Bonus()` | Flashes while alive and counts towards the combo |
| `OnDeath(step)` | When this monster falls |
| `OnHit(step)` | On each hit it survives |
| `OnHpBelow(percent, step)` | Once, on the hit that takes its HP below that share of its maximum |

A monster needs one of `At`, `Anywhere` or `Around`. `Npc(vnum).At(x, y)` places an NPC.

## Steps

A step receives `r`, the event, and chains verbs on it. Steps of a room, its monsters, its lever
or its exit act in that room; `After` and `OnTimeout` steps reach a room with `r.In(room)`.

| Verb | Meaning |
|---|---|
| `Say(message)` | A line for everyone in the run: a `Game18NConstString` first, a `LanguageKey` when the client has no text |
| `Unlock(portals…)`, `Unlock(exit)` | Opens them |
| `AddTime(length)` | More time on the run's clock, doubled in Loser Mode |
| `Succeed()`, `Fail()` | Ends the run |
| `Spawn(monsters…)` | Brings monsters into the room |
| `Dialog(id)` | Opens that NPC dialog for the players in the room |
| `Objective(key)` | The room's line in the mission panel until it is cleared |
| `StartRoomClock(length)` | The room's red clock, which drives `Every` and `OnClockOut` |
| `StopRoomClock()` | Takes the room's clock down early, so its waves and `OnClockOut` steps do not come |
| `SayToRoom(message)` | A line for the players standing in the room only |
| `Spawn(npcs…)` | Brings NPCs into the room |
| `Dismiss(vnums…)` | Takes the living monsters of those vnums out of the room |
| `After(delay, step)` | That long after this event, in the same room |
| `IfAnyoneInside(step)` | Only while someone stands in the room |
| `In(room)` | The same verbs, acting in another room |

A room held for a minute while waves come through it, from `instances/timespace-18.csx`:

```csharp
holdout.OnDialogClosed(6097, r => r
        .Spawn(Monster(338).At(20, 15))
        .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_PORTAL_OPENS)
        .StartRoomClock(TimeSpan.FromSeconds(60)))
    .Every(TimeSpan.FromSeconds(30), r => r
        .Say(Game18NConstString.EnemiesReinforcements)
        .Spawn(Monster(338).At(11, 20), Monster(338).At(11, 22)))
    .OnClockOut(r => r
        .Unlock(holdoutToCrossing, crossingToDescent)
        .Say(Game18NConstString.ADoorHasOpened));
```

## Testing a script

`test/NosCore.Instances.Tests` loads every `timespace-*.csx` against a recording run and walks
from the first room to the exit, closing each dialog, clearing each room, pulling each lever and
killing each monster on the way. A script that leaves the exit out of reach fails there.
