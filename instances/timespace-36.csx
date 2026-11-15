// Alliance with the Kovolts
public class TimeSpace36 : InstanceScript
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

    public TimeSpace36()
    {
        Definition = InstanceDefinitionBuilder.Named(36, "The Kenkos are attacking the Kovolts. Their land was taken by humans, and now the Kenkos threaten their very existence. Help them and show them that humans are not evil and can even be valuable friends.", "Alliance with the Kovolts")
            .ForLevels(46, 99)
            .StartingAt(15, 10)
            .WithLives(2)
            .Requiring(1012, 4)
            .WithDraw(713, 1)
            .WithDraw(526, 1)
            .WithSpecialReward(1016, 2)
            .WithReward(2127, 1)
            .WithReward(1087, 1)
            .WithRoom(4201, out _room1, 4, 11)
            .WithRoom(4209, out _room2, 4, 11)
            .WithRoom(4207, out _room3, 4, 10)
            .WithRoom(4206, out _room4, 3, 10)
            .WithRoom(4208, out _room5, 3, 9)
            .WithRoom(4205, out _room6, 3, 8)
            .WithRoom(4210, out _room7, 4, 8)
            .WithRoom(4208, out _room8, 5, 8)
            .WithRoom(4210, out _room9, 5, 7)
            .WithRoom(4209, out _room10, 6, 7)
            .WithRoom(4205, out _room11, 6, 6)
            .WithRoom(4207, out _room12, 5, 6)
            .WithRoom(4206, out _room13, 4, 6)
            .WithRoom(4206, out _room14, 4, 5)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room3, new(28, 15), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.Summon(_room2,
            new MonsterSpawn(61, new(11, 9), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(61, new(17, 17)));
            }),
            new MonsterSpawn(61, new(10, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(61, new(18, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room2,
                    new MonsterSpawn(67, new(22, 15), IsBonus: true),
                    new MonsterSpawn(67, new(19, 9), IsBonus: true));
                }));
            }),
            new MonsterSpawn(67, new(14, 26), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(67, new(7, 16)));
            }));
        r.SpawnPortal(_room3, new(28, 15), _room2, new(1, 15));
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28));
        r.Summon(_room3,
            new MonsterSpawn(149, new(12, 10)),
            new MonsterSpawn(149, new(22, 15)));
        r.SummonNpcs(_room3, new NpcSpawn(122, new(8, 17)), new NpcSpawn(122, new(14, 19)), new NpcSpawn(116, new(19, 21)));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(67, new(22, 8), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(61, new(17, 17), OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(67, new(7, 16), IsBonus: true));
                }));
            }),
            new MonsterSpawn(67, new(22, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(61, new(18, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15));
        r.Summon(_room5,
            new MonsterSpawn(149, new(12, 10)),
            new MonsterSpawn(149, new(22, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(149, new(17, 10), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(152, new(21, 19), IsBonus: true));
                }),
                new MonsterSpawn(152, new(8, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(152, new(17, 15), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(152, new(7, 12), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room5,
                            new MonsterSpawn(146, new(20, 13), IsBonus: true, OnDeath: r =>
                            {
                                r.Message(Game18NConstString.QuickMissionComplete);
                                r.AddClockTime(TimeSpan.FromSeconds(120));
                                r.Message(Game18NConstString.ADoorHasOpened);
                                r.UnlockPortal(_room5, _room4);
                                r.UnlockPortal(_room5, _room6);
                            }),
                            new MonsterSpawn(146, new(10, 13), IsBonus: true));
                        }));
                    }));
                }));
            }));
        r.SummonNpcs(_room5, new NpcSpawn(116, new(6, 19)), new NpcSpawn(122, new(9, 21)), new NpcSpawn(122, new(12, 20)), new NpcSpawn(116, new(21, 17)));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15), locked: false);
        r.SummonNpcs(_room6, new NpcSpawn(319, new(8, 14)));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15));
        r.SpawnPortal(_room7, new(15, 1), _room8, new(14, 28));
        r.Summon(_room7,
            new MonsterSpawn(149, new(4, 14)),
            new MonsterSpawn(149, new(12, 10)),
            new MonsterSpawn(149, new(23, 13)));
        r.SummonNpcs(_room7, new NpcSpawn(116, new(6, 19)), new NpcSpawn(122, new(10, 19)), new NpcSpawn(122, new(14, 19)), new NpcSpawn(116, new(19, 21)));
        r.SpawnPortal(_room8, new(14, 28), _room7, new(15, 1), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room9, new(1, 15), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(61, new(10, 8), IsBonus: true),
            new MonsterSpawn(61, new(10, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(61, new(17, 17), OnDeath: r =>
                {
                    r.Summon(_room8,
                    new MonsterSpawn(67, new(22, 15), IsBonus: true),
                    new MonsterSpawn(67, new(19, 9), IsBonus: true));
                }));
            }),
            new MonsterSpawn(67, new(20, 17), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(61, new(18, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(67, new(7, 16), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room9, new(1, 15), _room8, new(28, 15));
        r.SpawnPortal(_room9, new(15, 1), _room10, new(14, 28));
        r.Summon(_room9,
            new MonsterSpawn(149, new(4, 14)),
            new MonsterSpawn(149, new(12, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(149, new(22, 15), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(152, new(21, 19)));
                }),
                new MonsterSpawn(152, new(8, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9,
                    new MonsterSpawn(149, new(17, 10), IsBonus: true),
                    new MonsterSpawn(152, new(15, 17), OnDeath: r =>
                    {
                        r.Summon(_room9, new MonsterSpawn(149, new(24, 13), IsBonus: true, OnDeath: r =>
                        {
                            r.Message(Game18NConstString.QuickMissionComplete);
                            r.AddClockTime(TimeSpan.FromSeconds(150));
                            r.Message(Game18NConstString.ADoorHasOpened);
                            r.UnlockPortal(_room9, _room8);
                            r.UnlockPortal(_room9, _room10);
                        }));
                    }));
                }));
            }));
        r.SummonNpcs(_room9, new NpcSpawn(116, new(6, 19)), new NpcSpawn(122, new(10, 19)), new NpcSpawn(122, new(14, 19)), new NpcSpawn(122, new(17, 20)));
        r.SpawnPortal(_room10, new(1, 15), _room11, new(28, 15), locked: false);
        r.SpawnPortal(_room10, new(14, 28), _room9, new(15, 1), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(61, new(13, 10), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(61, new(18, 14), OnDeath: r =>
                {
                    r.Summon(_room10,
                    new MonsterSpawn(67, new(22, 15), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(142, new(7, 12)));
                    }),
                    new MonsterSpawn(67, new(19, 9)));
                }));
            }),
            new MonsterSpawn(61, new(11, 9), IsBonus: true),
            new MonsterSpawn(142, new(19, 19), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(61, new(17, 17)));
            }),
            new MonsterSpawn(67, new(15, 27), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(67, new(7, 16), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(142, new(15, 13)));
                }));
            }));
        r.SpawnPortal(_room11, new(1, 15), _room12, new(28, 15), locked: false);
        r.SpawnPortal(_room11, new(28, 15), _room10, new(1, 15), locked: false);
        r.SummonNpcs(_room11, new NpcSpawn(319, new(14, 15)));
        r.SpawnPortal(_room12, new(28, 15), _room11, new(1, 15));
        r.SpawnPortal(_room12, new(14, 1), _room13, new(14, 28));
        r.Summon(_room12,
            new MonsterSpawn(149, new(4, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(149, new(24, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Dialog(_room12, 6153);
                    r.AddClockTime(TimeSpan.FromSeconds(150));
                    r.Message(Game18NConstString.ADoorHasOpened);
                    r.UnlockPortal(_room12, _room11);
                    r.UnlockPortal(_room12, _room13);
                }),
                new MonsterSpawn(152, new(15, 17), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(152, new(7, 12), OnDeath: r =>
                    {
                        r.Summon(_room12, new MonsterSpawn(149, new(18, 15)));
                    }));
                }));
            }),
            new MonsterSpawn(149, new(12, 10), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(149, new(17, 10)),
                new MonsterSpawn(152, new(8, 16), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(152, new(21, 19), OnDeath: r =>
                    {
                        r.Summon(_room12,
                        new MonsterSpawn(146, new(20, 13), OnDeath: r =>
                        {
                            r.Summon(_room12, new MonsterSpawn(149, new(11, 15)));
                        }),
                        new MonsterSpawn(146, new(10, 13)));
                    }));
                }));
            }),
            new MonsterSpawn(149, new(22, 15)));
        r.SummonNpcs(_room12, new NpcSpawn(116, new(6, 19)), new NpcSpawn(122, new(10, 19)), new NpcSpawn(122, new(14, 19)), new NpcSpawn(122, new(17, 19)), new NpcSpawn(116, new(19, 21)), new NpcSpawn(122, new(23, 20)));
        r.SpawnPortal(_room13, new(14, 28), _room12, new(14, 1), locked: false);
        r.SpawnPortal(_room13, new(14, 1), _room14, new(14, 28), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(61, new(16, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(67, new(7, 16), IsBonus: true));
            }),
            new MonsterSpawn(61, new(10, 19), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(61, new(17, 17)));
            }),
            new MonsterSpawn(67, new(19, 20), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(61, new(18, 14), OnDeath: r =>
                {
                    r.Summon(_room13,
                    new MonsterSpawn(67, new(22, 15), IsBonus: true),
                    new MonsterSpawn(67, new(19, 9), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room14, new(14, 28), _room13, new(14, 1));
        r.SpawnExit(_room14, new(14, 1));
        r.Summon(_room14,
            new MonsterSpawn(149, new(4, 14), OnDeath: r =>
            {
                r.Summon(_room14,
                new MonsterSpawn(149, new(24, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Dialog(_room14, 6155);
                    r.Message(Game18NConstString.ADoorHasOpened);
                    r.UnlockPortal(_room14, _room13);
                    r.UnlockExit(_room14);
                }),
                new MonsterSpawn(152, new(16, 15), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(152, new(7, 12), IsBonus: true));
                }));
            }),
            new MonsterSpawn(149, new(12, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room14,
                new MonsterSpawn(149, new(17, 10), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14,
                    new MonsterSpawn(146, new(20, 13), IsBonus: true),
                    new MonsterSpawn(146, new(10, 13), OnDeath: r =>
                    {
                        r.Summon(_room14, new MonsterSpawn(149, new(18, 15)));
                    }));
                }),
                new MonsterSpawn(152, new(8, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(152, new(21, 19), OnDeath: r =>
                    {
                        r.Summon(_room14, new MonsterSpawn(149, new(11, 15)));
                    }));
                }));
            }),
            new MonsterSpawn(149, new(22, 15)));
        r.SummonNpcs(_room14, new NpcSpawn(122, new(8, 21)), new NpcSpawn(122, new(14, 22)), new NpcSpawn(122, new(20, 21)));
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
        }
        else if (room == _room3)
        {
            r.Summon(_room3,
                        new MonsterSpawn(149, new(13, 9), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room3,
                            new MonsterSpawn(149, new(17, 10), OnDeath: r =>
                            {
                                r.Summon(_room3, new MonsterSpawn(152, new(15, 17), OnDeath: r =>
                                {
                                    r.Summon(_room3, new MonsterSpawn(152, new(7, 12), IsBonus: true, OnDeath: r =>
                                    {
                                        r.Message(Game18NConstString.QuickMissionComplete);
                                        r.AddClockTime(TimeSpan.FromSeconds(150));
                                        r.Message(Game18NConstString.ADoorHasOpened);
                                        r.UnlockPortal(_room3, _room2);
                                        r.UnlockPortal(_room3, _room4);
                                    }));
                                }));
                            }),
                            new MonsterSpawn(152, new(8, 16), IsBonus: true, OnDeath: r =>
                            {
                                r.Summon(_room3, new MonsterSpawn(152, new(21, 19), IsBonus: true));
                            }));
                        }),
                        new MonsterSpawn(149, new(22, 16)));
            r.Dialog(_room3, 6151);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6151);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6152);
        }
        else if (room == _room7)
        {
            r.Summon(_room7,
                        new MonsterSpawn(149, new(4, 14)),
                        new MonsterSpawn(149, new(11, 12), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room7,
                            new MonsterSpawn(149, new(17, 10), OnDeath: r =>
                            {
                                r.Summon(_room7, new MonsterSpawn(152, new(7, 12), OnDeath: r =>
                                {
                                    r.Summon(_room7,
                                    new MonsterSpawn(146, new(20, 13)),
                                    new MonsterSpawn(146, new(10, 13)));
                                }));
                            }),
                            new MonsterSpawn(152, new(8, 16), OnDeath: r =>
                            {
                                r.Summon(_room7, new MonsterSpawn(152, new(21, 19)));
                            }));
                        }),
                        new MonsterSpawn(149, new(21, 15), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room7,
                            new MonsterSpawn(149, new(24, 13), IsBonus: true, OnDeath: r =>
                            {
                                r.Message(Game18NConstString.QuickMissionComplete);
                                r.AddClockTime(TimeSpan.FromSeconds(120));
                                r.Message(Game18NConstString.ADoorHasOpened);
                                r.UnlockPortal(_room7, _room6);
                                r.UnlockPortal(_room7, _room8);
                            }),
                            new MonsterSpawn(152, new(17, 15)));
                        }));
            r.Dialog(_room7, 6151);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6151);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6152);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6151);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6154);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace36();
