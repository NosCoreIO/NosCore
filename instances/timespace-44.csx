// The Heart of Fernon
public class TimeSpace44 : InstanceScript
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
    private readonly int _room16;

    public TimeSpace44()
    {
        Definition = InstanceDefinitionBuilder.Named(44, "With the dryads in the time-space destroyed, Fernon's tree can no longer grow. It has lost all its power. Your task now is to destroy the tree before it finds another way to regain its energy. We trust you completely!", "The Heart of Fernon")
            .ForLevels(54, 99)
            .StartingAt(14, 19)
            .WithLives(1)
            .Requiring(1012, 5)
            .WithSpecialReward(1033, 8)
            .WithReward(2003, 2)
            .WithReward(1087, 1)
            .WithRoom(4302, out _room1, 1, 6)
            .WithRoom(4305, out _room2, 1, 6)
            .WithRoom(4305, out _room3, 2, 6)
            .WithRoom(4305, out _room4, 3, 6)
            .WithRoom(4309, out _room5, 4, 6)
            .WithRoom(4306, out _room6, 5, 6)
            .WithRoom(4306, out _room7, 5, 7)
            .WithRoom(4310, out _room8, 5, 8)
            .WithRoom(4305, out _room9, 5, 9)
            .WithRoom(4305, out _room10, 4, 9)
            .WithRoom(4307, out _room11, 3, 9)
            .WithRoom(4306, out _room12, 2, 9)
            .WithRoom(4308, out _room13, 2, 8)
            .WithRoom(4305, out _room14, 2, 7)
            .WithRoom(4309, out _room15, 3, 7)
            .WithRoom(4310, out _room16, 4, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(46, 18), _room2, new(2, 20), locked: false);
        r.SpawnPortal(_room2, new(2, 20), _room1, new(46, 18), locked: false);
        r.SpawnPortal(_room2, new(46, 18), _room3, new(2, 20), locked: false);
        r.Summon(_room2,
            new MonsterSpawn(154, new(13, 19)),
            new MonsterSpawn(154, new(16, 24)),
            new MonsterSpawn(154, new(18, 13)),
            new MonsterSpawn(154, new(22, 14), IsBonus: true),
            new MonsterSpawn(154, new(24, 23), IsBonus: true),
            new MonsterSpawn(154, new(26, 15)),
            new MonsterSpawn(154, new(26, 19), IsBonus: true),
            new MonsterSpawn(154, new(28, 24)),
            new MonsterSpawn(154, new(30, 14)),
            new MonsterSpawn(154, new(35, 18), IsBonus: true));
        r.SpawnPortal(_room3, new(2, 20), _room2, new(46, 18), locked: false);
        r.SpawnPortal(_room3, new(46, 18), _room4, new(2, 20), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(154, new(13, 21), IsBonus: true),
            new MonsterSpawn(154, new(13, 14)),
            new MonsterSpawn(154, new(15, 24)),
            new MonsterSpawn(154, new(17, 25)),
            new MonsterSpawn(154, new(25, 18)),
            new MonsterSpawn(154, new(23, 13)),
            new MonsterSpawn(154, new(27, 26)),
            new MonsterSpawn(154, new(30, 15)),
            new MonsterSpawn(154, new(31, 26), IsBonus: true),
            new MonsterSpawn(154, new(39, 19), IsBonus: true));
        r.SpawnPortal(_room4, new(2, 20), _room3, new(46, 18));
        r.SpawnPortal(_room4, new(46, 18), _room5, new(2, 20));
        r.Summon(_room4,
            new MonsterSpawn(154, new(25, 23)),
            new MonsterSpawn(154, new(26, 14)),
            new MonsterSpawn(154, new(29, 26)),
            new MonsterSpawn(154, new(33, 13)),
            new MonsterSpawn(154, new(37, 21), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(71, new(22, 15), OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(92, new(19, 24), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4,
                        new MonsterSpawn(80, new(13, 13), IsBonus: true),
                        new MonsterSpawn(92, new(12, 17), IsBonus: true));
                    }),
                    new MonsterSpawn(71, new(16, 14)));
                }),
                new MonsterSpawn(80, new(20, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(80, new(15, 15), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(71, new(24, 14), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room5, new(2, 20), _room4, new(46, 18), locked: false);
        r.SpawnPortal(_room5, new(24, 36), _room6, new(24, 3), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(154, new(14, 16)),
            new MonsterSpawn(154, new(17, 27)),
            new MonsterSpawn(154, new(20, 14), IsBonus: true),
            new MonsterSpawn(154, new(23, 24)),
            new MonsterSpawn(154, new(30, 13), IsBonus: true),
            new MonsterSpawn(154, new(25, 22), IsBonus: true),
            new MonsterSpawn(154, new(36, 20), IsBonus: true));
        r.SpawnPortal(_room6, new(24, 36), _room7, new(24, 3), locked: false);
        r.SpawnPortal(_room6, new(24, 3), _room5, new(24, 36), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(154, new(19, 13)),
            new MonsterSpawn(154, new(21, 22), IsBonus: true),
            new MonsterSpawn(154, new(22, 15)),
            new MonsterSpawn(154, new(21, 21)),
            new MonsterSpawn(154, new(27, 16)),
            new MonsterSpawn(154, new(31, 23)));
        r.SpawnPortal(_room7, new(24, 36), _room8, new(24, 3));
        r.SpawnPortal(_room7, new(24, 3), _room6, new(24, 36));
        r.Summon(_room7,
            new MonsterSpawn(89, new(24, 19), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(154, new(16, 24), IsBonus: true));
            }),
            new MonsterSpawn(154, new(29, 24)),
            new MonsterSpawn(154, new(33, 23), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(154, new(17, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room7,
                    new MonsterSpawn(71, new(24, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(71, new(16, 14)));
                    }),
                    new MonsterSpawn(80, new(24, 13), OnDeath: r =>
                    {
                        r.Summon(_room7,
                        new MonsterSpawn(80, new(15, 15), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room7,
                            new MonsterSpawn(80, new(20, 13), OnDeath: r =>
                            {
                                r.Summon(_room7, new MonsterSpawn(71, new(22, 15), IsBonus: true));
                            }),
                            new MonsterSpawn(92, new(19, 24), IsBonus: true));
                        }),
                        new MonsterSpawn(92, new(12, 17), OnDeath: r =>
                        {
                            r.Summon(_room7, new MonsterSpawn(89, new(21, 23)));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(154, new(36, 21), IsBonus: true));
        r.SpawnPortal(_room8, new(2, 20), _room9, new(46, 18), locked: false);
        r.SpawnPortal(_room8, new(24, 3), _room7, new(24, 36), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(154, new(14, 22), IsBonus: true),
            new MonsterSpawn(154, new(23, 18), IsBonus: true),
            new MonsterSpawn(154, new(14, 14)),
            new MonsterSpawn(154, new(27, 20)),
            new MonsterSpawn(154, new(18, 14)),
            new MonsterSpawn(154, new(29, 24), IsBonus: true),
            new MonsterSpawn(154, new(27, 12)),
            new MonsterSpawn(154, new(34, 21)));
        r.SpawnPortal(_room9, new(2, 20), _room10, new(46, 18), locked: false);
        r.SpawnPortal(_room9, new(46, 18), _room8, new(2, 20), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(154, new(12, 14), IsBonus: true),
            new MonsterSpawn(154, new(13, 20)),
            new MonsterSpawn(154, new(31, 15), IsBonus: true),
            new MonsterSpawn(154, new(23, 13), IsBonus: true),
            new MonsterSpawn(154, new(27, 23)),
            new MonsterSpawn(154, new(34, 26)),
            new MonsterSpawn(154, new(35, 15)),
            new MonsterSpawn(154, new(36, 24)));
        r.SpawnPortal(_room10, new(2, 20), _room11, new(46, 18));
        r.SpawnPortal(_room10, new(46, 18), _room9, new(2, 20));
        r.Summon(_room10,
            new MonsterSpawn(89, new(21, 24), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(154, new(16, 24)));
            }),
            new MonsterSpawn(71, new(23, 17), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(154, new(17, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room11, new(46, 18), _room10, new(2, 20), locked: false);
        r.SpawnPortal(_room11, new(24, 3), _room12, new(24, 36), locked: false);
        r.Summon(_room11,
            new MonsterSpawn(154, new(11, 20), IsBonus: true),
            new MonsterSpawn(154, new(30, 23)),
            new MonsterSpawn(154, new(22, 19), IsBonus: true),
            new MonsterSpawn(154, new(34, 16)),
            new MonsterSpawn(154, new(24, 16)),
            new MonsterSpawn(154, new(33, 26)),
            new MonsterSpawn(154, new(33, 16)),
            new MonsterSpawn(154, new(34, 19)));
        r.SpawnPortal(_room12, new(24, 36), _room11, new(24, 3));
        r.SpawnPortal(_room12, new(24, 3), _room13, new(24, 36));
        r.Summon(_room12,
            new MonsterSpawn(154, new(25, 9), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room12, new MonsterSpawn(154, new(12, 16)));
                r.Summon(_room12, new MonsterSpawn(154, new(25, 9), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room12, new MonsterSpawn(154, new(16, 24)));
                    r.Summon(_room12, new MonsterSpawn(154, new(24, 25)));
                    r.Summon(_room12, new MonsterSpawn(154, new(23, 17)));
                    r.Summon(_room12, new MonsterSpawn(154, new(38, 19), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room12, new MonsterSpawn(154, new(23, 10)));
                        r.Summon(_room12, new MonsterSpawn(154, new(23, 27)));
                        r.Summon(_room12, new MonsterSpawn(154, new(21, 15)));
                        r.Summon(_room12, new MonsterSpawn(154, new(22, 14)));
                        r.Summon(_room12, new MonsterSpawn(154, new(34, 17)));
                    }));
                    r.Summon(_room12, new MonsterSpawn(154, new(12, 18)));
                }));
                r.Summon(_room12, new MonsterSpawn(154, new(27, 17)));
                r.Summon(_room12, new MonsterSpawn(154, new(13, 22)));
                r.Summon(_room12, new MonsterSpawn(154, new(22, 18)));
            }),
            new MonsterSpawn(154, new(13, 22)),
            new MonsterSpawn(154, new(33, 24)),
            new MonsterSpawn(154, new(17, 15)),
            new MonsterSpawn(154, new(25, 18)));
        r.SpawnPortal(_room13, new(24, 36), _room12, new(24, 3), locked: false);
        r.SpawnPortal(_room13, new(46, 18), _room14, new(2, 20), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(154, new(13, 13), IsBonus: true),
            new MonsterSpawn(154, new(17, 12), IsBonus: true),
            new MonsterSpawn(154, new(14, 23)),
            new MonsterSpawn(154, new(26, 16)),
            new MonsterSpawn(154, new(24, 13)),
            new MonsterSpawn(154, new(26, 19)),
            new MonsterSpawn(154, new(37, 15), IsBonus: true));
        r.SpawnPortal(_room14, new(2, 20), _room13, new(46, 18));
        r.SpawnPortal(_room14, new(46, 18), _room15, new(2, 20));
        r.Summon(_room14,
            new MonsterSpawn(154, new(16, 24)),
            new MonsterSpawn(154, new(14, 20)),
            new MonsterSpawn(154, new(6, 20)),
            new MonsterSpawn(154, new(8, 19)),
            new MonsterSpawn(154, new(31, 25)),
            new MonsterSpawn(154, new(17, 26), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room14, new MonsterSpawn(154, new(12, 24), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room14, new MonsterSpawn(154, new(18, 26)));
                    r.Summon(_room14, new MonsterSpawn(154, new(12, 21)));
                    r.Summon(_room14, new MonsterSpawn(154, new(29, 13)));
                    r.Summon(_room14, new MonsterSpawn(154, new(34, 19)));
                    r.Summon(_room14, new MonsterSpawn(154, new(37, 15)));
                    r.Summon(_room14, new MonsterSpawn(154, new(25, 15)));
                }));
                r.Summon(_room14, new MonsterSpawn(154, new(34, 21)));
                r.Summon(_room14, new MonsterSpawn(154, new(27, 14)));
                r.Summon(_room14, new MonsterSpawn(154, new(29, 13), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room14, new MonsterSpawn(154, new(44, 18), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room14, new MonsterSpawn(154, new(33, 23)));
                        r.Summon(_room14, new MonsterSpawn(154, new(37, 19)));
                        r.Summon(_room14, new MonsterSpawn(154, new(41, 18)));
                        r.Summon(_room14, new MonsterSpawn(154, new(25, 19)));
                        r.Summon(_room14, new MonsterSpawn(154, new(20, 14)));
                        r.Summon(_room14, new MonsterSpawn(154, new(36, 24)));
                    }));
                    r.Summon(_room14, new MonsterSpawn(154, new(37, 22)));
                    r.Summon(_room14, new MonsterSpawn(154, new(36, 21)));
                    r.Summon(_room14, new MonsterSpawn(154, new(12, 14)));
                    r.Summon(_room14, new MonsterSpawn(154, new(18, 24)));
                    r.Summon(_room14, new MonsterSpawn(154, new(21, 25), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room14, new MonsterSpawn(154, new(30, 14)));
                        r.Summon(_room14, new MonsterSpawn(154, new(11, 17)));
                        r.Summon(_room14, new MonsterSpawn(154, new(28, 12)));
                        r.Summon(_room14, new MonsterSpawn(154, new(10, 20)));
                        r.Summon(_room14, new MonsterSpawn(154, new(36, 20)));
                        r.Summon(_room14, new MonsterSpawn(154, new(35, 16)));
                    }));
                }));
                r.Summon(_room14, new MonsterSpawn(154, new(38, 18)));
                r.Summon(_room14, new MonsterSpawn(154, new(31, 23)));
            }));
        r.SpawnPortal(_room15, new(2, 20), _room14, new(46, 18));
        r.SpawnPortal(_room15, new(24, 36), _room16, new(24, 3));
        r.Summon(_room15,
            new MonsterSpawn(154, new(14, 15)),
            new MonsterSpawn(154, new(14, 21)),
            new MonsterSpawn(154, new(18, 25), IsBonus: true),
            new MonsterSpawn(154, new(22, 14)),
            new MonsterSpawn(155, new(24, 16)),
            new MonsterSpawn(155, new(24, 21)),
            new MonsterSpawn(154, new(27, 24)),
            new MonsterSpawn(154, new(32, 14)),
            new MonsterSpawn(154, new(33, 24), IsBonus: true),
            new MonsterSpawn(154, new(36, 19), IsBonus: true));
        r.SpawnExit(_room16, new(2, 20));
        r.SpawnPortal(_room16, new(24, 3), _room15, new(24, 36));
        r.Summon(_room16,
            new MonsterSpawn(154, new(12, 21), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room16, new MonsterSpawn(155, new(7, 19)));
                r.Summon(_room16, new MonsterSpawn(155, new(36, 14)));
                r.Summon(_room16, new MonsterSpawn(155, new(23, 8)));
                r.Summon(_room16, new MonsterSpawn(155, new(24, 25)));
            }),
            new MonsterSpawn(154, new(13, 16)),
            new MonsterSpawn(154, new(15, 14)),
            new MonsterSpawn(154, new(18, 25)),
            new MonsterSpawn(154, new(21, 14), IsBonus: true),
            new MonsterSpawn(154, new(23, 24)),
            new MonsterSpawn(154, new(24, 18)),
            new MonsterSpawn(154, new(24, 21)),
            new MonsterSpawn(154, new(25, 15)),
            new MonsterSpawn(154, new(27, 24)),
            new MonsterSpawn(154, new(29, 13)),
            new MonsterSpawn(154, new(33, 24)),
            new MonsterSpawn(154, new(35, 17)),
            new MonsterSpawn(154, new(36, 20)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(240));
            r.Dialog(_room2, 6171);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room12, TimeSpan.FromSeconds(120));
        }
        else if (room == _room14)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room14, TimeSpan.FromSeconds(180));
        }
        else if (room == _room15)
        {
            r.Dialog(_room15, 6172);
        }
        else if (room == _room16)
        {
            r.Dialog(_room16, 6173);
            r.StartRoomClock(_room16, TimeSpan.FromSeconds(180));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room3);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room7, _room8);
            r.UnlockPortal(_room7, _room6);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room11);
            r.UnlockPortal(_room10, _room9);
        }
        else if (room == _room12)
        {
            r.StopRoomClock(_room12);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room11);
            r.UnlockPortal(_room12, _room13);
        }
        else if (room == _room14)
        {
            r.StopRoomClock(_room14);
            r.AddClockTime(TimeSpan.FromSeconds(300));
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room14, _room13);
            r.UnlockPortal(_room14, _room15);
        }
        else if (room == _room15)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room15, _room14);
            r.UnlockPortal(_room15, _room16);
        }
        else if (room == _room16)
        {
            r.StopRoomClock(_room16);
            r.Dialog(_room16, 6174);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockExit(_room16);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace44();
