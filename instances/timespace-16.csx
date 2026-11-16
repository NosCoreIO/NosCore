public class TimeSpace16 : InstanceScript
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

    public TimeSpace16()
    {
        Definition = InstanceDefinitionBuilder.Named(16, "The Monster Army", "The Monster Army")
            .ForLevels(26, 99)
            .StartingAt(11, 10)
            .Requiring(1012, 2)
            .WithLives(2)
            .WithObjective(ObjectiveKind.UseObject, 1048, 1)
            .WithDraw(70, 1)
            .WithDraw(80, 1)
            .WithDraw(88, 1)
            .WithSpecialReward(1011, 50)
            .WithReward(2132, 1)
            .WithReward(1087, 1)
            .WithRoom(4204, out _room1, 5, 8)
            .WithRoom(4205, out _room2, 4, 8)
            .WithRoom(4205, out _room3, 3, 8)
            .WithRoom(4208, out _room4, 2, 8)
            .WithRoom(4211, out _room5, 2, 9)
            .WithRoom(4202, out _room6, 1, 9)
            .WithRoom(4209, out _room7, 3, 9)
            .WithRoom(4212, out _room8, 3, 10)
            .WithRoom(4201, out _room9, 3, 11)
            .WithRoom(4205, out _room10, 4, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(150));
        r.SpawnPortal(_room1, new(1, 15), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room1, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room3, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room2, new(1, 15));
        r.SpawnPortal(_room3, new(1, 15), _room4, new(28, 15));
        r.PlaceButton(_room3, 1, new(5, 15), 1051, 1052);
        r.Summon(_room3,
            new MonsterSpawn(58, new(11, 17)),
            new MonsterSpawn(58, new(11, 12), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(58, new(13, 22)));
            }),
            new MonsterSpawn(271, new(17, 13)));
        r.SpawnPortal(_room4, new(28, 15), _room3, new(1, 15));
        r.SpawnPortal(_room4, new(14, 28), _room5, new(14, 1));
        r.PlaceButton(_room4, 1, new(5, 15), 1051, 1052);
        r.Summon(_room4,
            new MonsterSpawn(64, new(13, 22)),
            new MonsterSpawn(64, new(7, 20), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(58, new(12, 15)));
            }),
            new MonsterSpawn(271, new(14, 18)));
        r.SpawnPortal(_room5, new(14, 1), _room4, new(14, 28));
        r.SpawnPortal(_room5, new(29, 15), _room7, new(1, 15));
        r.SpawnPortal(_room5, new(1, 15), _room6, new(28, 15));
        r.PlaceButton(_room5, 1, new(15, 17), 1051, 1052);
        r.Summon(_room5,
            new MonsterSpawn(20, new(7, 10), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(58, new(6, 18)),
                new MonsterSpawn(58, new(10, 21)));
            }),
            new MonsterSpawn(271, new(14, 18), IsBonus: true));
        r.SpawnPortal(_room6, new(28, 15), _room5, new(1, 15));
        r.Summon(_room6,
            new MonsterSpawn(58, new(16, 20)),
            new MonsterSpawn(58, new(12, 11), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(66, new(18, 14), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(66, new(6, 16), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(66, new(12, 17), OnDeath: r =>
                        {
                            r.Summon(_room6, new MonsterSpawn(66, new(13, 21)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room7, new(14, 28), _room8, new(14, 1));
        r.SpawnPortal(_room7, new(1, 15), _room5, new(29, 15));
        r.PlaceButton(_room7, 1, new(22, 15), 1051, 1052);
        r.Summon(_room7,
            new MonsterSpawn(58, new(20, 20)),
            new MonsterSpawn(58, new(8, 20), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(58, new(23, 18)));
            }),
            new MonsterSpawn(271, new(14, 18)));
        r.SpawnPortal(_room8, new(14, 1), _room7, new(14, 28));
        r.SpawnPortal(_room8, new(28, 15), _room10, new(1, 15));
        r.SpawnPortal(_room8, new(14, 28), _room9, new(14, 1));
        r.PlaceButton(_room8, 1, new(15, 15), 1051, 1052);
        r.Summon(_room8,
            new MonsterSpawn(58, new(9, 17), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(58, new(11, 21)),
                new MonsterSpawn(58, new(19, 22)));
            }),
            new MonsterSpawn(58, new(9, 14)),
            new MonsterSpawn(271, new(14, 22), IsBonus: true));
        r.SpawnPortal(_room9, new(14, 1), _room8, new(14, 28));
        r.Summon(_room9,
            new MonsterSpawn(20, new(15, 17), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(58, new(22, 11), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(66, new(12, 12), OnDeath: r =>
                    {
                        r.Summon(_room9, new MonsterSpawn(66, new(17, 13)));
                    }));
                }));
            }),
            new MonsterSpawn(58, new(9, 9), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(66, new(16, 18), OnDeath: r =>
                {
                    r.Summon(_room9,
                    new MonsterSpawn(58, new(9, 17), IsBonus: true),
                    new MonsterSpawn(58, new(23, 18)));
                }));
            }));
        r.SpawnExit(_room10, new(28, 15));
        r.SpawnPortal(_room10, new(1, 15), _room8, new(28, 15));
        r.PlaceButton(_room10, 1, new(13, 16), 1048, 1049, kind: ButtonKind.Sculpture);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room2, 6078);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6079);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6079);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6079);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room6)
        {
            r.Objective(_room6, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6079);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6079);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room9)
        {
            r.Objective(_room9, LanguageKey.TIMESPACE_GOAL_DARK_GARGS_NOT_HERE);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6081);
            r.Message(LanguageKey.TIMESPACE_SEAL_ROOM);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room6)
        {
            r.UnlockPortal(_room6, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room8);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(LanguageKey.TIMESPACE_NO_EXIT_IN_THIS_ROOM);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room4 && button == 1)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room5 && button == 1)
        {
            r.UnlockPortal(_room5, _room7);
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room7 && button == 1)
        {
            r.UnlockPortal(_room7, _room8);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room8 && button == 1)
        {
            r.UnlockPortal(_room8, _room9);
            r.UnlockPortal(_room8, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
            r.Message(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE);
        }
        else if (room == _room10 && button == 1)
        {
            r.StartRoomClock(_room10, TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room10, 6082);
            r.Objective(_room10, LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_MONSTER_LEAVES);
            r.Summon(_room10,
                new MonsterSpawn(18, new(25, 12), OnDeath: r =>
                {
                    r.Summon(_room10,
                    new MonsterSpawn(57, new(10, 10), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(136, new(17, 17), OnDeath: r =>
                        {
                            r.Summon(_room10,
                            new MonsterSpawn(58, new(9, 7)),
                            new MonsterSpawn(58, new(18, 8)),
                            new MonsterSpawn(58, new(20, 21)),
                            new MonsterSpawn(58, new(25, 15)));
                        }));
                    }),
                    new MonsterSpawn(57, new(11, 16), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(57, new(15, 10), OnDeath: r =>
                        {
                            r.Summon(_room10,
                            new MonsterSpawn(57, new(16, 21), OnDeath: r =>
                            {
                                r.Summon(_room10,
                                new MonsterSpawn(136, new(14, 12), OnDeath: r =>
                                {
                                    r.Summon(_room10, new MonsterSpawn(58, new(15, 17)));
                                }),
                                new MonsterSpawn(136, new(14, 21), OnDeath: r =>
                                {
                                    r.Summon(_room10,
                                    new MonsterSpawn(58, new(12, 13)),
                                    new MonsterSpawn(58, new(12, 24)));
                                }),
                                new MonsterSpawn(136, new(17, 13)));
                            }),
                            new MonsterSpawn(57, new(20, 17), OnDeath: r =>
                            {
                                r.Summon(_room10,
                                new MonsterSpawn(57, new(11, 20)),
                                new MonsterSpawn(57, new(6, 16)));
                            }));
                        }));
                    }));
                }),
                new MonsterSpawn(18, new(23, 12)),
                new MonsterSpawn(131, new(21, 15)),
                new MonsterSpawn(131, new(19, 19), OnDeath: r =>
                {
                    r.Summon(_room10,
                    new MonsterSpawn(136, new(7, 21), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(136, new(19, 12)));
                    }),
                    new MonsterSpawn(136, new(8, 16), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(136, new(15, 7)));
                    }));
                }),
                new MonsterSpawn(18, new(17, 6)),
                new MonsterSpawn(18, new(17, 4)),
                new MonsterSpawn(131, new(13, 10)),
                new MonsterSpawn(131, new(9, 19), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(136, new(6, 12)));
                }),
                new MonsterSpawn(131, new(8, 13)),
                new MonsterSpawn(271, new(14, 15)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Dismiss(_room10, 18);
                r.Dismiss(_room10, 57);
                r.Dismiss(_room10, 58);
                r.Dismiss(_room10, 131);
                r.Dismiss(_room10, 136);
                r.Dismiss(_room10, 271);
                r.UnlockExit(_room10);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(Game18NConstString.NewGoal);
                r.Message(LanguageKey.TIMESPACE_DARK_GARGS_GONE_EXIT_OPENS);
                r.StopRoomClock(_room10);
            });
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace16();
