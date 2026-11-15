// The Kenko Mystery
public class TimeSpace37 : InstanceScript
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
    private readonly int _room22;

    public TimeSpace37()
    {
        Definition = InstanceDefinitionBuilder.Named(37, "The Kenko troops are up to something, but we can't wait for the Koarens to solve the mystery. Show them what a real adventurer is made of!", "The Kenko Mystery")
            .ForLevels(47, 99)
            .StartingAt(9, 10)
            .WithLives(1)
            .Requiring(1012, 4)
            .WithDraw(157, 1)
            .WithDraw(159, 1)
            .WithDraw(161, 1)
            .WithSpecialReward(1021, 2)
            .WithReward(2127, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 9)
            .WithRoom(4205, out _room2, 0, 9)
            .WithRoom(4200, out _room3, 1, 9)
            .WithRoom(4208, out _room4, 2, 9)
            .WithRoom(4205, out _room5, 2, 8)
            .WithRoom(4205, out _room6, 3, 8)
            .WithRoom(4205, out _room7, 4, 8)
            .WithRoom(4204, out _room8, 5, 8)
            .WithRoom(4205, out _room9, 6, 8)
            .WithRoom(4213, out _room10, 3, 9)
            .WithRoom(4213, out _room11, 4, 9)
            .WithRoom(4200, out _room12, 2, 10)
            .WithRoom(4213, out _room13, 3, 10)
            .WithRoom(4213, out _room14, 4, 10)
            .WithRoom(4201, out _room15, 5, 9)
            .WithRoom(4213, out _room16, 5, 10)
            .WithRoom(4206, out _room17, 6, 9)
            .WithRoom(4210, out _room18, 6, 10)
            .WithRoom(4205, out _room19, 6, 11)
            .WithRoom(4205, out _room20, 5, 11)
            .WithRoom(4205, out _room21, 4, 11)
            .WithRoom(4205, out _room22, 3, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(85, new(13, 17), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(85, new(9, 11), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(62, new(15, 20), IsBonus: true));
                }));
            }),
            new MonsterSpawn(85, new(18, 9), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(62, new(19, 14), IsBonus: true),
                new MonsterSpawn(62, new(8, 16), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(75, new(13, 12), IsBonus: true));
                }));
            }),
            new MonsterSpawn(85, new(17, 18), IsBonus: true));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(138, new(7, 20)),
            new MonsterSpawn(138, new(18, 13), IsBonus: true),
            new MonsterSpawn(138, new(25, 18), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(73, new(18, 9), OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(61, new(13, 14), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(69, new(16, 19), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room4,
                            new MonsterSpawn(69, new(20, 14), IsBonus: true),
                            new MonsterSpawn(69, new(13, 11), IsBonus: true));
                        }));
                    }));
                }),
                new MonsterSpawn(73, new(9, 9), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(61, new(8, 16), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15), locked: false);
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(85, new(17, 14)),
            new MonsterSpawn(85, new(17, 10), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(85, new(9, 11), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(62, new(15, 20), OnDeath: r =>
                    {
                        r.Summon(_room5,
                        new MonsterSpawn(62, new(19, 14), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room5, new MonsterSpawn(75, new(13, 12), IsBonus: true));
                        }),
                        new MonsterSpawn(62, new(8, 16)));
                    }));
                }));
            }),
            new MonsterSpawn(85, new(22, 19), IsBonus: true));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15));
        r.PlaceButton(_room6, 1, new(14, 14), 1000, 1045);
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room8, new(1, 15), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(85, new(15, 21), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(85, new(9, 11), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(62, new(15, 20), IsBonus: true));
                }));
            }),
            new MonsterSpawn(85, new(20, 7), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(62, new(19, 14), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(75, new(13, 12), IsBonus: true));
                }),
                new MonsterSpawn(62, new(8, 16), IsBonus: true));
            }),
            new MonsterSpawn(85, new(17, 15), IsBonus: true));
        r.SpawnPortal(_room8, new(1, 15), _room9, new(28, 15));
        r.PlaceButton(_room8, 1, new(14, 22), 1000, 1045);
        r.Summon(_room8,
            new MonsterSpawn(152, new(18, 9), IsBonus: true),
            new MonsterSpawn(152, new(20, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(152, new(18, 18), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(147, new(11, 8), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room8,
                        new MonsterSpawn(147, new(16, 20), OnDeath: r =>
                        {
                            r.Summon(_room8, new MonsterSpawn(147, new(9, 17), IsBonus: true));
                        }),
                        new MonsterSpawn(147, new(12, 13), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room9, new(1, 15), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(28, 15), _room8, new(1, 15), locked: false);
        r.SpawnPortal(_room10, new(1, 15), _room11, new(28, 15), locked: false);
        r.SpawnPortal(_room11, new(1, 15), _room11, new(1, 15), locked: false);
        r.SpawnPortal(_room11, new(28, 15), _room10, new(1, 15), locked: false);
        r.SpawnPortal(_room12, new(14, 28), _room11, new(14, 0), locked: false);
        r.SpawnPortal(_room12, new(28, 15), _room12, new(28, 15), locked: false);
        r.SpawnPortal(_room13, new(14, 28), _room12, new(14, 0), locked: false);
        r.SpawnPortal(_room13, new(28, 15), _room13, new(28, 15), locked: false);
        r.SpawnPortal(_room14, new(14, 28), _room13, new(14, 0), locked: false);
        r.SpawnPortal(_room14, new(28, 15), _room14, new(28, 15), locked: false);
        r.SpawnPortal(_room15, new(14, 1), _room16, new(14, 28));
        r.SpawnPortal(_room16, new(14, 28), _room15, new(14, 1), locked: false);
        r.SpawnPortal(_room16, new(28, 15), _room16, new(28, 15), locked: false);
        r.SpawnPortal(_room17, new(14, 28), _room18, new(15, 1), locked: false);
        r.SpawnPortal(_room17, new(14, 1), _room16, new(14, 0), locked: false);
        r.Summon(_room17,
            new MonsterSpawn(85, new(20, 17), IsBonus: true),
            new MonsterSpawn(85, new(18, 5), OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(85, new(9, 11), OnDeath: r =>
                {
                    r.Summon(_room17,
                    new MonsterSpawn(62, new(19, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room17, new MonsterSpawn(75, new(13, 12), IsBonus: true));
                    }),
                    new MonsterSpawn(62, new(8, 16), IsBonus: true));
                }));
            }),
            new MonsterSpawn(85, new(25, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(62, new(15, 20), IsBonus: true));
            }));
        r.SpawnPortal(_room18, new(1, 15), _room19, new(28, 15));
        r.SpawnPortal(_room18, new(15, 1), _room17, new(14, 28));
        r.Summon(_room18,
            new MonsterSpawn(149, new(12, 12), OnDeath: r =>
            {
                r.Summon(_room18, new MonsterSpawn(152, new(12, 16), IsBonus: true));
            }),
            new MonsterSpawn(149, new(17, 12)),
            new MonsterSpawn(149, new(22, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room18, new MonsterSpawn(152, new(16, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room18, new MonsterSpawn(147, new(19, 20), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room18,
                        new MonsterSpawn(147, new(18, 8)),
                        new MonsterSpawn(147, new(10, 18), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room19, new(1, 15), _room20, new(28, 15), locked: false);
        r.SpawnPortal(_room19, new(28, 15), _room18, new(1, 15), locked: false);
        r.SpawnPortal(_room20, new(1, 15), _room21, new(28, 15), locked: false);
        r.SpawnPortal(_room20, new(28, 15), _room19, new(1, 15), locked: false);
        r.Summon(_room20,
            new MonsterSpawn(138, new(10, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room20,
                new MonsterSpawn(73, new(18, 9), OnDeath: r =>
                {
                    r.Summon(_room20, new MonsterSpawn(61, new(13, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room20, new MonsterSpawn(69, new(16, 19), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room20,
                            new MonsterSpawn(69, new(20, 14), IsBonus: true),
                            new MonsterSpawn(69, new(13, 11), IsBonus: true));
                        }));
                    }));
                }),
                new MonsterSpawn(73, new(9, 9), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room20, new MonsterSpawn(61, new(8, 16)));
                }));
            }),
            new MonsterSpawn(138, new(25, 15)),
            new MonsterSpawn(138, new(23, 20)));
        r.SpawnPortal(_room21, new(1, 15), _room22, new(28, 15), locked: false);
        r.SpawnPortal(_room21, new(28, 15), _room20, new(1, 15), locked: false);
        r.Summon(_room21,
            new MonsterSpawn(85, new(15, 22), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room21, new MonsterSpawn(62, new(15, 20), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room21, new MonsterSpawn(75, new(13, 12), IsBonus: true));
                }));
            }),
            new MonsterSpawn(85, new(18, 9), OnDeath: r =>
            {
                r.Summon(_room21, new MonsterSpawn(85, new(9, 11), OnDeath: r =>
                {
                    r.Summon(_room21,
                    new MonsterSpawn(62, new(19, 14)),
                    new MonsterSpawn(62, new(8, 16)));
                }));
            }),
            new MonsterSpawn(85, new(18, 5)));
        r.SpawnExit(_room22, new(1, 15));
        r.SpawnPortal(_room22, new(28, 15), _room21, new(1, 15));
        r.Summon(_room22,
            new MonsterSpawn(149, new(12, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(152, new(16, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room22, new MonsterSpawn(147, new(19, 20), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room22,
                        new MonsterSpawn(147, new(18, 8), IsBonus: true),
                        new MonsterSpawn(147, new(10, 18)));
                    }));
                }));
            }),
            new MonsterSpawn(149, new(17, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(152, new(12, 16)));
            }),
            new MonsterSpawn(149, new(22, 12)));
        r.SummonNpcs(_room22, new NpcSpawn(319, new(20, 16)));
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
            r.Dialog(_room2, 6156);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6157);
        }
        else if (room == _room14)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room15)
        {
            r.StopRoomClock(_room15);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room15, _room16);
            r.StartRoomClock(_room15, TimeSpan.FromSeconds(40));
        }
        else if (room == _room18)
        {
            r.Dialog(_room18, 6158);
        }
        else if (room == _room22)
        {
            r.Dialog(_room22, 6159);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room8)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
        }
        else if (room == _room18)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room18, _room19);
            r.UnlockPortal(_room18, _room17);
        }
        else if (room == _room22)
        {
            r.Dialog(_room22, 6160);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockExit(_room22);
            r.UnlockPortal(_room22, _room21);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room6 && button == 1)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room7);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace37();
