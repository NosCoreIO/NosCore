// The Counterattack
public class TimeSpace56 : InstanceScript
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

    public TimeSpace56()
    {
        Definition = InstanceDefinitionBuilder.Named(56, "The destruction of Fernon's tree drove the Kenkos to retreat. But now they are attacking Mount Krem. The Koarens are in great danger and, to top it all, the Whitethorn witches and Cenil have joined the fight!", "The Counterattack")
            .ForLevels(61, 99)
            .StartingAt(9, 11)
            .WithLives(2)
            .Requiring(1012, 6)
            .WithSpecialReward(228, 1)
            .WithReward(1031, 1)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 3)
            .WithRoom(4805, out _room2, 0, 3)
            .WithRoom(4805, out _room3, 1, 3)
            .WithRoom(2100, out _room4, 2, 3)
            .WithRoom(4811, out _room5, 3, 3)
            .WithRoom(4805, out _room6, 3, 4)
            .WithRoom(2100, out _room7, 4, 4)
            .WithRoom(4810, out _room8, 5, 4)
            .WithRoom(4805, out _room9, 5, 5)
            .WithRoom(2100, out _room10, 4, 5)
            .WithRoom(4807, out _room11, 3, 5)
            .WithRoom(2100, out _room12, 3, 6)
            .WithRoom(4806, out _room13, 3, 7)
            .WithRoom(4809, out _room14, 4, 7)
            .WithRoom(4807, out _room15, 5, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 14), _room3, new(1, 14), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(117, new(12, 17)));
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 14), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(9, 36), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(236, new(15, 7), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(245, new(13, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(245, new(12, 19)));
                }));
            }),
            new MonsterSpawn(236, new(18, 18), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(236, new(20, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room4, new(9, 36), _room3, new(28, 14));
        r.SpawnPortal(_room4, new(32, 59), _room5, new(15, 0));
        r.Summon(_room4,
            new MonsterSpawn(315, new(39, 34), IsBonus: true),
            new MonsterSpawn(326, new(43, 34), IsBonus: true, OnDeath: r =>
            {
                r.Dialog(_room4, 6283);
                r.AddClockTime(TimeSpan.FromSeconds(100));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room4, _room3);
                r.UnlockPortal(_room4, _room5);
            }),
            new MonsterSpawn(122, new(35, 34)),
            new MonsterSpawn(122, new(35, 40), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(122, new(37, 16), OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(122, new(35, 53), IsBonus: true));
                }),
                new MonsterSpawn(122, new(33, 21), OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(122, new(29, 28), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(325, new(45, 44)));
                    }),
                    new MonsterSpawn(122, new(28, 40), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(122, new(30, 46)));
                    }));
                }));
            }),
            new MonsterSpawn(122, new(37, 27), IsBonus: true),
            new MonsterSpawn(122, new(37, 43), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(325, new(46, 39)));
            }),
            new MonsterSpawn(325, new(44, 27), IsBonus: true),
            new MonsterSpawn(325, new(45, 33)));
        r.SpawnPortal(_room5, new(29, 14), _room6, new(1, 14), locked: false);
        r.SpawnPortal(_room5, new(15, 0), _room4, new(32, 59), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(236, new(18, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(245, new(13, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(245, new(12, 19), IsBonus: true));
                }));
            }),
            new MonsterSpawn(236, new(17, 19), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(236, new(20, 14), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room6, new(1, 14), _room5, new(29, 14), locked: false);
        r.SpawnPortal(_room6, new(28, 14), _room7, new(9, 36), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(236, new(12, 6), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(236, new(20, 14), IsBonus: true));
            }),
            new MonsterSpawn(236, new(15, 16), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(245, new(13, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(245, new(12, 19), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room7, new(9, 36), _room6, new(28, 14));
        r.SpawnPortal(_room7, new(32, 59), _room8, new(15, 0));
        r.Summon(_room7,
            new MonsterSpawn(153, new(10, 39)),
            new MonsterSpawn(153, new(37, 51)),
            new MonsterSpawn(150, new(24, 45)),
            new MonsterSpawn(150, new(46, 58), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room7, new MonsterSpawn(153, new(10, 25), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room7, new MonsterSpawn(153, new(51, 41)));
                    r.Summon(_room7, new MonsterSpawn(153, new(26, 52)));
                    r.Summon(_room7, new MonsterSpawn(150, new(17, 35)));
                    r.Summon(_room7, new MonsterSpawn(150, new(25, 21)));
                    r.Summon(_room7, new MonsterSpawn(147, new(10, 20), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room7, new MonsterSpawn(153, new(44, 16)));
                        r.Summon(_room7, new MonsterSpawn(153, new(12, 49)));
                        r.Summon(_room7, new MonsterSpawn(150, new(51, 56)));
                        r.Summon(_room7, new MonsterSpawn(150, new(48, 54)));
                        r.Summon(_room7, new MonsterSpawn(147, new(33, 39)));
                        r.Summon(_room7, new MonsterSpawn(147, new(41, 27)));
                        r.Summon(_room7, new MonsterSpawn(147, new(54, 18), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room7, new MonsterSpawn(153, new(35, 55), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room7, new MonsterSpawn(153, new(55, 55)));
                                r.Summon(_room7, new MonsterSpawn(153, new(24, 21)));
                                r.Summon(_room7, new MonsterSpawn(150, new(27, 12)));
                                r.Summon(_room7, new MonsterSpawn(150, new(39, 50)));
                                r.Summon(_room7, new MonsterSpawn(147, new(27, 32)));
                                r.Summon(_room7, new MonsterSpawn(147, new(33, 15)));
                                r.Summon(_room7, new MonsterSpawn(147, new(48, 21)));
                            }));
                            r.Summon(_room7, new MonsterSpawn(153, new(10, 58)));
                            r.Summon(_room7, new MonsterSpawn(150, new(17, 58)));
                            r.Summon(_room7, new MonsterSpawn(150, new(44, 28)));
                            r.Summon(_room7, new MonsterSpawn(147, new(33, 27)));
                            r.Summon(_room7, new MonsterSpawn(147, new(11, 24)));
                            r.Summon(_room7, new MonsterSpawn(147, new(47, 14)));
                        }));
                    }));
                    r.Summon(_room7, new MonsterSpawn(147, new(35, 33)));
                    r.Summon(_room7, new MonsterSpawn(147, new(20, 46)));
                }));
                r.Summon(_room7, new MonsterSpawn(153, new(57, 29)));
                r.Summon(_room7, new MonsterSpawn(150, new(43, 18)));
                r.Summon(_room7, new MonsterSpawn(150, new(18, 41)));
                r.Summon(_room7, new MonsterSpawn(147, new(21, 35)));
                r.Summon(_room7, new MonsterSpawn(147, new(12, 42)));
                r.Summon(_room7, new MonsterSpawn(147, new(57, 21)));
            }),
            new MonsterSpawn(147, new(47, 39)),
            new MonsterSpawn(147, new(19, 41)),
            new MonsterSpawn(147, new(25, 49)));
        r.SpawnPortal(_room8, new(1, 14), _room9, new(28, 14), locked: false);
        r.SpawnPortal(_room8, new(15, 0), _room7, new(32, 59), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(236, new(16, 3), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(245, new(13, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(245, new(12, 19), IsBonus: true));
                }));
            }),
            new MonsterSpawn(236, new(23, 19), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(236, new(20, 14), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room9, new(1, 14), _room10, new(58, 37));
        r.SpawnPortal(_room9, new(28, 14), _room8, new(1, 14));
        r.Summon(_room9,
            new MonsterSpawn(316, new(12, 13), IsBonus: true, OnDeath: r =>
            {
                r.Dialog(_room9, 6286);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room9, _room10);
                r.UnlockPortal(_room9, _room8);
            }),
            new MonsterSpawn(147, new(9, 13)),
            new MonsterSpawn(147, new(10, 17), IsBonus: true),
            new MonsterSpawn(147, new(11, 10)));
        r.SpawnPortal(_room10, new(32, 59), _room11, new(15, 0));
        r.SpawnPortal(_room10, new(58, 37), _room9, new(1, 14));
        r.Summon(_room10,
            new MonsterSpawn(153, new(21, 31), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(153, new(30, 22)));
            }),
            new MonsterSpawn(153, new(26, 49), IsBonus: true),
            new MonsterSpawn(153, new(28, 43), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(153, new(28, 29), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(153, new(20, 40)));
                }));
            }),
            new MonsterSpawn(150, new(33, 27), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(150, new(33, 36), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(150, new(33, 19), IsBonus: true));
                }));
            }),
            new MonsterSpawn(150, new(33, 46)),
            new MonsterSpawn(150, new(33, 52)));
        r.SpawnPortal(_room11, new(15, 28), _room12, new(34, 11), locked: false);
        r.SpawnPortal(_room11, new(15, 0), _room10, new(32, 59), locked: false);
        r.SummonNpcs(_room11, new NpcSpawn(117, new(12, 14)));
        r.SpawnPortal(_room12, new(58, 37), _room13, new(1, 14));
        r.SpawnPortal(_room12, new(34, 11), _room11, new(15, 28));
        r.Summon(_room12,
            new MonsterSpawn(150, new(33, 36)),
            new MonsterSpawn(153, new(21, 31), OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(150, new(33, 19), OnDeath: r =>
                {
                    r.Summon(_room12,
                    new MonsterSpawn(147, new(39, 20), OnDeath: r =>
                    {
                        r.Summon(_room12,
                        new MonsterSpawn(147, new(37, 31), OnDeath: r =>
                        {
                            r.Summon(_room12, new MonsterSpawn(147, new(44, 54), IsBonus: true));
                        }),
                        new MonsterSpawn(147, new(37, 25)),
                        new MonsterSpawn(153, new(20, 40)));
                    }),
                    new MonsterSpawn(153, new(28, 29), OnDeath: r =>
                    {
                        r.Summon(_room12, new MonsterSpawn(153, new(30, 22), OnDeath: r =>
                        {
                            r.Summon(_room12,
                            new MonsterSpawn(147, new(39, 46), IsBonus: true),
                            new MonsterSpawn(147, new(38, 39), OnDeath: r =>
                            {
                                r.Summon(_room12, new MonsterSpawn(147, new(40, 50), IsBonus: true));
                            }));
                        }));
                    }));
                }));
            }),
            new MonsterSpawn(153, new(26, 49), IsBonus: true),
            new MonsterSpawn(153, new(28, 43), OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(147, new(43, 18)));
            }),
            new MonsterSpawn(150, new(33, 27)),
            new MonsterSpawn(150, new(33, 46), IsBonus: true),
            new MonsterSpawn(150, new(33, 52), IsBonus: true));
        r.SpawnPortal(_room13, new(1, 14), _room12, new(58, 37), locked: false);
        r.SpawnPortal(_room13, new(29, 14), _room14, new(1, 14), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(236, new(13, 18), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(245, new(13, 13), OnDeath: r =>
                {
                    r.Summon(_room13, new MonsterSpawn(236, new(20, 14), OnDeath: r =>
                    {
                        r.Summon(_room13, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(236, new(24, 24), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(245, new(12, 19), IsBonus: true));
            }));
        r.SpawnPortal(_room14, new(1, 14), _room13, new(29, 14), locked: false);
        r.SpawnPortal(_room14, new(15, 28), _room15, new(15, 0), locked: false);
        r.Summon(_room14,
            new MonsterSpawn(245, new(13, 13), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(236, new(20, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(205, new(16, 13), IsBonus: true));
                }));
            }),
            new MonsterSpawn(236, new(14, 12)),
            new MonsterSpawn(236, new(16, 25), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(245, new(12, 19), IsBonus: true));
            }));
        r.SpawnExit(_room15, new(15, 28), locked: false);
        r.SpawnPortal(_room15, new(15, 0), _room14, new(15, 28), locked: false);
        r.SummonNpcs(_room15, new NpcSpawn(307, new(11, 15)), new NpcSpawn(335, new(13, 17)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(600));
            r.Dialog(_room2, 6281);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6282);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room7, TimeSpan.FromSeconds(180));
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6285);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6287);
        }
        else if (room == _room15)
        {
            r.Dialog(_room15, 6288);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room7)
        {
            r.StopRoomClock(_room7);
            r.Dialog(_room7, 6284);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room7, _room6);
            r.UnlockPortal(_room7, _room8);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room11);
            r.UnlockPortal(_room10, _room9);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room13);
            r.UnlockPortal(_room12, _room11);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace56();
