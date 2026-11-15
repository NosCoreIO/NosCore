// The Catsy People
public class TimeSpace46 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace46()
    {
        Definition = InstanceDefinitionBuilder.Named(46, "Welcome to West Mount Krem, home of the Koaren tribe. Lord Cuarry no longer seems interested in this place, since the monsters and the Kenkos leave the Koarens in peace. As you can see, it is full of time-space stones. Just like north of NosVille, the monsters seem to be troubling the people here too!", "The Catsy People")
            .ForLevels(56, 99)
            .StartingAt(13, 7)
            .WithLives(1)
            .Requiring(1012, 5)
            .WithDraw(715, 1)
            .WithDraw(533, 1)
            .WithSpecialReward(1033, 8)
            .WithReward(2099, 4)
            .WithReward(1087, 1)
            .WithRoom(4813, out _room1, 3, 11)
            .WithRoom(4812, out _room2, 3, 11)
            .WithRoom(4810, out _room3, 3, 10)
            .WithRoom(4812, out _room4, 4, 10)
            .WithRoom(4810, out _room5, 4, 9)
            .WithRoom(4808, out _room6, 5, 9)
            .WithRoom(4807, out _room7, 5, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(15, 0), _room2, new(15, 28), locked: false);
        r.SpawnPortal(_room2, new(15, 28), _room1, new(15, 0));
        r.SpawnPortal(_room2, new(29, 14), _room3, new(1, 14));
        r.Summon(_room2,
            new MonsterSpawn(79, new(9, 19)),
            new MonsterSpawn(79, new(15, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2,
                new MonsterSpawn(79, new(21, 19), OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(200, new(18, 9), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room2,
                        new MonsterSpawn(200, new(21, 14), IsBonus: true),
                        new MonsterSpawn(92, new(12, 14), OnDeath: r =>
                        {
                            r.Summon(_room2, new MonsterSpawn(92, new(18, 14)));
                        }));
                    }));
                }),
                new MonsterSpawn(200, new(11, 7), IsBonus: true));
            }));
        r.SpawnPortal(_room3, new(1, 14), _room2, new(29, 14));
        r.SpawnPortal(_room3, new(15, 0), _room4, new(15, 28));
        r.Summon(_room3,
            new MonsterSpawn(80, new(13, 11), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(88, new(11, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(232, new(21, 20), IsBonus: true));
                }));
            }),
            new MonsterSpawn(80, new(19, 18), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(232, new(13, 8), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(232, new(21, 10)));
                }));
            }),
            new MonsterSpawn(80, new(20, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(88, new(14, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(91, new(16, 13), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room4, new(15, 28), _room3, new(15, 0));
        r.SpawnPortal(_room4, new(29, 14), _room5, new(1, 14));
        r.Summon(_room4,
            new MonsterSpawn(91, new(12, 13)),
            new MonsterSpawn(88, new(14, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(91, new(20, 15), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(92, new(11, 8), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(92, new(19, 9), IsBonus: true));
                    }),
                    new MonsterSpawn(232, new(7, 15), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(232, new(20, 11), OnDeath: r =>
                        {
                            r.Summon(_room4, new MonsterSpawn(232, new(21, 19)));
                        }));
                    }));
                }),
                new MonsterSpawn(88, new(12, 15), IsBonus: true));
            }),
            new MonsterSpawn(91, new(17, 14)));
        r.SpawnPortal(_room5, new(1, 14), _room4, new(29, 14));
        r.SpawnPortal(_room5, new(15, 0), _room6, new(15, 28));
        r.Summon(_room5,
            new MonsterSpawn(135, new(13, 11), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(88, new(14, 15), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(88, new(15, 16)));
                }));
            }),
            new MonsterSpawn(135, new(19, 12), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(88, new(15, 18), OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(92, new(17, 15), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room5,
                        new MonsterSpawn(200, new(23, 13), IsBonus: true),
                        new MonsterSpawn(200, new(20, 9), IsBonus: true));
                    }),
                    new MonsterSpawn(92, new(11, 12), OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(200, new(17, 8)));
                    }));
                }));
            }),
            new MonsterSpawn(135, new(19, 18)),
            new MonsterSpawn(135, new(21, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(92, new(17, 19)));
            }));
        r.SpawnPortal(_room6, new(15, 28), _room5, new(15, 0));
        r.SpawnPortal(_room6, new(15, 0), _room7, new(15, 28));
        r.Summon(_room6,
            new MonsterSpawn(135, new(13, 11), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(92, new(17, 15), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(92, new(17, 19)));
                }),
                new MonsterSpawn(92, new(11, 12), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(200, new(17, 8), OnDeath: r =>
                    {
                        r.Summon(_room6,
                        new MonsterSpawn(200, new(23, 13)),
                        new MonsterSpawn(200, new(20, 9)));
                    }));
                }));
            }),
            new MonsterSpawn(135, new(19, 12), IsBonus: true),
            new MonsterSpawn(135, new(21, 19), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(88, new(14, 18), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(88, new(15, 18)));
                }));
            }),
            new MonsterSpawn(135, new(21, 14), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(88, new(15, 16)));
            }));
        r.SpawnPortal(_room7, new(15, 28), _room6, new(15, 0));
        r.SpawnExit(_room7, new(15, 0));
        r.Summon(_room7,
            new MonsterSpawn(244, new(21, 11), IsBonus: true),
            new MonsterSpawn(244, new(15, 11)),
            new MonsterSpawn(244, new(9, 11)),
            new MonsterSpawn(94, new(17, 13), IsBonus: true),
            new MonsterSpawn(94, new(12, 13)),
            new MonsterSpawn(80, new(21, 16), IsBonus: true),
            new MonsterSpawn(80, new(15, 16), IsBonus: true),
            new MonsterSpawn(80, new(9, 16), IsBonus: true),
            new MonsterSpawn(134, new(7, 15)),
            new MonsterSpawn(134, new(11, 15), IsBonus: true),
            new MonsterSpawn(134, new(15, 19)),
            new MonsterSpawn(134, new(19, 15), IsBonus: true),
            new MonsterSpawn(134, new(23, 15), IsBonus: true));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(200));
            r.Dialog(_room2, 6233);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6234);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6236);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room2, _room1);
            r.UnlockPortal(_room2, _room3);
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6235);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room3);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room5)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room4);
            r.UnlockPortal(_room5, _room6);
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
            r.Dialog(_room7, 6237);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room7, _room6);
            r.UnlockExit(_room7);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace46();
