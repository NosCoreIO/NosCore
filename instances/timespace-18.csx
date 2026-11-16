public class TimeSpace18 : InstanceScript
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
    private readonly int _room13;
    private readonly int _room14;
    private readonly int _room15;

    public TimeSpace18()
    {
        Definition = InstanceDefinitionBuilder.Named(18, "Hidden Schemes", "Hidden Schemes")
            .ForLevels(28, 99)
            .StartingAt(7, 9)
            .Requiring(1012, 2)
            .WithLives(2)
            .WithDraw(2034, 3)
            .WithDraw(2043, 3)
            .WithSpecialReward(215, 1)
            .WithReward(2132, 1)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 0, 6)
            .WithRoom(4100, out _room2, 0, 7)
            .WithRoom(4100, out _room3, 1, 7)
            .WithRoom(4100, out _room4, 2, 7)
            .WithRoom(4100, out _room5, 3, 7)
            .WithRoom(4100, out _room6, 3, 8)
            .WithRoom(4100, out _room7, 2, 8)
            .WithRoom(4100, out _room8, 1, 8)
            .WithRoom(4100, out _room9, 0, 8)
            .WithRoom(4100, out _room10, 0, 9)
            .WithRoom(4100, out _room11, 1, 9)
            .WithRoom(4100, out _room12, 2, 9)
            .WithRoom(4102, out _room13, 1, 10)
            .WithRoom(4102, out _room14, 1, 11)
            .WithRoom(4102, out _room15, 2, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(120));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(311, new(16, 12)), new NpcSpawn(311, new(15, 10)), new NpcSpawn(311, new(12, 12)));
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(14, 28), _room7, new(14, 1));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15));
        r.Summon(_room4,
            new MonsterSpawn(338, new(21, 14), IsBonus: true),
            new MonsterSpawn(338, new(17, 20)),
            new MonsterSpawn(338, new(16, 10)));
        r.SpawnPortal(_room5, new(14, 28), _room6, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room6, new(1, 15), _room7, new(28, 15));
        r.Summon(_room6,
            new MonsterSpawn(338, new(19, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(338, new(11, 22)),
                new MonsterSpawn(338, new(15, 22), IsBonus: true),
                new MonsterSpawn(338, new(22, 22)));
            }),
            new MonsterSpawn(338, new(15, 10)),
            new MonsterSpawn(338, new(11, 10)));
        r.SpawnPortal(_room7, new(14, 1), _room4, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room6, new(1, 15), locked: false);
        r.SpawnPortal(_room7, new(1, 15), _room8, new(28, 15), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room7, new(1, 15));
        r.SpawnPortal(_room8, new(1, 15), _room9, new(28, 15));
        r.Summon(_room8,
            new MonsterSpawn(338, new(17, 11)),
            new MonsterSpawn(338, new(17, 9), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(338, new(17, 16), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(338, new(7, 8)));
                }),
                new MonsterSpawn(338, new(17, 19), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(338, new(8, 12), OnDeath: r =>
                    {
                        r.Summon(_room8,
                        new MonsterSpawn(338, new(8, 19)),
                        new MonsterSpawn(338, new(8, 22)));
                    }));
                }));
            }));
        r.SpawnPortal(_room9, new(28, 15), _room8, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(14, 28), _room10, new(14, 1), locked: false);
        r.SpawnPortal(_room10, new(14, 1), _room9, new(14, 28));
        r.SpawnPortal(_room10, new(28, 15), _room11, new(1, 15));
        r.Summon(_room10,
            new MonsterSpawn(72, new(25, 15)),
            new MonsterSpawn(72, new(20, 17)),
            new MonsterSpawn(72, new(10, 18), OnDeath: r =>
            {
                r.Summon(_room10,
                new MonsterSpawn(72, new(13, 20), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(72, new(7, 12)));
                }),
                new MonsterSpawn(72, new(17, 20), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(72, new(23, 20)));
                }));
            }),
            new MonsterSpawn(72, new(6, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10,
                new MonsterSpawn(72, new(7, 20)),
                new MonsterSpawn(72, new(21, 12)));
            }));
        r.SpawnPortal(_room11, new(28, 15), _room12, new(1, 15), locked: false);
        r.SpawnPortal(_room11, new(14, 28), _room13, new(14, 1));
        r.SpawnPortal(_room11, new(1, 15), _room10, new(28, 15));
        r.SpawnExit(_room12, new(28, 15));
        r.SpawnPortal(_room12, new(1, 15), _room11, new(28, 15));
        r.Summon(_room12, new MonsterSpawn(338, new(20, 15)));
        r.SpawnPortal(_room13, new(14, 1), _room11, new(14, 28));
        r.SpawnPortal(_room13, new(14, 28), _room14, new(14, 1), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(338, new(14, 12)),
            new MonsterSpawn(338, new(9, 23), IsBonus: true),
            new MonsterSpawn(338, new(9, 21)),
            new MonsterSpawn(338, new(9, 19)),
            new MonsterSpawn(338, new(9, 17)),
            new MonsterSpawn(338, new(9, 15)),
            new MonsterSpawn(338, new(9, 13), IsBonus: true),
            new MonsterSpawn(338, new(7, 10)),
            new MonsterSpawn(338, new(9, 7)),
            new MonsterSpawn(338, new(10, 6)),
            new MonsterSpawn(338, new(6, 21)),
            new MonsterSpawn(338, new(6, 21)),
            new MonsterSpawn(338, new(6, 18)),
            new MonsterSpawn(338, new(6, 15)),
            new MonsterSpawn(338, new(6, 14)),
            new MonsterSpawn(338, new(6, 11)),
            new MonsterSpawn(338, new(6, 9)));
        r.SpawnPortal(_room14, new(14, 1), _room13, new(14, 28), locked: false);
        r.SpawnPortal(_room14, new(28, 14), _room15, new(1, 14), locked: false);
        r.Summon(_room14,
            new MonsterSpawn(338, new(18, 21)),
            new MonsterSpawn(338, new(18, 18)),
            new MonsterSpawn(338, new(17, 15)),
            new MonsterSpawn(338, new(12, 17), IsBonus: true),
            new MonsterSpawn(338, new(15, 15)),
            new MonsterSpawn(338, new(13, 20)),
            new MonsterSpawn(338, new(12, 18)),
            new MonsterSpawn(338, new(11, 14)),
            new MonsterSpawn(338, new(10, 21)),
            new MonsterSpawn(338, new(9, 18)),
            new MonsterSpawn(338, new(9, 16)),
            new MonsterSpawn(338, new(8, 21)),
            new MonsterSpawn(338, new(6, 19)),
            new MonsterSpawn(338, new(8, 15)),
            new MonsterSpawn(338, new(8, 15)));
        r.SpawnExit(_room15, new(28, 14), locked: false);
        r.SpawnPortal(_room15, new(1, 14), _room14, new(28, 14), locked: false);
        r.SummonNpcs(_room15, new NpcSpawn(318, new(14, 14)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6089);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6090);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6092);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6094);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6096);
            r.Objective(_room10, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6097);
            r.Objective(_room12, LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_PORTAL_OPENS);
            r.StartRoomClock(_room12, TimeSpan.FromSeconds(60));
            Reinforce(r);
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), Reinforce);
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.UnlockPortal(_room12, _room11);
                r.UnlockPortal(_room11, _room13);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(Game18NConstString.QuickMissionComplete);
                r.StopRoomClock(_room12);
            });
        }
        else if (room == _room13)
        {
            r.Dialog(_room13, 6098);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6099);
        }
        else if (room == _room15)
        {
            r.Dialog(_room15, 6100);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6091);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.UnlockPortal(_room6, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room6, 6093);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room9);
            r.UnlockPortal(_room8, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room8, 6095);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room11);
            r.UnlockPortal(_room10, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
    }

    // Room 12 holds the party for a minute while two waves come through it.
    private void Reinforce(IInstanceRun r)
    {
        if (r.PlayersIn(_room12) == 0)
        {
            return;
        }

        r.Message(Game18NConstString.EnemiesReinforcements);
        r.Summon(_room12,
            new MonsterSpawn(338, new(11, 20)),
            new MonsterSpawn(338, new(11, 22)),
            new MonsterSpawn(338, new(12, 20)),
            new MonsterSpawn(338, new(13, 10)),
            new MonsterSpawn(338, new(13, 22)),
            new MonsterSpawn(338, new(14, 20)),
            new MonsterSpawn(338, new(15, 10)),
            new MonsterSpawn(338, new(16, 20)));
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace18();
