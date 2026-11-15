// The Second Path
public class TimeSpace630 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;
    private readonly int _room8;
    private readonly int _room9;
    private readonly int _room10;
    private readonly int _room11;
    private readonly int _room12;

    public TimeSpace630()
    {
        Definition = InstanceDefinitionBuilder.Named(630, "", "The Second Path")
            .ForLevels(47, 99)
            .StartingAt(12, 8)
            .WithLives(1)
            .Requiring(1012, 1)
            .WithRoom(4414, out _room1, 5, 6)
            .WithRoom(4409, out _room2, 5, 6)
            .WithRoom(4400, out _room3, 6, 6)
            .WithRoom(4410, out _room4, 7, 6)
            .WithRoom(4400, out _room5, 7, 5)
            .WithRoom(4400, out _room6, 8, 6)
            .WithRoom(4400, out _room7, 7, 7)
            .WithRoom(4406, out _room8, 9, 6)
            .WithRoom(4401, out _room9, 10, 6)
            .WithRoom(4403, out _room10, 9, 7)
            .WithRoom(4416, out _room11, 9, 8)
            .WithRoom(4403, out _room12, 10, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 11), locked: false);
        r.SpawnPortal(_room2, new(1, 11), _room1, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 11), _room3, new(1, 12), locked: false);
        r.SpawnPortal(_room3, new(1, 12), _room2, new(23, 11), locked: false);
        r.SpawnPortal(_room3, new(12, 1), _room4, new(12, 23), locked: false);
        r.SpawnPortal(_room4, new(12, 23), _room5, new(12, 1));
        r.SpawnExit(_room4, new(12, 1), locked: false);
        r.Summon(_room4, new MonsterSpawn(147, new(10, 14), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(147, new(16, 11), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(147, new(12, 11)),
                    new MonsterSpawn(150, new(7, 5), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4,
                        new MonsterSpawn(150, new(17, 5), IsBonus: true),
                        new MonsterSpawn(147, new(8, 11)));
                    }));
                }));
            }));
        r.SpawnPortal(_room5, new(23, 12), _room5, new(23, 12), locked: false);
        r.SpawnPortal(_room5, new(12, 1), _room4, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(12, 23), _room6, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(23, 12), _room5, new(1, 0), locked: false);
        r.SpawnPortal(_room7, new(12, 23), _room6, new(12, 0), locked: false);
        r.SpawnPortal(_room7, new(23, 12), _room7, new(23, 12), locked: false);
        r.SpawnPortal(_room8, new(1, 12), _room9, new(23, 11));
        r.SpawnExit(_room8, new(12, 1), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(150, new(10, 9), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room8, new MonsterSpawn(150, new(7, 17)));
                r.Summon(_room8, new MonsterSpawn(147, new(14, 9), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room8, new MonsterSpawn(150, new(7, 5)));
                    r.Summon(_room8, new MonsterSpawn(147, new(9, 11)));
                    r.Summon(_room8, new MonsterSpawn(147, new(12, 17)));
                    r.Summon(_room8, new MonsterSpawn(147, new(10, 7), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room8, new MonsterSpawn(150, new(15, 5)));
                        r.Summon(_room8, new MonsterSpawn(147, new(18, 7)));
                        r.Summon(_room8, new MonsterSpawn(147, new(18, 9)));
                        r.Summon(_room8, new MonsterSpawn(147, new(13, 17), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room8, new MonsterSpawn(153, new(5, 6)));
                            r.Summon(_room8, new MonsterSpawn(153, new(16, 12)));
                            r.Summon(_room8, new MonsterSpawn(150, new(15, 11)));
                            r.Summon(_room8, new MonsterSpawn(147, new(19, 15)));
                            r.Summon(_room8, new MonsterSpawn(147, new(13, 16)));
                            r.Summon(_room8, new MonsterSpawn(147, new(9, 19)));
                        }));
                    }));
                }));
                r.Summon(_room8, new MonsterSpawn(147, new(19, 10)));
                r.Summon(_room8, new MonsterSpawn(147, new(17, 15)));
            }),
            new MonsterSpawn(147, new(7, 17)),
            new MonsterSpawn(147, new(9, 9)),
            new MonsterSpawn(147, new(5, 15)));
        r.SpawnPortal(_room9, new(12, 23), _room9, new(12, 23), locked: false);
        r.SpawnPortal(_room9, new(23, 11), _room8, new(1, 12), locked: false);
        r.SpawnPortal(_room10, new(23, 12), _room11, new(1, 12), locked: false);
        r.SpawnPortal(_room10, new(12, 1), _room9, new(11, 0), locked: false);
        r.SpawnPortal(_room11, new(1, 12), _room12, new(23, 12));
        r.PlaceButton(_room11, 1, new(12, 12), 1000, 1045);
        r.SpawnPortal(_room12, new(1, 12), _room12, new(1, 12), locked: false);
        r.SpawnPortal(_room12, new(23, 12), _room11, new(1, 12), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(500));
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room8, TimeSpan.FromSeconds(150));
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
        else if (room == _room8)
        {
            r.Message(Game18NConstString.NewGoal);
            r.StopRoomClock(_room8);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room12);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room11 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Summon(_room11,
                        new MonsterSpawn(60, new(5, 14)),
                        new MonsterSpawn(60, new(6, 10)),
                        new MonsterSpawn(60, new(6, 12)),
                        new MonsterSpawn(60, new(6, 14)),
                        new MonsterSpawn(60, new(6, 16)),
                        new MonsterSpawn(60, new(6, 17)),
                        new MonsterSpawn(60, new(8, 10)),
                        new MonsterSpawn(60, new(8, 14)),
                        new MonsterSpawn(60, new(9, 8)),
                        new MonsterSpawn(60, new(9, 16)),
                        new MonsterSpawn(60, new(9, 17)),
                        new MonsterSpawn(60, new(11, 8)),
                        new MonsterSpawn(60, new(11, 17)),
                        new MonsterSpawn(60, new(13, 9)),
                        new MonsterSpawn(60, new(14, 9)),
                        new MonsterSpawn(60, new(14, 15)),
                        new MonsterSpawn(60, new(14, 17), IsBonus: true),
                        new MonsterSpawn(60, new(15, 10)),
                        new MonsterSpawn(60, new(16, 11)),
                        new MonsterSpawn(60, new(16, 13)),
                        new MonsterSpawn(60, new(16, 15)));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace630();
