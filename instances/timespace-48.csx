// The Catsy Princess
public class TimeSpace48 : InstanceScript
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
    private readonly int _room17;
    private readonly int _room18;
    private readonly int _room19;
    private readonly int _room20;
    private readonly int _room21;

    public TimeSpace48()
    {
        Definition = InstanceDefinitionBuilder.Named(48, "Fernon's tree is destroyed, but its roots still wind through Mount Krem. To be rid of it for good, we must get rid of its roots. Wait! What's this? It seems others came here before us, but are they friends or foes?", "The Catsy Princess")
            .ForLevels(57, 99)
            .StartingAt(16, 14)
            .WithLives(1)
            .Requiring(1012, 5)
            .WithSpecialReward(1011, 99)
            .WithReward(2003, 4)
            .WithReward(1087, 1)
            .WithRoom(4303, out _room1, 0, 7)
            .WithRoom(4306, out _room2, 0, 7)
            .WithRoom(4306, out _room3, 0, 8)
            .WithRoom(4307, out _room4, 0, 9)
            .WithRoom(4310, out _room5, 0, 10)
            .WithRoom(4306, out _room6, 1, 10)
            .WithRoom(4308, out _room7, 1, 9)
            .WithRoom(4309, out _room8, 1, 8)
            .WithRoom(4306, out _room9, 2, 8)
            .WithRoom(4307, out _room10, 2, 9)
            .WithRoom(4310, out _room11, 2, 10)
            .WithRoom(4306, out _room12, 3, 10)
            .WithRoom(4308, out _room13, 3, 9)
            .WithRoom(4309, out _room14, 3, 8)
            .WithRoom(4306, out _room15, 4, 8)
            .WithRoom(4307, out _room16, 4, 9)
            .WithRoom(4310, out _room17, 4, 10)
            .WithRoom(4306, out _room18, 5, 10)
            .WithRoom(4308, out _room19, 5, 9)
            .WithRoom(4309, out _room20, 5, 8)
            .WithRoom(4306, out _room21, 6, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(24, 36), _room2, new(24, 3), locked: false);
        r.SpawnPortal(_room2, new(24, 36), _room3, new(24, 3), locked: false);
        r.SpawnPortal(_room2, new(24, 3), _room1, new(24, 36), locked: false);
        r.SpawnPortal(_room3, new(24, 36), _room4, new(24, 3), locked: false);
        r.SpawnPortal(_room3, new(24, 3), _room2, new(24, 36), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(154, new(23, 25), IsBonus: true),
            new MonsterSpawn(154, new(24, 17)),
            new MonsterSpawn(154, new(29, 26), IsBonus: true));
        r.SpawnPortal(_room4, new(46, 18), _room5, new(2, 20), locked: false);
        r.SpawnPortal(_room4, new(24, 3), _room3, new(24, 36), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(154, new(19, 13), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(154, new(19, 24), IsBonus: true));
            }),
            new MonsterSpawn(154, new(25, 13)),
            new MonsterSpawn(154, new(23, 18), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(154, new(30, 13)));
            }),
            new MonsterSpawn(154, new(30, 26), IsBonus: true));
        r.SpawnPortal(_room5, new(2, 20), _room4, new(46, 18), locked: false);
        r.SpawnPortal(_room5, new(24, 3), _room6, new(24, 36), locked: false);
        r.Summon(_room5, new MonsterSpawn(154, new(26, 25), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(154, new(33, 15), IsBonus: true),
                new MonsterSpawn(154, new(24, 15), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(154, new(36, 22), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room6, new(24, 36), _room5, new(24, 3));
        r.SpawnPortal(_room6, new(24, 3), _room7, new(24, 36));
        r.Summon(_room6,
            new MonsterSpawn(235, new(15, 15), IsBonus: true),
            new MonsterSpawn(235, new(16, 14), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(244, new(19, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(244, new(24, 19), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room6,
                        new MonsterSpawn(235, new(36, 17), IsBonus: true),
                        new MonsterSpawn(235, new(24, 17), IsBonus: true),
                        new MonsterSpawn(95, new(24, 13), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room6, new MonsterSpawn(95, new(28, 13), IsBonus: true));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(235, new(23, 15)));
        r.SpawnPortal(_room7, new(24, 36), _room6, new(24, 3), locked: false);
        r.SpawnPortal(_room7, new(46, 18), _room8, new(2, 20), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(154, new(27, 26), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(154, new(25, 22)),
                new MonsterSpawn(154, new(25, 14), IsBonus: true),
                new MonsterSpawn(154, new(19, 26), IsBonus: true),
                new MonsterSpawn(154, new(16, 24), IsBonus: true));
            }),
            new MonsterSpawn(154, new(25, 24), IsBonus: true));
        r.SpawnPortal(_room8, new(2, 20), _room7, new(46, 18), locked: false);
        r.SpawnPortal(_room8, new(24, 36), _room9, new(24, 3), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(154, new(19, 23), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(154, new(36, 22)));
            }),
            new MonsterSpawn(154, new(24, 17)),
            new MonsterSpawn(154, new(24, 28)),
            new MonsterSpawn(154, new(36, 14), IsBonus: true));
        r.SpawnPortal(_room9, new(24, 36), _room10, new(24, 3));
        r.SpawnPortal(_room9, new(24, 3), _room8, new(24, 36));
        r.Summon(_room9,
            new MonsterSpawn(235, new(14, 15), IsBonus: true),
            new MonsterSpawn(235, new(18, 15), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(244, new(19, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(244, new(24, 19), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room9,
                        new MonsterSpawn(235, new(36, 17), IsBonus: true),
                        new MonsterSpawn(235, new(24, 17), OnDeath: r =>
                        {
                            r.Summon(_room9, new MonsterSpawn(95, new(28, 13)));
                        }),
                        new MonsterSpawn(95, new(24, 13)));
                    }));
                }));
            }),
            new MonsterSpawn(235, new(26, 14)));
        r.SpawnPortal(_room10, new(46, 18), _room11, new(2, 20), locked: false);
        r.SpawnPortal(_room10, new(24, 3), _room9, new(24, 36), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(154, new(18, 27), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(154, new(16, 14), IsBonus: true));
            }),
            new MonsterSpawn(154, new(23, 15)),
            new MonsterSpawn(154, new(29, 23), OnDeath: r =>
            {
                r.Summon(_room10,
                new MonsterSpawn(154, new(34, 14)),
                new MonsterSpawn(154, new(28, 14)));
            }),
            new MonsterSpawn(154, new(21, 21)));
        r.SpawnPortal(_room11, new(2, 20), _room10, new(46, 18), locked: false);
        r.SpawnPortal(_room11, new(24, 3), _room12, new(24, 36), locked: false);
        r.Summon(_room11,
            new MonsterSpawn(154, new(24, 17), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(154, new(24, 14), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(154, new(31, 25)));
                }));
            }),
            new MonsterSpawn(154, new(17, 26)),
            new MonsterSpawn(154, new(32, 24), IsBonus: true));
        r.SpawnPortal(_room12, new(24, 36), _room11, new(24, 3));
        r.SpawnPortal(_room12, new(24, 3), _room13, new(24, 36));
        r.Summon(_room12,
            new MonsterSpawn(235, new(14, 15), IsBonus: true),
            new MonsterSpawn(235, new(19, 15), OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(244, new(19, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(244, new(24, 19), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room12,
                        new MonsterSpawn(235, new(36, 17), IsBonus: true),
                        new MonsterSpawn(235, new(24, 17)),
                        new MonsterSpawn(95, new(24, 13), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room12, new MonsterSpawn(95, new(28, 13), IsBonus: true));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(235, new(26, 16), IsBonus: true));
        r.SpawnPortal(_room13, new(24, 36), _room12, new(24, 3), locked: false);
        r.SpawnPortal(_room13, new(46, 18), _room14, new(2, 20), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(154, new(20, 26), IsBonus: true),
            new MonsterSpawn(154, new(24, 31)),
            new MonsterSpawn(154, new(28, 22), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(154, new(32, 24)),
                new MonsterSpawn(154, new(24, 16), IsBonus: true),
                new MonsterSpawn(154, new(24, 13)));
            }));
        r.SpawnPortal(_room14, new(2, 20), _room13, new(46, 18), locked: false);
        r.SpawnPortal(_room14, new(24, 36), _room15, new(24, 3), locked: false);
        r.SpawnPortal(_room15, new(24, 36), _room16, new(24, 3));
        r.SpawnPortal(_room15, new(24, 3), _room14, new(24, 36));
        r.Summon(_room15,
            new MonsterSpawn(235, new(14, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room15, new MonsterSpawn(244, new(24, 19), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room15,
                    new MonsterSpawn(235, new(36, 17), IsBonus: true),
                    new MonsterSpawn(235, new(24, 17), OnDeath: r =>
                    {
                        r.Summon(_room15, new MonsterSpawn(95, new(28, 13)));
                    }),
                    new MonsterSpawn(95, new(24, 13)));
                }));
            }),
            new MonsterSpawn(235, new(18, 15), OnDeath: r =>
            {
                r.Summon(_room15, new MonsterSpawn(244, new(19, 14)));
            }),
            new MonsterSpawn(235, new(25, 15)));
        r.SpawnPortal(_room16, new(46, 18), _room17, new(2, 20), locked: false);
        r.SpawnPortal(_room16, new(24, 3), _room15, new(24, 36), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(154, new(27, 22), IsBonus: true),
            new MonsterSpawn(154, new(26, 15)),
            new MonsterSpawn(154, new(27, 20)));
        r.SpawnPortal(_room17, new(2, 20), _room16, new(46, 18), locked: false);
        r.SpawnPortal(_room17, new(24, 3), _room18, new(24, 36), locked: false);
        r.Summon(_room17,
            new MonsterSpawn(154, new(21, 12), IsBonus: true),
            new MonsterSpawn(154, new(19, 24), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room17,
                new MonsterSpawn(154, new(36, 18), IsBonus: true),
                new MonsterSpawn(154, new(12, 18), OnDeath: r =>
                {
                    r.Summon(_room17,
                    new MonsterSpawn(154, new(35, 22), IsBonus: true),
                    new MonsterSpawn(154, new(13, 21)));
                }));
            }));
        r.SpawnPortal(_room18, new(24, 36), _room17, new(24, 3));
        r.SpawnPortal(_room18, new(24, 3), _room19, new(24, 36));
        r.PlaceButton(_room18, 1, new(24, 18), 1000, 1045);
        r.SpawnPortal(_room19, new(24, 36), _room18, new(24, 3), locked: false);
        r.SpawnPortal(_room19, new(46, 18), _room20, new(2, 20), locked: false);
        r.Summon(_room19,
            new MonsterSpawn(154, new(35, 21), IsBonus: true),
            new MonsterSpawn(154, new(35, 25)),
            new MonsterSpawn(154, new(37, 21), IsBonus: true));
        r.SpawnPortal(_room20, new(2, 20), _room19, new(46, 18), locked: false);
        r.SpawnPortal(_room20, new(24, 36), _room21, new(24, 3), locked: false);
        r.Summon(_room20,
            new MonsterSpawn(154, new(6, 19)),
            new MonsterSpawn(154, new(16, 23)),
            new MonsterSpawn(154, new(13, 16)),
            new MonsterSpawn(154, new(30, 26)),
            new MonsterSpawn(154, new(32, 25), IsBonus: true));
        r.SpawnExit(_room21, new(24, 36), locked: false);
        r.SpawnPortal(_room21, new(24, 3), _room20, new(24, 36), locked: false);
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
            r.Dialog(_room2, 6238);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6240);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6241);
        }
        else if (room == _room15)
        {
            r.Dialog(_room15, 6242);
        }
        else if (room == _room18)
        {
            r.Dialog(_room18, 6244);
        }
        else if (room == _room21)
        {
            r.Dialog(_room21, 6246);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room6)
        {
            r.Dialog(_room6, 6239);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room5);
            r.UnlockPortal(_room6, _room7);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room10);
            r.UnlockPortal(_room9, _room8);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(160));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room11);
            r.UnlockPortal(_room12, _room13);
        }
        else if (room == _room15)
        {
            r.Dialog(_room15, 6243);
            r.AddClockTime(TimeSpan.FromSeconds(160));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room15, _room16);
            r.UnlockPortal(_room15, _room14);
        }
        else if (room == _room18)
        {
            r.Dialog(_room18, 6245);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room18, _room17);
            r.UnlockPortal(_room18, _room19);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room18 && button == 1)
        {
            r.Summon(_room18,
                        new MonsterSpawn(76, new(13, 18), IsBonus: true),
                        new MonsterSpawn(76, new(22, 25), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room18, new MonsterSpawn(232, new(16, 13), IsBonus: true));
                        }),
                        new MonsterSpawn(76, new(24, 22), OnDeath: r =>
                        {
                            r.Summon(_room18, new MonsterSpawn(244, new(32, 14)));
                        }),
                        new MonsterSpawn(76, new(25, 13), OnDeath: r =>
                        {
                            r.Summon(_room18, new MonsterSpawn(244, new(29, 14)));
                        }),
                        new MonsterSpawn(76, new(25, 19), OnDeath: r =>
                        {
                            r.Summon(_room18, new MonsterSpawn(232, new(28, 25), OnDeath: r =>
                            {
                                r.Summon(_room18, new MonsterSpawn(244, new(25, 17), OnDeath: r =>
                                {
                                    r.Summon(_room18, new MonsterSpawn(244, new(17, 24)));
                                }));
                            }));
                        }),
                        new MonsterSpawn(76, new(26, 22), OnDeath: r =>
                        {
                            r.Summon(_room18, new MonsterSpawn(232, new(22, 14)));
                        }),
                        new MonsterSpawn(76, new(34, 24)),
                        new MonsterSpawn(76, new(36, 17), IsBonus: true));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace48();
