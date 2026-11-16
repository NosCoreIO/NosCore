public class TimeSpace14 : InstanceScript
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

    private static readonly Spot[] LeverSpots = [new(16, 12), new(19, 15), new(22, 23)];

    public TimeSpace14()
    {
        Definition = InstanceDefinitionBuilder.Named(14, "The Mine Manager", "The Mine Manager")
            .ForLevels(24, 99)
            .StartingAt(9, 15)
            .Requiring(1012, 2)
            .WithLives(3)
            .WithDraw(717, 1)
            .WithDraw(709, 1)
            .WithSpecialReward(1013, 10)
            .WithReward(2028, 5)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 0, 7)
            .WithRoom(4100, out _room2, 1, 7)
            .WithRoom(2103, out _room3, 2, 7)
            .WithRoom(4102, out _room4, 3, 7)
            .WithRoom(4103, out _room5, 3, 8)
            .WithRoom(4102, out _room6, 3, 9)
            .WithRoom(4105, out _room7, 3, 10)
            .WithRoom(4107, out _room8, 4, 10)
            .WithRoom(4111, out _room9, 4, 9)
            .WithRoom(4112, out _room10, 5, 9)
            .WithRoom(4102, out _room11, 2, 10)
            .WithRoom(4108, out _room12, 3, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(8, 26), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(41, 26), _room4, new(1, 14));
        r.SpawnPortal(_room3, new(8, 26), _room2, new(28, 15));
        r.PlaceButton(_room3, 1, new(37, 10), 1000, 1045);
        r.Summon(_room3,
            new MonsterSpawn(107, new(39, 31)),
            new MonsterSpawn(19, new(39, 15)),
            new MonsterSpawn(107, new(37, 29)),
            new MonsterSpawn(107, new(37, 25)),
            new MonsterSpawn(107, new(36, 21)),
            new MonsterSpawn(19, new(37, 12)),
            new MonsterSpawn(107, new(34, 37)),
            new MonsterSpawn(107, new(33, 26)),
            new MonsterSpawn(107, new(35, 22)),
            new MonsterSpawn(107, new(34, 20)),
            new MonsterSpawn(107, new(32, 38)),
            new MonsterSpawn(107, new(32, 34)),
            new MonsterSpawn(111, new(32, 26)));
        r.SummonNpcs(_room3, new NpcSpawn(321, new(16, 29)), new NpcSpawn(321, new(16, 17)), new NpcSpawn(321, new(14, 17)), new NpcSpawn(321, new(14, 11)), new NpcSpawn(321, new(18, 10)), new NpcSpawn(321, new(14, 25)), new NpcSpawn(321, new(14, 21)), new NpcSpawn(321, new(14, 17)), new NpcSpawn(321, new(14, 14)), new NpcSpawn(321, new(12, 30)), new NpcSpawn(321, new(12, 29)), new NpcSpawn(321, new(14, 23)), new NpcSpawn(321, new(12, 14)), new NpcSpawn(321, new(14, 11)), new NpcSpawn(321, new(14, 13)));
        r.SpawnPortal(_room4, new(14, 28), _room5, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(1, 14), _room3, new(41, 26), locked: false);
        r.SummonNpcs(_room4, new NpcSpawn(332, new(15, 15)));
        r.SpawnPortal(_room5, new(14, 1), _room4, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(14, 28), _room6, new(14, 1), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(56, new(19, 15)),
            new MonsterSpawn(56, new(12, 19)),
            new MonsterSpawn(56, new(10, 10)));
        r.SpawnPortal(_room6, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room6, new(14, 28), _room7, new(14, 1));
        r.Summon(_room6,
            new MonsterSpawn(20, new(14, 15)),
            new MonsterSpawn(136, new(12, 13), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(136, new(16, 13), OnDeath: r =>
                {
                    r.Summon(_room6,
                    new MonsterSpawn(136, new(20, 14), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(58, new(20, 16)));
                    }),
                    new MonsterSpawn(58, new(24, 13)));
                }));
            }));
        r.SpawnPortal(_room7, new(14, 1), _room6, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room8, new(1, 14));
        r.SpawnPortal(_room7, new(14, 28), _room12, new(14, 5), locked: false);
        r.SpawnPortal(_room7, new(1, 14), _room11, new(28, 14), locked: false);
        r.SpawnPortal(_room8, new(14, 1), _room9, new(14, 28));
        r.SpawnPortal(_room8, new(1, 14), _room7, new(28, 15));
        r.PlaceButton(_room8, 1, LeverSpots[Random.Shared.Next(LeverSpots.Length)], 1000, 1045);
        r.Summon(_room8,
            new MonsterSpawn(136, new(14, 20)),
            new MonsterSpawn(136, new(10, 16)),
            new MonsterSpawn(58, new(7, 12), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(58, new(17, 15)),
                new MonsterSpawn(58, new(21, 13), IsBonus: true));
            }));
        r.SpawnPortal(_room9, new(28, 15), _room10, new(1, 15));
        r.SpawnPortal(_room9, new(14, 28), _room8, new(14, 1));
        r.Summon(_room9,
            new MonsterSpawn(56, new(10, 17), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(56, new(14, 22), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(56, new(14, 6)));
                }));
            }),
            new MonsterSpawn(56, new(13, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(136, new(10, 14)),
                new MonsterSpawn(136, new(13, 18), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(136, new(13, 10)));
                }));
            }));
        r.SpawnExit(_room10, new(14, 1), locked: false);
        r.SpawnPortal(_room10, new(1, 15), _room9, new(28, 15), locked: false);
        r.SpawnPortal(_room11, new(28, 14), _room7, new(1, 14));
        r.PlaceButton(_room11, 1, new(16, 24), 1000, 1045);
        r.Summon(_room11,
            new MonsterSpawn(58, new(16, 17), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(66, new(10, 14), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(140, new(7, 16), OnDeath: r =>
                    {
                        r.Summon(_room11, new MonsterSpawn(140, new(10, 10)));
                    }));
                }),
                new MonsterSpawn(66, new(19, 16)));
            }),
            new MonsterSpawn(58, new(14, 10), IsBonus: true));
        r.SpawnPortal(_room12, new(14, 5), _room7, new(14, 28));
        r.Summon(_room12,
            new MonsterSpawn(102, new(21, 13), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(136, new(11, 11)),
                new MonsterSpawn(136, new(15, 23)));
            }),
            new MonsterSpawn(102, new(16, 13)),
            new MonsterSpawn(102, new(13, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(136, new(23, 17)));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6075);
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(600));
            for (var wave = 0; wave < 20; wave++)
            {
                var large = wave >= 2;
                r.At(r.Elapsed + TimeSpan.FromSeconds(30 * wave), r => Reinforce(r, large));
            }
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6076);
        }
        else if (room == _room8)
        {
            r.Message(LanguageKey.TIMESPACE_LEVER_OVER_THERE);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6077);
            r.Message(LanguageKey.TIMESPACE_EXIT_IN_SIGHT);
        }
        else if (room == _room11)
        {
            r.Message(LanguageKey.TIMESPACE_PULL_LEVER_OPEN_PORTAL);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room7, _room8);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room10);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room12)
        {
            r.UnlockPortal(_room12, _room7);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room8 && button == 1)
        {
            r.UnlockPortal(_room8, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(LanguageKey.TIMESPACE_LEVER_OVER_THERE);
        }
        else if (room == _room11 && button == 1)
        {
            r.UnlockPortal(_room11, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(LanguageKey.TIMESPACE_PULL_LEVER_OPEN_PORTAL);
        }
    }

    // The mine is attacked every 30 seconds while someone is in it, and harder from the third wave on.
    private void Reinforce(IInstanceRun r, bool large)
    {
        if (r.PlayersIn(_room3) == 0)
        {
            return;
        }

        r.Message(Game18NConstString.EnemiesReinforcements);
        r.Summon(_room3,
            new MonsterSpawn(105, new(30, 27)),
            new MonsterSpawn(105, new(32, 19)),
            new MonsterSpawn(108, new(32, 24)),
            new MonsterSpawn(108, new(32, 33)));
        if (large)
        {
            r.Summon(_room3,
                new MonsterSpawn(105, new(30, 32)),
                new MonsterSpawn(109, new(33, 34)));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace14();
