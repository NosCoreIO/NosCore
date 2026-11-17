public class TimeSpace24 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace24()
    {
        Definition = InstanceDefinitionBuilder.Named(24, "La lutte pour Fernon", "La lutte pour Fernon")
            .ForLevels(34, 99)
            .StartingAt(15, 9)
            .Requiring(1012, 3)
            .WithLives(2)
            .WithDraw(164, 1)
            .WithDraw(170, 1)
            .WithDraw(176, 1)
            .WithSpecialReward(2047, 15)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 8)
            .WithRoom(2100, out _room2, 1, 8)
            .WithRoom(2100, out _room3, 2, 8)
            .WithRoom(4205, out _room4, 3, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(400));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(9, 36), locked: false);
        r.SpawnPortal(_room2, new(58, 37), _room3, new(9, 36));
        r.SpawnPortal(_room2, new(9, 36), _room1, new(28, 15));
        r.Summon(_room2,
            new MonsterSpawn(112, new(39, 29)),
            new MonsterSpawn(20, new(34, 51)),
            new MonsterSpawn(20, new(34, 47)),
            new MonsterSpawn(20, new(34, 44)),
            new MonsterSpawn(20, new(34, 39)),
            new MonsterSpawn(20, new(34, 35)),
            new MonsterSpawn(20, new(34, 32)),
            new MonsterSpawn(20, new(34, 29)),
            new MonsterSpawn(20, new(34, 25)),
            new MonsterSpawn(20, new(34, 22)),
            new MonsterSpawn(20, new(34, 19)));
        r.SummonNpcs(_room2, new NpcSpawn(310, new(22, 40)), new NpcSpawn(310, new(22, 37)), new NpcSpawn(310, new(22, 34)), new NpcSpawn(310, new(22, 31)), new NpcSpawn(310, new(22, 27)), new NpcSpawn(310, new(22, 24)), new NpcSpawn(310, new(22, 20)), new NpcSpawn(312, new(19, 21)), new NpcSpawn(312, new(18, 45)), new NpcSpawn(312, new(18, 41)));
        r.SpawnPortal(_room3, new(58, 37), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(9, 36), _room2, new(58, 37));
        r.Summon(_room3,
            new MonsterSpawn(112, new(37, 41), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(110, new(35, 41)),
                new MonsterSpawn(110, new(36, 32)));
            }),
            new MonsterSpawn(112, new(37, 38), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(110, new(33, 31)),
                new MonsterSpawn(110, new(35, 26), IsBonus: true));
            }),
            new MonsterSpawn(112, new(37, 35), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(110, new(33, 35), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(110, new(36, 28)));
                }));
            }),
            new MonsterSpawn(112, new(37, 31)),
            new MonsterSpawn(112, new(37, 27)),
            new MonsterSpawn(110, new(34, 38)),
            new MonsterSpawn(110, new(34, 34), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(110, new(35, 36)));
            }),
            new MonsterSpawn(110, new(34, 29)));
        r.SummonNpcs(_room3, new NpcSpawn(254, new(39, 39)), new NpcSpawn(254, new(39, 35)), new NpcSpawn(254, new(39, 31)), new NpcSpawn(254, new(39, 26)), new NpcSpawn(254, new(38, 43)), new NpcSpawn(254, new(35, 23)), new NpcSpawn(254, new(33, 40)), new NpcSpawn(254, new(33, 25)), new NpcSpawn(254, new(32, 37)), new NpcSpawn(254, new(32, 29)), new NpcSpawn(338, new(28, 26)), new NpcSpawn(338, new(28, 23)), new NpcSpawn(338, new(27, 35)), new NpcSpawn(338, new(27, 31)));
        r.SpawnExit(_room4, new(28, 15), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(58, 37), locked: false);
        r.SummonNpcs(_room4, new NpcSpawn(310, new(14, 10)), new NpcSpawn(310, new(14, 20)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(240));
            r.Message(_room2, Game18NConstString.EnemiesReinforcements);
            r.Dialog(_room2, 6119);
            r.Objective(_room2, LanguageKey.TIMESPACE_GOAL_FLEE_AFTER_FOUR_MINUTES);
            r.Summon(_room2,
                new MonsterSpawn(110, new(47, 22)),
                new MonsterSpawn(110, new(10, 23)),
                new MonsterSpawn(112, new(31, 17)),
                new MonsterSpawn(112, new(13, 29)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(41, 40)),
                    new MonsterSpawn(110, new(46, 20)),
                    new MonsterSpawn(112, new(55, 21)),
                    new MonsterSpawn(112, new(49, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(43), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(106, new(36, 30)),
                    new MonsterSpawn(106, new(37, 30)),
                    new MonsterSpawn(109, new(36, 32)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(17, 57)),
                    new MonsterSpawn(110, new(19, 51)),
                    new MonsterSpawn(112, new(38, 30)),
                    new MonsterSpawn(112, new(31, 59)),
                    new MonsterSpawn(113, new(47, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(64), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(106, new(34, 30)),
                    new MonsterSpawn(106, new(35, 31)),
                    new MonsterSpawn(109, new(35, 29)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(24, 29)),
                    new MonsterSpawn(110, new(45, 54)),
                    new MonsterSpawn(112, new(48, 42)),
                    new MonsterSpawn(112, new(22, 59)),
                    new MonsterSpawn(113, new(47, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(112), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(106, new(22, 52)),
                    new MonsterSpawn(106, new(24, 52)),
                    new MonsterSpawn(109, new(24, 54)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(119), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(106, new(25, 51)),
                    new MonsterSpawn(106, new(24, 52)),
                    new MonsterSpawn(109, new(24, 50)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(27, 35)),
                    new MonsterSpawn(110, new(28, 17)),
                    new MonsterSpawn(112, new(56, 13)),
                    new MonsterSpawn(112, new(30, 13)),
                    new MonsterSpawn(110, new(43, 19)),
                    new MonsterSpawn(113, new(47, 33)),
                    new MonsterSpawn(113, new(47, 46)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(137), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(107, new(27, 44)),
                    new MonsterSpawn(107, new(28, 45)),
                    new MonsterSpawn(110, new(27, 46)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(18, 20)),
                    new MonsterSpawn(110, new(57, 13)),
                    new MonsterSpawn(112, new(39, 32)),
                    new MonsterSpawn(112, new(52, 16)),
                    new MonsterSpawn(110, new(43, 19)),
                    new MonsterSpawn(113, new(47, 33)),
                    new MonsterSpawn(113, new(47, 46)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(154), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(106, new(50, 15)),
                    new MonsterSpawn(106, new(48, 17)),
                    new MonsterSpawn(109, new(49, 15)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(110, new(51, 20)),
                    new MonsterSpawn(110, new(45, 43)),
                    new MonsterSpawn(112, new(39, 43)),
                    new MonsterSpawn(112, new(15, 26)),
                    new MonsterSpawn(110, new(43, 19)),
                    new MonsterSpawn(113, new(47, 33)),
                    new MonsterSpawn(113, new(47, 46)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(192), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(107, new(13, 43)),
                    new MonsterSpawn(107, new(13, 45)),
                    new MonsterSpawn(110, new(13, 45)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(210), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(112, new(36, 52)),
                    new MonsterSpawn(112, new(22, 12)),
                    new MonsterSpawn(110, new(43, 19)),
                    new MonsterSpawn(113, new(47, 33)),
                    new MonsterSpawn(113, new(47, 46)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(240), r =>
            {
                r.Dismiss(_room2, 20);
                r.Dismiss(_room2, 106);
                r.Dismiss(_room2, 107);
                r.Dismiss(_room2, 109);
                r.Dismiss(_room2, 110);
                r.Dismiss(_room2, 112);
                r.Dismiss(_room2, 113);
                r.UnlockPortal(_room2, _room3);
                r.Message(_room2, Game18NConstString.ADoorHasOpened);
                r.Dialog(_room2, 6120);
                r.StopRoomClock(_room2);
            });
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6121);
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_CUARRY_KNIGHTS_AND_DEMONS);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6122);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(_room3, Game18NConstString.ADoorHasOpened);
            r.Message(_room3, LanguageKey.TIMESPACE_DEMONS_CRUSH_KOVOLTS);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace24();
