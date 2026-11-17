public class TimeSpace624 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace624()
    {
        Definition = InstanceDefinitionBuilder.Named(624, "Gate 1", "Gate 1")
            .ForLevels(30, 99)
            .StartingAt(12, 8)
            .WithLives(2)
            .WithRoom(4201, out _room1, 4, 11)
            .WithRoom(4206, out _room2, 4, 10)
            .WithRoom(4206, out _room3, 4, 9)
            .WithRoom(4206, out _room4, 4, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(200));
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28));
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1));
        r.Summon(_room2,
            new MonsterSpawn(196, new(20, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2,
                new MonsterSpawn(196, new(12, 13)),
                new MonsterSpawn(196, new(21, 13), IsBonus: true));
            }),
            new MonsterSpawn(196, new(11, 11), IsBonus: true));
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28));
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1));
        r.Summon(_room3,
            new MonsterSpawn(196, new(9, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(196, new(10, 16)),
                new MonsterSpawn(196, new(15, 10), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(196, new(20, 16)),
                    new MonsterSpawn(196, new(21, 10), IsBonus: true),
                    new MonsterSpawn(196, new(21, 12)));
                }));
            }),
            new MonsterSpawn(196, new(8, 12)));
        r.SpawnExit(_room4, new(14, 1));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1));
        r.Summon(_room4,
            new MonsterSpawn(196, new(18, 18), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(196, new(20, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(196, new(6, 15), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4,
                        new MonsterSpawn(196, new(11, 9)),
                        new MonsterSpawn(196, new(15, 9), IsBonus: true),
                        new MonsterSpawn(196, new(19, 9)));
                    }),
                    new MonsterSpawn(196, new(9, 11), IsBonus: true));
                }),
                new MonsterSpawn(196, new(23, 15)));
            }),
            new MonsterSpawn(196, new(12, 18), IsBonus: true));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockExit(_room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace624();
