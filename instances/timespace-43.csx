// Fernon's Energy: Part 4
public class TimeSpace43 : InstanceScript
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

    public TimeSpace43()
    {
        Definition = InstanceDefinitionBuilder.Named(43, "We're nearly there! Seal the last time-space stone feeding Fernon's tree with energy and it will stop growing. Set off as quickly as you can!", "Fernon's Energy: Part 4")
            .ForLevels(53, 99)
            .StartingAt(21, 15)
            .Requiring(1012, 5)
            .WithDraw(530, 1)
            .WithDraw(532, 1)
            .WithSpecialReward(218, 1)
            .WithReward(2003, 2)
            .WithReward(1087, 1)
            .WithRoom(4303, out _room1, 5, 0)
            .WithRoom(4306, out _room2, 5, 0)
            .WithRoom(4310, out _room3, 5, 1)
            .WithRoom(4308, out _room4, 5, 2)
            .WithRoom(4312, out _room5, 4, 2)
            .WithRoom(4305, out _room6, 4, 3)
            .WithRoom(4309, out _room7, 5, 3)
            .WithRoom(4307, out _room8, 6, 3)
            .WithRoom(4309, out _room9, 6, 4)
            .WithRoom(4306, out _room10, 7, 4)
            .WithRoom(4306, out _room11, 7, 5)
            .WithRoom(4306, out _room12, 7, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(24, 36), _room2, new(24, 3), locked: false);
        r.SpawnPortal(_room2, new(24, 36), _room3, new(24, 3), locked: false);
        r.SpawnPortal(_room2, new(24, 3), _room1, new(24, 36), locked: false);
        r.SpawnPortal(_room3, new(2, 20), _room4, new(46, 18));
        r.SpawnPortal(_room3, new(24, 3), _room2, new(24, 36));
        r.Summon(_room3,
            new MonsterSpawn(71, new(23, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(88, new(13, 19), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(134, new(16, 13), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room3,
                        new MonsterSpawn(134, new(20, 14), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room3, new MonsterSpawn(88, new(23, 17)));
                        }),
                        new MonsterSpawn(92, new(17, 14), OnDeath: r =>
                        {
                            r.Summon(_room3, new MonsterSpawn(71, new(17, 24), IsBonus: true));
                        }));
                    }));
                }),
                new MonsterSpawn(134, new(11, 17), IsBonus: true));
            }),
            new MonsterSpawn(134, new(24, 17)));
        r.SpawnPortal(_room4, new(24, 36), _room5, new(24, 3), locked: false);
        r.SpawnPortal(_room4, new(46, 18), _room3, new(2, 20), locked: false);
        r.SpawnPortal(_room5, new(46, 18), _room6, new(2, 20));
        r.SpawnPortal(_room5, new(24, 3), _room4, new(24, 36));
        r.Summon(_room5,
            new MonsterSpawn(134, new(24, 10), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(134, new(16, 13)));
            }),
            new MonsterSpawn(71, new(24, 14)),
            new MonsterSpawn(134, new(24, 17), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(88, new(13, 17), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(88, new(22, 17), OnDeath: r =>
                    {
                        r.Summon(_room5,
                        new MonsterSpawn(71, new(17, 24), IsBonus: true),
                        new MonsterSpawn(92, new(15, 24), IsBonus: true));
                    }),
                    new MonsterSpawn(134, new(20, 14), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room6, new(2, 20), _room5, new(46, 18));
        r.SpawnPortal(_room6, new(46, 18), _room7, new(2, 20));
        r.Summon(_room6, new MonsterSpawn(134, new(24, 17), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(71, new(24, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room6,
                    new MonsterSpawn(134, new(20, 24), IsBonus: true),
                    new MonsterSpawn(88, new(20, 14), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(92, new(17, 12), IsBonus: true));
                    }),
                    new MonsterSpawn(134, new(16, 13), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(71, new(17, 24)));
                    }));
                }),
                new MonsterSpawn(88, new(13, 17)),
                new MonsterSpawn(134, new(11, 19), IsBonus: true));
            }));
        r.SpawnPortal(_room7, new(2, 20), _room6, new(46, 18), locked: false);
        r.SpawnPortal(_room7, new(24, 36), _room8, new(24, 3), locked: false);
        r.SpawnPortal(_room8, new(46, 18), _room9, new(2, 20));
        r.SpawnPortal(_room8, new(24, 3), _room7, new(24, 36));
        r.Summon(_room8,
            new MonsterSpawn(71, new(19, 23), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(134, new(24, 10)),
                new MonsterSpawn(134, new(16, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(71, new(24, 14), IsBonus: true));
                }),
                new MonsterSpawn(88, new(13, 17), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(134, new(24, 17), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room8,
                        new MonsterSpawn(88, new(20, 24), IsBonus: true),
                        new MonsterSpawn(134, new(20, 14), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(92, new(22, 16)));
        r.SpawnPortal(_room9, new(2, 20), _room8, new(46, 18));
        r.SpawnPortal(_room9, new(24, 36), _room10, new(24, 3));
        r.Summon(_room9,
            new MonsterSpawn(134, new(20, 14)),
            new MonsterSpawn(88, new(22, 17), IsBonus: true),
            new MonsterSpawn(134, new(23, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(71, new(17, 23), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(134, new(24, 24), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room9, new MonsterSpawn(92, new(17, 24), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(134, new(16, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(71, new(24, 14), IsBonus: true));
                }),
                new MonsterSpawn(88, new(13, 17), IsBonus: true));
            }));
        r.SpawnPortal(_room10, new(24, 36), _room11, new(24, 3));
        r.SpawnPortal(_room10, new(24, 3), _room9, new(24, 36));
        r.Summon(_room10, new MonsterSpawn(134, new(24, 17), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(71, new(24, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(134, new(24, 10), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room10,
                        new MonsterSpawn(71, new(17, 24), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room10,
                            new MonsterSpawn(88, new(20, 24), IsBonus: true),
                            new MonsterSpawn(134, new(20, 14), IsBonus: true, OnDeath: r =>
                            {
                                r.Summon(_room10, new MonsterSpawn(92, new(22, 16), IsBonus: true));
                            }));
                        }),
                        new MonsterSpawn(134, new(16, 13), IsBonus: true),
                        new MonsterSpawn(88, new(13, 17), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnExit(_room11, new(24, 36), locked: false);
        r.SpawnPortal(_room11, new(24, 3), _room12, new(24, 36), locked: false);
        r.SpawnPortal(_room12, new(24, 36), _room11, new(24, 3), locked: false);
        r.SpawnPortal(_room12, new(24, 3), _room12, new(24, 3), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(160));
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room2, 6169);
        }
        else if (room == _room5)
        {
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(100));
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room11, 6166);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room2);
        }
        else if (room == _room5)
        {
            r.StopRoomClock(_room5);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room6)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room5);
            r.UnlockPortal(_room6, _room7);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
            r.UnlockPortal(_room8, _room7);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room8);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room11);
            r.UnlockPortal(_room10, _room9);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace43();
