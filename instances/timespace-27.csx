public class TimeSpace27 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace27()
    {
        Definition = InstanceDefinitionBuilder.Named(27, "Chasse aux monstres : 1er volet", "Chasse aux monstres : 1er volet")
            .ForLevels(37, 99)
            .StartingAt(7, 8)
            .Requiring(1012, 3)
            .WithLives(3)
            .WithDraw(1047, 2)
            .WithDraw(1072, 1)
            .WithSpecialReward(2039, 15)
            .WithReward(2134, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 1, 9)
            .WithRoom(2101, out _room2, 2, 9)
            .WithRoom(4209, out _room3, 3, 9)
            .WithRoom(2101, out _room4, 3, 10)
            .WithRoom(4208, out _room5, 2, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(720));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(5, 20), locked: false);
        r.SpawnPortal(_room2, new(35, 20), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(5, 20), _room1, new(28, 15), locked: false);
        r.Summon(_room2,
            new MonsterSpawn(65, new(22, 9)),
            new MonsterSpawn(65, new(5, 11)),
            new MonsterSpawn(16, new(34, 21)),
            new MonsterSpawn(16, new(20, 21)),
            new MonsterSpawn(16, new(16, 32)),
            new MonsterSpawn(16, new(14, 31)),
            new MonsterSpawn(65, new(12, 30)),
            new MonsterSpawn(65, new(22, 24)),
            new MonsterSpawn(16, new(13, 27)),
            new MonsterSpawn(16, new(22, 10)),
            new MonsterSpawn(58, new(25, 33)),
            new MonsterSpawn(58, new(14, 31)),
            new MonsterSpawn(58, new(14, 22)),
            new MonsterSpawn(65, new(16, 20)),
            new MonsterSpawn(65, new(20, 12)),
            new MonsterSpawn(198, new(8, 28)),
            new MonsterSpawn(198, new(33, 21)),
            new MonsterSpawn(198, new(33, 15)),
            new MonsterSpawn(198, new(18, 6)),
            new MonsterSpawn(198, new(16, 18)),
            new MonsterSpawn(16, new(25, 19)),
            new MonsterSpawn(16, new(21, 31)),
            new MonsterSpawn(58, new(27, 6)),
            new MonsterSpawn(58, new(13, 32)),
            new MonsterSpawn(58, new(16, 20)),
            new MonsterSpawn(65, new(9, 9)),
            new MonsterSpawn(65, new(23, 5)),
            new MonsterSpawn(16, new(25, 12)),
            new MonsterSpawn(16, new(12, 26)),
            new MonsterSpawn(58, new(20, 15)),
            new MonsterSpawn(58, new(22, 5)),
            new MonsterSpawn(58, new(33, 21)),
            new MonsterSpawn(65, new(31, 11)),
            new MonsterSpawn(65, new(21, 11)),
            new MonsterSpawn(142, new(19, 16)),
            new MonsterSpawn(142, new(23, 14)),
            new MonsterSpawn(198, new(25, 31)),
            new MonsterSpawn(198, new(21, 24)),
            new MonsterSpawn(198, new(31, 11)),
            new MonsterSpawn(198, new(17, 20)),
            new MonsterSpawn(198, new(32, 22)),
            new MonsterSpawn(58, new(27, 28)),
            new MonsterSpawn(58, new(7, 14)),
            new MonsterSpawn(58, new(28, 5)),
            new MonsterSpawn(142, new(23, 35)),
            new MonsterSpawn(142, new(30, 33)));
        r.SpawnPortal(_room3, new(14, 28), _room4, new(20, 5));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(35, 20));
        r.Summon(_room3,
            new MonsterSpawn(22, new(16, 17)),
            new MonsterSpawn(22, new(16, 10), IsBonus: true),
            new MonsterSpawn(137, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(142, new(16, 20), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(67, new(21, 18)));
                }));
            }),
            new MonsterSpawn(137, new(11, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(142, new(19, 14), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(142, new(13, 10)),
                    new MonsterSpawn(67, new(21, 11)));
                }));
            }));
        r.SpawnPortal(_room4, new(20, 5), _room3, new(14, 28));
        r.SpawnPortal(_room4, new(5, 20), _room5, new(28, 15));
        r.SpawnPortal(_room5, new(28, 15), _room4, new(5, 20), locked: false);
        r.SpawnExit(_room5, new(14, 28), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(142, new(18, 9), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(59, new(12, 18), OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(198, new(11, 13)),
                    new MonsterSpawn(198, new(23, 18), IsBonus: true),
                    new MonsterSpawn(198, new(13, 23)));
                }));
            }),
            new MonsterSpawn(59, new(15, 16), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(142, new(15, 19), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(198, new(11, 9)));
                }),
                new MonsterSpawn(142, new(21, 14)));
            }),
            new MonsterSpawn(59, new(8, 9)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(154));
            r.Objective(_room2, LanguageKey.TIMESPACE_GOAL_HUNT_FOR_THREE_MINUTES);
        }
        else if (room == _room3)
        {
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_KILL_ALL_MONSTERS);
        }
        else if (room == _room4)
        {
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(180));
            r.Message(_room4, Game18NConstString.EnemiesReinforcements);
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_SURVIVE_THREE_MINUTES_DOOR);
            r.Summon(_room4,
                new MonsterSpawn(22, new(35, 25)),
                new MonsterSpawn(22, new(34, 28)),
                new MonsterSpawn(56, new(13, 9)),
                new MonsterSpawn(56, new(15, 34)),
                new MonsterSpawn(56, new(25, 18)),
                new MonsterSpawn(56, new(10, 14)),
                new MonsterSpawn(59, new(16, 19)),
                new MonsterSpawn(59, new(7, 14)),
                new MonsterSpawn(59, new(16, 7)),
                new MonsterSpawn(137, new(34, 25)),
                new MonsterSpawn(137, new(31, 13)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(22, new(21, 26)),
                    new MonsterSpawn(22, new(13, 26)),
                    new MonsterSpawn(56, new(12, 28)),
                    new MonsterSpawn(56, new(21, 25)),
                    new MonsterSpawn(56, new(26, 7)),
                    new MonsterSpawn(56, new(16, 28)),
                    new MonsterSpawn(59, new(27, 7)),
                    new MonsterSpawn(59, new(33, 15)),
                    new MonsterSpawn(59, new(33, 18)),
                    new MonsterSpawn(137, new(14, 24)),
                    new MonsterSpawn(137, new(10, 32)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(22, new(16, 23)),
                    new MonsterSpawn(22, new(28, 31)),
                    new MonsterSpawn(56, new(17, 8)),
                    new MonsterSpawn(56, new(26, 8)),
                    new MonsterSpawn(56, new(6, 30)),
                    new MonsterSpawn(56, new(31, 19)),
                    new MonsterSpawn(59, new(24, 19)),
                    new MonsterSpawn(59, new(34, 28)),
                    new MonsterSpawn(59, new(13, 27)),
                    new MonsterSpawn(67, new(16, 15)),
                    new MonsterSpawn(137, new(19, 8)),
                    new MonsterSpawn(137, new(25, 6)),
                    new MonsterSpawn(198, new(22, 5)),
                    new MonsterSpawn(198, new(25, 18)),
                    new MonsterSpawn(198, new(11, 33)),
                    new MonsterSpawn(198, new(32, 22)),
                    new MonsterSpawn(198, new(25, 28)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(22, new(8, 13)),
                    new MonsterSpawn(22, new(16, 15)),
                    new MonsterSpawn(56, new(27, 22)),
                    new MonsterSpawn(56, new(9, 28)),
                    new MonsterSpawn(56, new(30, 15)),
                    new MonsterSpawn(56, new(15, 23)),
                    new MonsterSpawn(59, new(27, 9)),
                    new MonsterSpawn(59, new(30, 10)),
                    new MonsterSpawn(59, new(16, 33)),
                    new MonsterSpawn(67, new(21, 17)),
                    new MonsterSpawn(137, new(34, 29)),
                    new MonsterSpawn(137, new(16, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(22, new(24, 24)),
                    new MonsterSpawn(22, new(18, 5)),
                    new MonsterSpawn(56, new(34, 18)),
                    new MonsterSpawn(56, new(10, 16)),
                    new MonsterSpawn(56, new(5, 14)),
                    new MonsterSpawn(56, new(35, 19)),
                    new MonsterSpawn(59, new(32, 14)),
                    new MonsterSpawn(59, new(10, 22)),
                    new MonsterSpawn(59, new(23, 17)),
                    new MonsterSpawn(67, new(20, 20)),
                    new MonsterSpawn(137, new(22, 18)),
                    new MonsterSpawn(137, new(10, 17)),
                    new MonsterSpawn(198, new(8, 28)),
                    new MonsterSpawn(198, new(15, 6)),
                    new MonsterSpawn(198, new(27, 14)),
                    new MonsterSpawn(198, new(21, 24)),
                    new MonsterSpawn(198, new(22, 28)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(56, new(22, 26)),
                    new MonsterSpawn(56, new(26, 14)),
                    new MonsterSpawn(56, new(26, 6)),
                    new MonsterSpawn(56, new(28, 33)),
                    new MonsterSpawn(67, new(7, 20)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Dismiss(_room4, 22);
                r.Dismiss(_room4, 56);
                r.Dismiss(_room4, 59);
                r.Dismiss(_room4, 67);
                r.Dismiss(_room4, 137);
                r.Dismiss(_room4, 198);
                r.UnlockPortal(_room4, _room3);
                r.UnlockPortal(_room4, _room5);
                r.Message(_room4, Game18NConstString.ADoorHasOpened);
                r.Message(_room4, Game18NConstString.QuickMissionComplete);
                r.StopRoomClock(_room4);
            });
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
            r.Message(_room3, Game18NConstString.ADoorHasOpened);
            r.Message(_room3, Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace27();
