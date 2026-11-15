// The Meeting
public class TimeSpace33 : InstanceScript
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

    public TimeSpace33()
    {
        Definition = InstanceDefinitionBuilder.Named(33, "At last, Korrkoaren has found Kenko! Korrkoaren wants to persuade him to fight Lord Cuarry once more.", "The Meeting")
            .ForLevels(43, 99)
            .StartingAt(7, 12)
            .WithLives(2)
            .Requiring(1012, 4)
            .WithDraw(138, 1)
            .WithDraw(145, 1)
            .WithDraw(152, 1)
            .WithSpecialReward(220, 1)
            .WithReward(1027, 2)
            .WithReward(1087, 1)
            .WithRoom(4418, out _room1, 0, 6)
            .WithRoom(4411, out _room2, 0, 6)
            .WithRoom(4411, out _room3, 1, 6)
            .WithRoom(4411, out _room4, 2, 6)
            .WithRoom(4400, out _room5, 3, 6)
            .WithRoom(4419, out _room6, 4, 6)
            .WithRoom(4400, out _room7, 4, 5)
            .WithRoom(4400, out _room8, 4, 7)
            .WithRoom(4419, out _room9, 5, 6)
            .WithRoom(4400, out _room10, 5, 5)
            .WithRoom(4400, out _room11, 5, 7)
            .WithRoom(4405, out _room12, 6, 6)
            .WithRoom(4410, out _room13, 7, 6)
            .WithRoom(4410, out _room14, 7, 7)
            .WithRoom(4406, out _room15, 7, 8)
            .WithRoom(4411, out _room16, 7, 9)
            .WithRoom(4411, out _room17, 6, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 11), locked: false);
        r.SpawnPortal(_room2, new(1, 11), _room1, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 11), _room3, new(1, 11), locked: false);
        r.SpawnPortal(_room3, new(1, 11), _room2, new(23, 11));
        r.SpawnPortal(_room3, new(23, 11), _room4, new(1, 11));
        r.Summon(_room3,
            new MonsterSpawn(148, new(8, 9), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(151, new(18, 14)),
                new MonsterSpawn(151, new(12, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(151, new(15, 9), IsBonus: true),
                    new MonsterSpawn(151, new(11, 7), OnDeath: r =>
                    {
                        r.Summon(_room3, new MonsterSpawn(152, new(16, 15), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(148, new(9, 12)),
            new MonsterSpawn(148, new(14, 11), IsBonus: true));
        r.SpawnPortal(_room4, new(1, 11), _room3, new(23, 11));
        r.SpawnPortal(_room4, new(23, 11), _room5, new(1, 12));
        r.Summon(_room4,
            new MonsterSpawn(148, new(8, 9)),
            new MonsterSpawn(148, new(9, 12), IsBonus: true),
            new MonsterSpawn(148, new(14, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(151, new(18, 14), IsBonus: true),
                new MonsterSpawn(151, new(12, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(151, new(15, 9), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(152, new(16, 15), IsBonus: true));
                    }),
                    new MonsterSpawn(151, new(11, 7), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room5, new(1, 12), _room4, new(23, 11));
        r.SpawnPortal(_room5, new(12, 1), _room6, new(12, 23));
        r.PlaceButton(_room5, 1, new(12, 12), 1000, 1045);
        r.SpawnPortal(_room6, new(12, 23), _room7, new(12, 1));
        r.Summon(_room6,
            new MonsterSpawn(148, new(14, 11)),
            new MonsterSpawn(148, new(8, 9), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(151, new(15, 9), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room6,
                    new MonsterSpawn(151, new(18, 14)),
                    new MonsterSpawn(149, new(18, 8), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(152, new(16, 15), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room6, new MonsterSpawn(152, new(16, 8), IsBonus: true, OnDeath: r =>
                            {
                                r.Summon(_room6, new MonsterSpawn(147, new(13, 7), IsBonus: true));
                            }));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(148, new(9, 12)));
        r.SpawnPortal(_room7, new(12, 23), _room7, new(12, 23), locked: false);
        r.SpawnPortal(_room7, new(12, 1), _room6, new(12, 23), locked: false);
        r.SpawnPortal(_room8, new(12, 23), _room7, new(12, 0), locked: false);
        r.SpawnPortal(_room8, new(23, 12), _room8, new(23, 12), locked: false);
        r.SpawnPortal(_room9, new(12, 23), _room10, new(12, 1));
        r.Summon(_room9,
            new MonsterSpawn(151, new(12, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(151, new(15, 9), IsBonus: true),
                new MonsterSpawn(151, new(11, 7), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(149, new(18, 8), IsBonus: true));
                }));
            }),
            new MonsterSpawn(148, new(14, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(151, new(18, 14), IsBonus: true));
            }),
            new MonsterSpawn(148, new(8, 9)),
            new MonsterSpawn(148, new(9, 12)));
        r.SpawnPortal(_room10, new(12, 23), _room10, new(12, 23), locked: false);
        r.SpawnPortal(_room10, new(12, 1), _room9, new(12, 23), locked: false);
        r.SpawnPortal(_room11, new(12, 23), _room10, new(12, 0), locked: false);
        r.SpawnPortal(_room11, new(23, 12), _room11, new(23, 12), locked: false);
        r.SpawnPortal(_room12, new(1, 12), _room11, new(23, 0), locked: false);
        r.SpawnPortal(_room12, new(12, 23), _room13, new(12, 1), locked: false);
        r.SummonNpcs(_room12, new NpcSpawn(253, new(8, 14)), new NpcSpawn(316, new(11, 13)));
        r.SpawnPortal(_room13, new(12, 23), _room14, new(12, 1));
        r.SpawnPortal(_room13, new(12, 1), _room12, new(12, 23));
        r.Summon(_room13,
            new MonsterSpawn(148, new(6, 7), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(148, new(14, 11), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room13,
                    new MonsterSpawn(151, new(18, 14), IsBonus: true),
                    new MonsterSpawn(151, new(12, 14), OnDeath: r =>
                    {
                        r.Summon(_room13,
                        new MonsterSpawn(151, new(15, 9), OnDeath: r =>
                        {
                            r.Summon(_room13, new MonsterSpawn(152, new(16, 15), IsBonus: true, OnDeath: r =>
                            {
                                r.Summon(_room13, new MonsterSpawn(153, new(16, 11), IsBonus: true));
                            }));
                        }),
                        new MonsterSpawn(151, new(11, 7), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room13, new MonsterSpawn(152, new(16, 8), OnDeath: r =>
                            {
                                r.Summon(_room13, new MonsterSpawn(149, new(18, 8), IsBonus: true));
                            }));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(148, new(7, 11)));
        r.SpawnPortal(_room14, new(12, 23), _room15, new(12, 1), locked: false);
        r.SpawnPortal(_room14, new(12, 1), _room13, new(12, 23), locked: false);
        r.SummonNpcs(_room14, new NpcSpawn(307, new(9, 4)), new NpcSpawn(329, new(10, 8)));
        r.SpawnPortal(_room15, new(1, 12), _room16, new(23, 11));
        r.SpawnPortal(_room15, new(12, 1), _room14, new(12, 23));
        r.Summon(_room15,
            new MonsterSpawn(148, new(8, 9)),
            new MonsterSpawn(148, new(14, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room15, new MonsterSpawn(152, new(16, 15), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room15,
                    new MonsterSpawn(151, new(18, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room15, new MonsterSpawn(147, new(13, 7), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room15, new MonsterSpawn(153, new(16, 11), IsBonus: true));
                        }));
                    }),
                    new MonsterSpawn(151, new(12, 14), OnDeath: r =>
                    {
                        r.Summon(_room15,
                        new MonsterSpawn(149, new(18, 8)),
                        new MonsterSpawn(151, new(15, 9)));
                    }));
                }));
            }));
        r.SpawnPortal(_room16, new(1, 11), _room17, new(23, 11));
        r.SpawnPortal(_room16, new(23, 11), _room15, new(1, 12));
        r.Summon(_room16,
            new MonsterSpawn(253, new(14, 5), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room16, new MonsterSpawn(253, new(11, 13)));
                r.Summon(_room16, new MonsterSpawn(253, new(13, 11)));
                r.Summon(_room16, new MonsterSpawn(253, new(11, 14), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room16, new MonsterSpawn(253, new(8, 17)));
                    r.Summon(_room16, new MonsterSpawn(253, new(16, 5), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room16, new MonsterSpawn(253, new(14, 15)));
                        r.Summon(_room16, new MonsterSpawn(253, new(16, 18), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room16, new MonsterSpawn(253, new(12, 18)));
                            r.Summon(_room16, new MonsterSpawn(253, new(20, 17)));
                            r.Summon(_room16, new MonsterSpawn(253, new(13, 12)));
                        }));
                        r.Summon(_room16, new MonsterSpawn(253, new(13, 13)));
                    }));
                    r.Summon(_room16, new MonsterSpawn(253, new(11, 5)));
                }));
            }),
            new MonsterSpawn(316, new(20, 10), IsBonus: true));
        r.SummonNpcs(_room16, new NpcSpawn(307, new(20, 12)), new NpcSpawn(315, new(20, 14)));
        r.SpawnExit(_room17, new(1, 11), locked: false);
        r.SpawnPortal(_room17, new(23, 11), _room16, new(1, 11), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(1200));
            r.Dialog(_room2, 6208);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6209);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6210);
        }
        else if (room == _room16)
        {
            r.Dialog(_room16, 6211);
            r.StartRoomClock(_room16, TimeSpan.FromSeconds(180));
        }
        else if (room == _room17)
        {
            r.Dialog(_room17, 6213);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room6)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room7);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room13)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room13, _room14);
            r.UnlockPortal(_room13, _room12);
        }
        else if (room == _room15)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room15, _room16);
            r.UnlockPortal(_room15, _room14);
        }
        else if (room == _room16)
        {
            r.StopRoomClock(_room16);
            r.Dialog(_room16, 6212);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room16, _room17);
            r.UnlockPortal(_room16, _room15);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room5 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace33();
