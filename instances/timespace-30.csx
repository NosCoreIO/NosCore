public class TimeSpace30 : InstanceScript
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

    public TimeSpace30()
    {
        Definition = InstanceDefinitionBuilder.Named(30, "La poursuite", "La poursuite")
            .ForLevels(40, 99)
            .StartingAt(11, 9)
            .Requiring(1012, 4)
            .WithLives(1)
            .WithDraw(74, 1)
            .WithDraw(82, 1)
            .WithDraw(90, 1)
            .WithSpecialReward(217, 1)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 0, 7)
            .WithRoom(4100, out _room2, 0, 8)
            .WithRoom(4101, out _room3, 1, 8)
            .WithRoom(4104, out _room4, 2, 8)
            .WithRoom(4107, out _room5, 3, 8)
            .WithRoom(4111, out _room6, 4, 8)
            .WithRoom(4102, out _room7, 1, 7)
            .WithRoom(4102, out _room8, 1, 6)
            .WithRoom(4104, out _room9, 2, 7)
            .WithRoom(4106, out _room10, 3, 7)
            .WithRoom(4110, out _room11, 4, 7)
            .WithRoom(4103, out _room12, 2, 6)
            .WithRoom(4106, out _room13, 3, 6)
            .WithRoom(4109, out _room14, 4, 6)
            .WithRoom(4108, out _room15, 3, 9)
            .WithRoom(4105, out _room16, 2, 9)
            .WithRoom(4103, out _room17, 1, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(240));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room7, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(67, new(20, 14), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(67, new(9, 11)));
            }),
            new MonsterSpawn(67, new(17, 17), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(67, new(11, 22)));
            }),
            new MonsterSpawn(67, new(15, 12), IsBonus: true),
            new MonsterSpawn(67, new(6, 15), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(67, new(20, 10)));
            }));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 14), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(138, new(20, 10), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(67, new(12, 15), OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(138, new(15, 20)),
                    new MonsterSpawn(138, new(19, 16)));
                }));
            }),
            new MonsterSpawn(67, new(15, 14), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(138, new(22, 14), IsBonus: true));
            }),
            new MonsterSpawn(138, new(12, 11), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(138, new(5, 17)));
            }));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15), locked: false);
        r.SpawnPortal(_room5, new(14, 28), _room15, new(14, 5));
        r.SpawnPortal(_room5, new(1, 14), _room4, new(28, 15), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(17, new(25, 12)),
            new MonsterSpawn(17, new(21, 17)),
            new MonsterSpawn(17, new(19, 12), IsBonus: true),
            new MonsterSpawn(17, new(18, 22)),
            new MonsterSpawn(17, new(14, 12)),
            new MonsterSpawn(82, new(13, 17)),
            new MonsterSpawn(82, new(12, 14)));
        r.SpawnExit(_room6, new(28, 15));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15));
        r.Summon(_room6,
            new MonsterSpawn(67, new(16, 21), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(82, new(18, 15), OnDeath: r =>
                {
                    r.Summon(_room6,
                    new MonsterSpawn(82, new(17, 9)),
                    new MonsterSpawn(82, new(18, 11)));
                }));
            }),
            new MonsterSpawn(67, new(14, 18), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(82, new(18, 18), IsBonus: true),
                new MonsterSpawn(82, new(18, 21)));
            }),
            new MonsterSpawn(67, new(14, 11), IsBonus: true),
            new MonsterSpawn(67, new(10, 14), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(82, new(17, 23)));
            }));
        r.SpawnPortal(_room7, new(14, 1), _room8, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 14), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room7, new(14, 28), _room3, new(14, 1), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(138, new(24, 17), IsBonus: true),
            new MonsterSpawn(82, new(13, 10), IsBonus: true),
            new MonsterSpawn(82, new(13, 4)),
            new MonsterSpawn(138, new(13, 9), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(138, new(12, 10), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(82, new(17, 16)));
                }));
            }));
        r.SpawnPortal(_room8, new(28, 14), _room12, new(1, 15), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room7, new(14, 1), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(17, new(23, 16), IsBonus: true),
            new MonsterSpawn(17, new(25, 12), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(67, new(20, 17), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(138, new(18, 12), OnDeath: r =>
                    {
                        r.Summon(_room8, new MonsterSpawn(138, new(8, 12), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(67, new(12, 18)));
            }),
            new MonsterSpawn(17, new(7, 16), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(67, new(16, 17), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(138, new(13, 10)));
                }));
            }));
        r.SpawnPortal(_room9, new(28, 15), _room10, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(1, 15), _room7, new(28, 14), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(67, new(16, 21)),
            new MonsterSpawn(67, new(14, 18), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(60, new(21, 12)),
                new MonsterSpawn(60, new(10, 12), IsBonus: true));
            }),
            new MonsterSpawn(67, new(7, 15), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(60, new(21, 17)));
            }));
        r.SpawnPortal(_room10, new(28, 15), _room11, new(1, 15), locked: false);
        r.SpawnPortal(_room10, new(1, 15), _room9, new(28, 15), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(17, new(25, 16)),
            new MonsterSpawn(17, new(20, 14)),
            new MonsterSpawn(137, new(17, 19)),
            new MonsterSpawn(137, new(17, 7)),
            new MonsterSpawn(17, new(15, 17), IsBonus: true),
            new MonsterSpawn(17, new(23, 22)),
            new MonsterSpawn(137, new(14, 9)),
            new MonsterSpawn(137, new(13, 21)));
        r.SpawnPortal(_room11, new(1, 15), _room10, new(28, 15));
        r.Summon(_room11,
            new MonsterSpawn(67, new(19, 14), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(67, new(16, 23)),
                new MonsterSpawn(67, new(18, 20), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(60, new(23, 16)));
                }));
            }),
            new MonsterSpawn(67, new(18, 8), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(143, new(12, 12), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(143, new(11, 14), OnDeath: r =>
                    {
                        r.Summon(_room11, new MonsterSpawn(60, new(23, 13)));
                    }));
                }));
            }),
            new MonsterSpawn(23, new(16, 12)),
            new MonsterSpawn(23, new(15, 16), IsBonus: true),
            new MonsterSpawn(67, new(15, 5)),
            new MonsterSpawn(23, new(13, 9)),
            new MonsterSpawn(23, new(12, 18)),
            new MonsterSpawn(23, new(9, 21)),
            new MonsterSpawn(23, new(9, 7), IsBonus: true),
            new MonsterSpawn(23, new(6, 23)),
            new MonsterSpawn(23, new(23, 10)));
        r.SpawnPortal(_room12, new(28, 15), _room13, new(1, 15), locked: false);
        r.SpawnPortal(_room12, new(1, 15), _room8, new(28, 14), locked: false);
        r.Summon(_room12,
            new MonsterSpawn(82, new(22, 14)),
            new MonsterSpawn(60, new(19, 10)),
            new MonsterSpawn(60, new(15, 8)),
            new MonsterSpawn(82, new(16, 18)),
            new MonsterSpawn(82, new(11, 12), IsBonus: true));
        r.SpawnPortal(_room13, new(28, 15), _room14, new(1, 15), locked: false);
        r.SpawnPortal(_room13, new(1, 15), _room12, new(28, 15), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(142, new(15, 17), IsBonus: true),
            new MonsterSpawn(142, new(14, 20)),
            new MonsterSpawn(142, new(14, 12)),
            new MonsterSpawn(142, new(13, 10), OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(67, new(9, 12), OnDeath: r =>
                {
                    r.Summon(_room13, new MonsterSpawn(60, new(16, 22), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room13, new MonsterSpawn(60, new(19, 8)));
                    }));
                }),
                new MonsterSpawn(67, new(9, 15), OnDeath: r =>
                {
                    r.Summon(_room13, new MonsterSpawn(60, new(22, 12)));
                }),
                new MonsterSpawn(67, new(9, 19), OnDeath: r =>
                {
                    r.Summon(_room13, new MonsterSpawn(60, new(22, 17)));
                }));
            }));
        r.SpawnPortal(_room14, new(1, 15), _room13, new(28, 15));
        r.PlaceButton(_room14, 1, new(15, 15), 1057, 1057, kind: ButtonKind.Key);
        r.SpawnPortal(_room15, new(14, 5), _room5, new(14, 28), locked: false);
        r.SpawnPortal(_room15, new(5, 15), _room16, new(28, 15), locked: false);
        r.Summon(_room15,
            new MonsterSpawn(67, new(18, 10)),
            new MonsterSpawn(67, new(17, 17)),
            new MonsterSpawn(67, new(15, 8)),
            new MonsterSpawn(67, new(13, 11)),
            new MonsterSpawn(67, new(12, 19)));
        r.SpawnPortal(_room16, new(28, 15), _room15, new(5, 15), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room17, new(28, 15), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(23, new(15, 22)),
            new MonsterSpawn(23, new(15, 18), IsBonus: true),
            new MonsterSpawn(23, new(15, 13), IsBonus: true),
            new MonsterSpawn(23, new(15, 9)),
            new MonsterSpawn(67, new(8, 17)),
            new MonsterSpawn(67, new(9, 12)));
        r.SpawnPortal(_room17, new(28, 15), _room16, new(1, 14), locked: false);
        r.SummonNpcs(_room17, new NpcSpawn(122, new(15, 17)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Message(_room2, Game18NConstString.NewGoal);
            r.Dialog(_room2, 6137);
        }
        else if (room == _room6)
        {
            r.Objective(_room6, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room11)
        {
            r.Objective(_room11, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room17)
        {
            r.Message(_room17, Game18NConstString.NewGoal);
            r.Dialog(_room17, 6138);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room6)
        {
            r.UnlockPortal(_room6, _room5);
            r.UnlockExit(_room6);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(_room6, Game18NConstString.ADoorHasOpened);
            r.Message(_room6, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(_room11, Game18NConstString.ADoorHasOpened);
            r.Message(_room11, Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room14 && button == 1)
        {
            r.UnlockPortal(_room14, _room13);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(_room14, Game18NConstString.DoorOpenedSomewhere);
            r.Message(_room14, Game18NConstString.ADoorHasOpened);
            r.Message(_room14, Game18NConstString.FindKey);
            r.Message(_room14, Game18NConstString.NewGoal);
            r.UnlockPortal(_room5, _room15);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace30();
