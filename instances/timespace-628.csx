// The Koaren Spy
public class TimeSpace628 : InstanceScript
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

    public TimeSpace628()
    {
        Definition = InstanceDefinitionBuilder.Named(628, "", "The Koaren Spy")
            .ForLevels(44, 99)
            .StartingAt(10, 8)
            .WithLives(2)
            .Requiring(1012, 1)
            .WithRoom(4414, out _room1, 0, 8)
            .WithRoom(4409, out _room2, 0, 8)
            .WithRoom(4405, out _room3, 1, 8)
            .WithRoom(4402, out _room4, 2, 8)
            .WithRoom(4403, out _room5, 2, 9)
            .WithRoom(4409, out _room6, 2, 10)
            .WithRoom(4416, out _room7, 3, 10)
            .WithRoom(4409, out _room8, 4, 10)
            .WithRoom(4403, out _room9, 1, 10)
            .WithRoom(4410, out _room10, 3, 9)
            .WithRoom(4408, out _room11, 3, 8)
            .WithRoom(4411, out _room12, 3, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 11), locked: false);
        r.SpawnPortal(_room2, new(1, 11), _room1, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 11), _room3, new(1, 12), locked: false);
        r.SpawnPortal(_room3, new(1, 12), _room2, new(23, 11));
        r.SpawnPortal(_room3, new(12, 23), _room4, new(12, 1));
        r.Summon(_room3,
            new MonsterSpawn(145, new(8, 10)),
            new MonsterSpawn(145, new(14, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(145, new(15, 17), IsBonus: true),
                new MonsterSpawn(151, new(10, 13)));
            }));
        r.SpawnPortal(_room4, new(12, 23), _room5, new(12, 1), locked: false);
        r.SpawnPortal(_room4, new(12, 1), _room3, new(12, 23), locked: false);
        r.SpawnPortal(_room5, new(23, 12), _room6, new(1, 11), locked: false);
        r.SpawnPortal(_room5, new(12, 1), _room4, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(1, 11), _room5, new(23, 12));
        r.SpawnPortal(_room6, new(23, 11), _room7, new(1, 12));
        r.Summon(_room6,
            new MonsterSpawn(146, new(11, 8), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(149, new(16, 11), IsBonus: true));
            }),
            new MonsterSpawn(146, new(11, 14)));
        r.SpawnPortal(_room7, new(1, 12), _room8, new(23, 11));
        r.PlaceButton(_room7, 1, new(18, 12), 1000, 1045);
        r.SpawnPortal(_room8, new(1, 11), _room8, new(1, 11), locked: false);
        r.SpawnPortal(_room8, new(23, 11), _room7, new(1, 12), locked: false);
        r.SpawnPortal(_room9, new(1, 12), _room8, new(23, 0), locked: false);
        r.SpawnPortal(_room9, new(12, 1), _room9, new(12, 1), locked: false);
        r.SpawnPortal(_room10, new(12, 23), _room9, new(12, 0));
        r.SpawnPortal(_room10, new(12, 1), _room11, new(12, 23));
        r.PlaceButton(_room10, 1, new(10, 10), 1000, 1045);
        r.Summon(_room10,
            new MonsterSpawn(146, new(7, 9)),
            new MonsterSpawn(146, new(8, 7), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10,
                new MonsterSpawn(146, new(17, 9), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(149, new(15, 5), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room10,
                        new MonsterSpawn(152, new(14, 14), IsBonus: true),
                        new MonsterSpawn(152, new(10, 14), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(146, new(15, 7), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(152, new(6, 12), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(152, new(18, 13), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(149, new(9, 5)));
        r.SpawnPortal(_room11, new(12, 23), _room10, new(12, 1));
        r.SpawnPortal(_room11, new(23, 12), _room12, new(1, 11));
        r.Summon(_room11,
            new MonsterSpawn(146, new(12, 10), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(149, new(17, 11), IsBonus: true),
                new MonsterSpawn(146, new(15, 18), IsBonus: true),
                new MonsterSpawn(146, new(9, 11), IsBonus: true));
            }),
            new MonsterSpawn(146, new(17, 16)));
        r.SpawnPortal(_room12, new(1, 11), _room11, new(23, 12));
        r.SpawnExit(_room12, new(23, 11));
        r.Summon(_room12,
            new MonsterSpawn(146, new(9, 5)),
            new MonsterSpawn(146, new(19, 5)),
            new MonsterSpawn(146, new(18, 18), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(149, new(20, 6), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room12,
                    new MonsterSpawn(152, new(20, 16), IsBonus: true),
                    new MonsterSpawn(152, new(18, 6)),
                    new MonsterSpawn(152, new(11, 6)));
                }),
                new MonsterSpawn(149, new(9, 7)));
            }));
        r.SummonNpcs(_room12, new NpcSpawn(389, new(14, 11)));
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
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room6)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room5);
            r.UnlockPortal(_room6, _room7);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room7, _room8);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room10);
            r.UnlockPortal(_room11, _room12);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room11);
            r.UnlockExit(_room12);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room7 && button == 1)
        {
            r.Summon(_room7,
                        new MonsterSpawn(146, new(8, 8), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room7,
                            new MonsterSpawn(146, new(14, 8)),
                            new MonsterSpawn(146, new(8, 16), IsBonus: true),
                            new MonsterSpawn(153, new(12, 12), IsBonus: true));
                        }),
                        new MonsterSpawn(146, new(15, 16), IsBonus: true));
        }
        else if (room == _room10 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room9);
            r.UnlockPortal(_room10, _room11);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace628();
