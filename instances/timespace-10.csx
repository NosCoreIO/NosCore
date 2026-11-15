public class TimeSpace10 : InstanceScript
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

    public TimeSpace10()
    {
        Definition = InstanceDefinitionBuilder.Named(10, "A Bold Warrior: Part 1", "A Bold Warrior: Part 1")
            .ForLevels(20, 99)
            .StartingAt(7, 10)
            .Requiring(1012, 2)
            .WithDraw(135, 1)
            .WithDraw(142, 1)
            .WithDraw(149, 1)
            .WithSpecialReward(1031, 2)
            .WithReward(2028, 2)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 3, 7)
            .WithRoom(4100, out _room2, 3, 8)
            .WithRoom(4101, out _room3, 3, 9)
            .WithRoom(4103, out _room4, 4, 9)
            .WithRoom(4107, out _room5, 5, 9)
            .WithRoom(4102, out _room6, 2, 9)
            .WithRoom(4108, out _room7, 1, 9)
            .WithRoom(4101, out _room8, 3, 10)
            .WithRoom(4105, out _room9, 4, 10)
            .WithRoom(4107, out _room10, 5, 10)
            .WithRoom(4103, out _room11, 2, 10)
            .WithRoom(4108, out _room12, 1, 10)
            .WithRoom(4105, out _room13, 3, 11)
            .WithRoom(4109, out _room14, 4, 11)
            .WithRoom(4109, out _room15, 2, 11)
            .WithRoom(4110, out _room16, 1, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(240));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(14, 28), _room8, new(14, 1));
        r.SpawnPortal(_room3, new(1, 14), _room6, new(28, 14));
        r.Summon(_room3,
            new MonsterSpawn(131, new(21, 18)),
            new MonsterSpawn(131, new(8, 17), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(19, new(14, 12), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(131, new(12, 19)));
                }),
                new MonsterSpawn(131, new(23, 14), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(57, new(11, 10)),
                    new MonsterSpawn(57, new(20, 10), OnDeath: r =>
                    {
                        r.Summon(_room3, new MonsterSpawn(57, new(15, 17)));
                    }));
                }));
            }));
        r.SummonNpcs(_room3, new NpcSpawn(317, new(14, 7)));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 14), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15), locked: false);
        r.SpawnPortal(_room5, new(1, 14), _room4, new(28, 15));
        r.PlaceButton(_room5, 1, new(20, 15), 1055, 1056);
        r.SpawnPortal(_room6, new(28, 14), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room6, new(1, 14), _room7, new(28, 15));
        r.PlaceButton(_room6, 1, new(15, 15), 1000, 1045);
        r.Summon(_room6, new MonsterSpawn(18, new(21, 9), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(131, new(8, 12), IsBonus: true));
            }));
        r.SpawnPortal(_room7, new(28, 15), _room6, new(1, 14));
        r.PlaceButton(_room7, 1, new(10, 15), 1055, 1056);
        r.SpawnPortal(_room8, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room9, new(1, 14), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room13, new(14, 1));
        r.SpawnPortal(_room8, new(1, 14), _room11, new(28, 15), locked: false);
        r.SpawnPortal(_room9, new(28, 15), _room10, new(1, 14));
        r.SpawnPortal(_room9, new(1, 14), _room8, new(28, 15), locked: false);
        r.PlaceButton(_room9, 1, new(18, 11), 1000, 1045);
        r.Summon(_room9,
            new MonsterSpawn(129, new(17, 8)),
            new MonsterSpawn(129, new(16, 5), IsBonus: true),
            new MonsterSpawn(129, new(10, 4)),
            new MonsterSpawn(129, new(7, 14)));
        r.SpawnPortal(_room10, new(1, 14), _room9, new(28, 15));
        r.PlaceButton(_room10, 1, new(20, 15), 1055, 1056);
        r.SpawnPortal(_room11, new(28, 15), _room8, new(1, 14), locked: false);
        r.SpawnPortal(_room11, new(1, 15), _room12, new(28, 15), locked: false);
        r.Summon(_room11,
            new MonsterSpawn(19, new(25, 20), IsBonus: true),
            new MonsterSpawn(19, new(25, 9)),
            new MonsterSpawn(19, new(22, 20)),
            new MonsterSpawn(19, new(22, 9)),
            new MonsterSpawn(19, new(19, 20)),
            new MonsterSpawn(19, new(19, 9)),
            new MonsterSpawn(19, new(17, 20)),
            new MonsterSpawn(19, new(17, 9)),
            new MonsterSpawn(19, new(12, 20)),
            new MonsterSpawn(19, new(12, 9)),
            new MonsterSpawn(19, new(10, 20)),
            new MonsterSpawn(19, new(10, 9), IsBonus: true),
            new MonsterSpawn(19, new(7, 20)),
            new MonsterSpawn(19, new(7, 9)),
            new MonsterSpawn(19, new(4, 20)),
            new MonsterSpawn(19, new(4, 9), IsBonus: true));
        r.SpawnPortal(_room12, new(28, 15), _room11, new(1, 15));
        r.PlaceButton(_room12, 1, new(10, 15), 1055, 1056);
        r.SpawnPortal(_room13, new(14, 1), _room8, new(14, 28), locked: false);
        r.SpawnPortal(_room13, new(28, 15), _room14, new(1, 15), locked: false);
        r.SpawnPortal(_room13, new(1, 14), _room15, new(28, 15), locked: false);
        r.SpawnPortal(_room14, new(1, 15), _room13, new(28, 15));
        r.PlaceButton(_room14, 1, new(15, 15), 1051, 1052);
        r.Summon(_room14,
            new MonsterSpawn(136, new(14, 11)),
            new MonsterSpawn(136, new(10, 15)),
            new MonsterSpawn(19, new(10, 11), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(136, new(15, 17), OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(58, new(20, 20)));
                }));
            }));
        r.SpawnPortal(_room15, new(28, 15), _room13, new(1, 14));
        r.SpawnPortal(_room15, new(1, 15), _room16, new(28, 15));
        r.PlaceButton(_room15, 1, new(15, 15), 1055, 1056);
        r.SpawnPortal(_room16, new(28, 15), _room15, new(1, 15));
        r.SpawnExit(_room16, new(1, 15));
        r.Summon(_room16,
            new MonsterSpawn(130, new(14, 11)),
            new MonsterSpawn(130, new(13, 21)),
            new MonsterSpawn(130, new(13, 15), IsBonus: true),
            new MonsterSpawn(130, new(11, 19)),
            new MonsterSpawn(130, new(11, 17)),
            new MonsterSpawn(130, new(10, 11)));
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
            r.Dialog(_room2, 6052);
        }
        else if (room == _room16)
        {
            r.Dialog(_room16, 6060);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room6);
            r.UnlockPortal(_room3, _room8);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room3, 6053);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room12)
        {
            r.UnlockPortal(_room12, _room11);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room8, _room13);
        }
        else if (room == _room14)
        {
            r.UnlockPortal(_room14, _room13);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room15)
        {
            r.UnlockPortal(_room15, _room13);
            r.UnlockPortal(_room15, _room16);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room16)
        {
            r.UnlockExit(_room16);
            r.UnlockPortal(_room16, _room15);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room16, 6061);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room5 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room5, 6055);
            r.Summon(_room5,
                new MonsterSpawn(129, new(25, 19)),
                new MonsterSpawn(129, new(25, 17), IsBonus: true),
                new MonsterSpawn(129, new(22, 22)),
                new MonsterSpawn(129, new(19, 23)),
                new MonsterSpawn(56, new(16, 16)),
                new MonsterSpawn(129, new(14, 21)),
                new MonsterSpawn(129, new(12, 15)));
        }
        else if (room == _room6 && button == 1)
        {
            r.UnlockPortal(_room6, _room7);
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room7 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room7, 6054);
            r.Summon(_room7,
                new MonsterSpawn(130, new(15, 15)),
                new MonsterSpawn(130, new(15, 8), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(57, new(18, 20), OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(57, new(18, 13), OnDeath: r =>
                        {
                            r.Summon(_room7, new MonsterSpawn(57, new(12, 9)));
                        }));
                    }));
                }),
                new MonsterSpawn(130, new(13, 18)),
                new MonsterSpawn(18, new(7, 11)));
        }
        else if (room == _room9 && button == 1)
        {
            r.UnlockPortal(_room9, _room10);
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room10 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room10, 6056);
            r.Summon(_room10,
                new MonsterSpawn(57, new(17, 16), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(57, new(7, 12)));
                }),
                new MonsterSpawn(57, new(17, 11), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(57, new(14, 19), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(57, new(13, 8), OnDeath: r =>
                        {
                            r.Summon(_room10, new MonsterSpawn(102, new(19, 19), OnDeath: r =>
                            {
                                r.Summon(_room10, new MonsterSpawn(136, new(15, 14)));
                            }));
                        }));
                    }));
                }));
        }
        else if (room == _room12 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room12, 6057);
            r.Summon(_room12,
                new MonsterSpawn(129, new(9, 22), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(57, new(11, 10), OnDeath: r =>
                    {
                        r.Summon(_room12, new MonsterSpawn(129, new(13, 12)));
                    }));
                }),
                new MonsterSpawn(129, new(9, 7), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(57, new(10, 20)));
                }),
                new MonsterSpawn(19, new(7, 13), IsBonus: true),
                new MonsterSpawn(129, new(5, 17)),
                new MonsterSpawn(129, new(5, 12)));
        }
        else if (room == _room14 && button == 1)
        {
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room15 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room15, 6058);
            r.Summon(_room15,
                new MonsterSpawn(57, new(12, 10), OnDeath: r =>
                {
                    r.Summon(_room15, new MonsterSpawn(57, new(16, 10), OnDeath: r =>
                    {
                        r.Summon(_room15, new MonsterSpawn(57, new(14, 21), OnDeath: r =>
                        {
                            r.Summon(_room15,
                            new MonsterSpawn(136, new(11, 14)),
                            new MonsterSpawn(136, new(20, 14)));
                        }));
                    }));
                }),
                new MonsterSpawn(57, new(8, 17), OnDeath: r =>
                {
                    r.Summon(_room15, new MonsterSpawn(57, new(22, 17)));
                }));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace10();
