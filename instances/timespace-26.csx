public class TimeSpace26 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace26()
    {
        Definition = InstanceDefinitionBuilder.Named(26, "La bataille des d�mons", "La bataille des d�mons")
            .ForLevels(36, 99)
            .StartingAt(6, 10)
            .Requiring(1012, 3)
            .WithLives(2)
            .WithDraw(137, 1)
            .WithDraw(144, 1)
            .WithDraw(151, 1)
            .WithSpecialReward(2035, 15)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(5203, out _room1, 0, 8)
            .WithRoom(5201, out _room2, 1, 8)
            .WithRoom(5208, out _room3, 1, 9)
            .WithRoom(5204, out _room4, 2, 9)
            .WithRoom(5207, out _room5, 3, 9)
            .WithRoom(5208, out _room6, 3, 10)
            .WithRoom(5201, out _room7, 4, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(230));
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 27), locked: false);
        r.SpawnPortal(_room2, new(25, 48), _room3, new(12, 1));
        r.SpawnPortal(_room2, new(1, 27), _room1, new(23, 12));
        r.Summon(_room2,
            new MonsterSpawn(151, new(31, 42), IsBonus: true),
            new MonsterSpawn(145, new(31, 26)),
            new MonsterSpawn(151, new(24, 46)),
            new MonsterSpawn(145, new(24, 19), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(148, new(30, 36), OnDeath: r =>
                {
                    r.Summon(_room2,
                    new MonsterSpawn(145, new(28, 22)),
                    new MonsterSpawn(151, new(28, 46), OnDeath: r =>
                    {
                        r.Summon(_room2,
                        new MonsterSpawn(145, new(30, 37)),
                        new MonsterSpawn(145, new(30, 43)),
                        new MonsterSpawn(145, new(31, 47)));
                    }),
                    new MonsterSpawn(148, new(30, 31)));
                }));
            }),
            new MonsterSpawn(145, new(30, 16), IsBonus: true),
            new MonsterSpawn(145, new(30, 25)),
            new MonsterSpawn(145, new(30, 32)));
        r.SummonNpcs(_room2, new NpcSpawn(311, new(25, 34)), new NpcSpawn(311, new(22, 45)), new NpcSpawn(311, new(22, 42)), new NpcSpawn(311, new(22, 39)), new NpcSpawn(311, new(22, 36)), new NpcSpawn(311, new(22, 33)), new NpcSpawn(311, new(22, 29)), new NpcSpawn(311, new(22, 26)), new NpcSpawn(311, new(20, 45)), new NpcSpawn(311, new(20, 42)), new NpcSpawn(311, new(20, 39)), new NpcSpawn(311, new(20, 36)), new NpcSpawn(311, new(20, 33)), new NpcSpawn(311, new(20, 29)), new NpcSpawn(311, new(20, 26)));
        r.SpawnPortal(_room3, new(12, 1), _room2, new(25, 48), locked: false);
        r.SpawnPortal(_room3, new(23, 12), _room4, new(1, 12), locked: false);
        r.Summon(_room3, new MonsterSpawn(145, new(11, 5), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(145, new(14, 6)),
                new MonsterSpawn(145, new(14, 10), IsBonus: true));
            }));
        r.SummonNpcs(_room3, new NpcSpawn(311, new(21, 12)), new NpcSpawn(311, new(15, 13)), new NpcSpawn(311, new(17, 8)), new NpcSpawn(311, new(16, 15)), new NpcSpawn(311, new(18, 10)));
        r.SpawnPortal(_room4, new(23, 12), _room5, new(1, 12), locked: false);
        r.SpawnPortal(_room4, new(1, 12), _room3, new(23, 12), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(148, new(19, 11), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(148, new(20, 14), IsBonus: true));
            }),
            new MonsterSpawn(148, new(13, 13)));
        r.SpawnPortal(_room5, new(12, 23), _room6, new(12, 1), locked: false);
        r.SpawnPortal(_room5, new(1, 12), _room4, new(23, 12), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(148, new(8, 16)),
            new MonsterSpawn(145, new(2, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(148, new(10, 18)));
            }));
        r.SpawnPortal(_room6, new(12, 1), _room5, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(23, 12), _room7, new(1, 27), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(151, new(21, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(145, new(17, 8)));
            }),
            new MonsterSpawn(148, new(11, 9)),
            new MonsterSpawn(145, new(12, 10), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(151, new(17, 14)));
            }));
        r.SpawnExit(_room7, new(48, 25));
        r.SpawnPortal(_room7, new(1, 27), _room6, new(23, 12));
        r.Summon(_room7,
            new MonsterSpawn(145, new(32, 33)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(146, new(30, 38)),
            new MonsterSpawn(146, new(31, 28)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 41)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(146, new(30, 38)),
            new MonsterSpawn(146, new(31, 28)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 41)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(146, new(30, 38)),
            new MonsterSpawn(145, new(31, 25)),
            new MonsterSpawn(146, new(31, 28)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 41)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(146, new(30, 38)),
            new MonsterSpawn(145, new(31, 25)),
            new MonsterSpawn(146, new(31, 28)),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(148, new(31, 41)),
            new MonsterSpawn(148, new(31, 45)),
            new MonsterSpawn(145, new(33, 20)));
        r.SummonNpcs(_room7, new NpcSpawn(312, new(32, 15)), new NpcSpawn(311, new(25, 6)), new NpcSpawn(312, new(23, 44)), new NpcSpawn(312, new(23, 39)), new NpcSpawn(312, new(23, 34)), new NpcSpawn(312, new(23, 26)), new NpcSpawn(311, new(21, 26)), new NpcSpawn(311, new(20, 44)), new NpcSpawn(311, new(20, 34)), new NpcSpawn(311, new(20, 29)), new NpcSpawn(311, new(19, 38)), new NpcSpawn(312, new(17, 22)), new NpcSpawn(253, new(30, 27)), new NpcSpawn(253, new(31, 27)), new NpcSpawn(253, new(31, 46)), new NpcSpawn(253, new(32, 25)), new NpcSpawn(253, new(34, 23)), new NpcSpawn(253, new(36, 23)), new NpcSpawn(253, new(36, 31)), new NpcSpawn(253, new(37, 21)), new NpcSpawn(253, new(30, 27)), new NpcSpawn(253, new(31, 27)), new NpcSpawn(253, new(31, 46)), new NpcSpawn(253, new(32, 25)), new NpcSpawn(253, new(34, 23)), new NpcSpawn(253, new(36, 23)), new NpcSpawn(253, new(36, 31)), new NpcSpawn(253, new(37, 21)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6125);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6127);
            r.Objective(_room7, LanguageKey.TIMESPACE_GOAL_MONSTERS_VANISH_IN_THREE_MINUTES);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(240));
            r.Message(_room2, Game18NConstString.ADoorHasOpened);
            r.Dialog(_room2, 6126);
        }
        else if (room == _room7)
        {
            r.UnlockExit(_room7);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace26();
