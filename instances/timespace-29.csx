public class TimeSpace29 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace29()
    {
        Definition = InstanceDefinitionBuilder.Named(29, "La trahison", "La trahison")
            .ForLevels(39, 99)
            .StartingAt(9, 13)
            .Requiring(1012, 3)
            .WithLives(3)
            .WithDraw(2047, 5)
            .WithDraw(2039, 5)
            .WithSpecialReward(2127, 20)
            .WithReward(2131, 2)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 7)
            .WithRoom(4205, out _room2, 1, 7)
            .WithRoom(2100, out _room3, 2, 7)
            .WithRoom(2100, out _room4, 3, 7)
            .WithRoom(2100, out _room5, 4, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(210));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(9, 36), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(58, 37), _room4, new(9, 36));
        r.SpawnPortal(_room3, new(9, 36), _room2, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(151, new(37, 36)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)),
            new MonsterSpawn(145, new(27, 19)),
            new MonsterSpawn(151, new(29, 42)),
            new MonsterSpawn(151, new(29, 45)),
            new MonsterSpawn(119, new(37, 52)),
            new MonsterSpawn(116, new(38, 31)));
        r.SummonNpcs(_room3, new NpcSpawn(310, new(45, 37)), new NpcSpawn(310, new(45, 30)), new NpcSpawn(310, new(44, 43)), new NpcSpawn(310, new(43, 23)), new NpcSpawn(311, new(40, 39)), new NpcSpawn(311, new(40, 33)), new NpcSpawn(312, new(38, 42)), new NpcSpawn(312, new(36, 28)), new NpcSpawn(312, new(35, 48)), new NpcSpawn(312, new(35, 34)), new NpcSpawn(310, new(34, 26)), new NpcSpawn(311, new(34, 19)), new NpcSpawn(311, new(33, 41)), new NpcSpawn(311, new(32, 47)), new NpcSpawn(311, new(30, 37)), new NpcSpawn(311, new(30, 23)), new NpcSpawn(311, new(29, 33)), new NpcSpawn(311, new(28, 27)), new NpcSpawn(311, new(23, 47)), new NpcSpawn(312, new(21, 35)));
        r.SpawnPortal(_room4, new(58, 37), _room5, new(9, 36));
        r.SpawnPortal(_room4, new(9, 36), _room3, new(58, 37));
        r.Summon(_room4,
            new MonsterSpawn(119, new(48, 27), IsBonus: true),
            new MonsterSpawn(119, new(48, 21), IsBonus: true),
            new MonsterSpawn(122, new(43, 24)),
            new MonsterSpawn(122, new(42, 38)),
            new MonsterSpawn(116, new(37, 28), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(116, new(36, 52)),
                new MonsterSpawn(116, new(37, 16)),
                new MonsterSpawn(116, new(37, 22), OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(122, new(43, 31)),
                    new MonsterSpawn(122, new(43, 45)),
                    new MonsterSpawn(119, new(47, 43), OnDeath: r =>
                    {
                        r.Summon(_room4,
                        new MonsterSpawn(119, new(48, 36)),
                        new MonsterSpawn(119, new(48, 49), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(116, new(36, 47)),
            new MonsterSpawn(116, new(36, 42)),
            new MonsterSpawn(116, new(36, 34)));
        r.SummonNpcs(_room4, new NpcSpawn(311, new(27, 45)), new NpcSpawn(311, new(27, 40)), new NpcSpawn(311, new(27, 30)), new NpcSpawn(311, new(27, 23)), new NpcSpawn(311, new(26, 34)), new NpcSpawn(312, new(23, 24)), new NpcSpawn(310, new(23, 22)), new NpcSpawn(312, new(22, 51)), new NpcSpawn(312, new(22, 48)), new NpcSpawn(312, new(22, 44)), new NpcSpawn(312, new(22, 40)), new NpcSpawn(310, new(22, 36)), new NpcSpawn(310, new(22, 32)), new NpcSpawn(310, new(22, 27)), new NpcSpawn(310, new(22, 18)));
        r.SpawnPortal(_room5, new(9, 36), _room4, new(58, 37));
        r.Summon(_room5,
            new MonsterSpawn(121, new(39, 43)),
            new MonsterSpawn(121, new(37, 55), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(121, new(40, 34)),
                new MonsterSpawn(121, new(40, 48)),
                new MonsterSpawn(122, new(40, 57)),
                new MonsterSpawn(121, new(41, 17)));
            }),
            new MonsterSpawn(116, new(36, 27), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(119, new(45, 35)),
                new MonsterSpawn(119, new(45, 42), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(122, new(49, 53)),
                    new MonsterSpawn(122, new(51, 31)),
                    new MonsterSpawn(122, new(51, 46)));
                }),
                new MonsterSpawn(119, new(45, 50), IsBonus: true),
                new MonsterSpawn(122, new(49, 21)));
            }),
            new MonsterSpawn(116, new(36, 23), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(121, new(42, 23)),
                new MonsterSpawn(121, new(42, 28)),
                new MonsterSpawn(122, new(43, 14)),
                new MonsterSpawn(119, new(44, 28)),
                new MonsterSpawn(119, new(45, 21)));
            }),
            new MonsterSpawn(116, new(35, 45)),
            new MonsterSpawn(116, new(35, 34), IsBonus: true),
            new MonsterSpawn(116, new(34, 51)),
            new MonsterSpawn(122, new(34, 14), IsBonus: true));
        r.SummonNpcs(_room5, new NpcSpawn(311, new(24, 45)), new NpcSpawn(311, new(24, 39)), new NpcSpawn(311, new(24, 28)), new NpcSpawn(311, new(24, 23)), new NpcSpawn(311, new(23, 34)), new NpcSpawn(310, new(18, 47)), new NpcSpawn(310, new(18, 38)), new NpcSpawn(310, new(18, 29)), new NpcSpawn(310, new(18, 23)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Message(_room2, LanguageKey.TIMESPACE_HOUR_OF_WAR);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6131);
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_HELP_NOSVILLE_ALLIANCE);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6133);
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_DESTROY_KOVOLT_ALLIANCE);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6135);
            r.Objective(_room5, LanguageKey.TIMESPACE_GOAL_DESTROY_KOVOLT_ALLIANCE);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(240));
            r.Message(_room4, Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6134);
        }
        else if (room == _room5)
        {
            r.Message(_room5, Game18NConstString.QuickMissionComplete);
            r.Message(_room5, Game18NConstString.NewGoal);
            r.Dialog(_room5, 6136);
            r.Succeed();
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace29();
