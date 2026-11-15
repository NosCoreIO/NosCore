// The First Path
public class TimeSpace629 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace629()
    {
        Definition = InstanceDefinitionBuilder.Named(629, "", "The First Path")
            .ForLevels(47, 99)
            .StartingAt(8, 6)
            .WithLives(1)
            .Requiring(1012, 1)
            .WithRoom(4413, out _room1, 5, 6)
            .WithRoom(4410, out _room2, 5, 6)
            .WithRoom(4400, out _room3, 5, 5)
            .WithRoom(4411, out _room4, 5, 4)
            .WithRoom(4400, out _room5, 4, 4)
            .WithRoom(4400, out _room6, 5, 3)
            .WithRoom(4412, out _room7, 6, 4)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(12, 1), _room2, new(12, 23), locked: false);
        r.SpawnPortal(_room2, new(12, 23), _room1, new(12, 1), locked: false);
        r.SpawnPortal(_room2, new(12, 1), _room3, new(12, 23), locked: false);
        r.SpawnPortal(_room3, new(1, 12), _room4, new(23, 11), locked: false);
        r.SpawnPortal(_room3, new(12, 23), _room2, new(12, 1), locked: false);
        r.SpawnExit(_room4, new(1, 11), locked: false);
        r.SpawnPortal(_room4, new(23, 11), _room5, new(1, 12));
        r.Summon(_room4,
            new MonsterSpawn(153, new(9, 17)),
            new MonsterSpawn(153, new(10, 7)),
            new MonsterSpawn(153, new(10, 12), IsBonus: true),
            new MonsterSpawn(150, new(19, 12)));
        r.SpawnPortal(_room5, new(1, 12), _room4, new(23, 11), locked: false);
        r.SpawnPortal(_room5, new(12, 1), _room5, new(12, 1), locked: false);
        r.SpawnPortal(_room6, new(23, 12), _room6, new(23, 12), locked: false);
        r.SpawnPortal(_room6, new(12, 1), _room5, new(12, 0), locked: false);
        r.SpawnPortal(_room7, new(11, 23), _room6, new(12, 0));
        r.SpawnExit(_room7, new(11, 1), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(150, new(12, 7), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(153, new(10, 5), IsBonus: true),
                new MonsterSpawn(147, new(9, 10)));
            }),
            new MonsterSpawn(147, new(15, 8)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(300));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room7, _room6);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace629();
