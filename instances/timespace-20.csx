public class TimeSpace20 : InstanceScript
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

    public TimeSpace20()
    {
        Definition = InstanceDefinitionBuilder.Named(20, "Guards in Danger", "Guards in Danger")
            .ForLevels(30, 99)
            .StartingAt(11, 13)
            .Requiring(1012, 3)
            .WithDraw(2047, 3)
            .WithDraw(2039, 3)
            .WithDraw(235, 1)
            .WithSpecialReward(1015, 2)
            .WithReward(2131, 2)
            .WithReward(1087, 1)
            .WithRoom(4201, out _room1, 3, 11)
            .WithRoom(4212, out _room2, 3, 10)
            .WithRoom(4200, out _room3, 3, 9)
            .WithRoom(4207, out _room4, 2, 9)
            .WithRoom(4206, out _room5, 2, 8)
            .WithRoom(4210, out _room6, 4, 9)
            .WithRoom(4212, out _room7, 4, 8)
            .WithRoom(4203, out _room8, 4, 7)
            .WithRoom(4209, out _room9, 5, 8)
            .WithRoom(4206, out _room10, 3, 8)
            .WithRoom(4203, out _room11, 2, 7)
            .WithRoom(4201, out _room12, 5, 9)
            .WithRoom(4203, out _room13, 3, 7)
            .WithRoom(5100, out _room14, 4, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(150));
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room14, new(1, 15));
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room10, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room6, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(1, 15), _room4, new(28, 15), locked: false);
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28), locked: false);
        r.SpawnPortal(_room4, new(28, 15), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room5, new(14, 1), _room11, new(14, 28));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(141, new(18, 16), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(100, new(23, 16)));
            }),
            new MonsterSpawn(100, new(13, 11)),
            new MonsterSpawn(100, new(11, 13), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(141, new(12, 18)));
            }));
        r.SpawnPortal(_room6, new(15, 1), _room7, new(14, 28), locked: false);
        r.SpawnPortal(_room6, new(1, 15), _room3, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(14, 1), _room8, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room7, new(14, 28), _room6, new(15, 1), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room7, new(14, 1));
        r.PlaceButton(_room8, 1, new(17, 15), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room8,
            new MonsterSpawn(100, new(22, 17), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(100, new(9, 14), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(141, new(7, 18)));
                }));
            }),
            new MonsterSpawn(100, new(22, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(141, new(14, 18), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(141, new(24, 18), OnDeath: r =>
                    {
                        r.Summon(_room8, new MonsterSpawn(141, new(17, 18), OnDeath: r =>
                        {
                            r.Summon(_room8, new MonsterSpawn(65, new(16, 12), IsBonus: true));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room9, new(14, 28), _room12, new(14, 1));
        r.SpawnPortal(_room9, new(1, 15), _room7, new(28, 15), locked: false);
        r.SpawnPortal(_room10, new(14, 1), _room13, new(14, 28));
        r.SpawnPortal(_room10, new(14, 28), _room3, new(14, 1), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(141, new(20, 16), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(141, new(24, 16)));
            }),
            new MonsterSpawn(141, new(16, 13), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(141, new(12, 20)));
            }),
            new MonsterSpawn(141, new(10, 12)));
        r.SpawnPortal(_room11, new(14, 28), _room5, new(14, 1));
        r.PlaceButton(_room11, 1, new(15, 17), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room11,
            new MonsterSpawn(100, new(5, 16)),
            new MonsterSpawn(100, new(6, 16), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(100, new(8, 11), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(141, new(19, 17), OnDeath: r =>
                    {
                        r.Summon(_room11,
                        new MonsterSpawn(100, new(20, 11), OnDeath: r =>
                        {
                            r.Summon(_room11, new MonsterSpawn(65, new(11, 18)));
                        }),
                        new MonsterSpawn(100, new(23, 13)));
                    }));
                }),
                new MonsterSpawn(141, new(11, 14), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(141, new(16, 13), OnDeath: r =>
                    {
                        r.Summon(_room11, new MonsterSpawn(100, new(22, 17)));
                    }));
                }));
            }));
        r.SpawnPortal(_room12, new(14, 1), _room9, new(14, 28));
        r.PlaceButton(_room12, 1, new(16, 15), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room12,
            new MonsterSpawn(58, new(19, 13)),
            new MonsterSpawn(58, new(18, 20)),
            new MonsterSpawn(58, new(15, 19)),
            new MonsterSpawn(58, new(14, 11), IsBonus: true),
            new MonsterSpawn(58, new(11, 19), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(65, new(7, 15)),
                new MonsterSpawn(65, new(12, 16)));
            }),
            new MonsterSpawn(58, new(9, 11)));
        r.SpawnPortal(_room13, new(14, 28), _room10, new(14, 1));
        r.PlaceButton(_room13, 1, new(14, 17), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room13,
            new MonsterSpawn(56, new(21, 16)),
            new MonsterSpawn(56, new(15, 19), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(56, new(22, 13), OnDeath: r =>
                {
                    r.Summon(_room13,
                    new MonsterSpawn(65, new(6, 16)),
                    new MonsterSpawn(56, new(9, 19), OnDeath: r =>
                    {
                        r.Summon(_room13, new MonsterSpawn(65, new(23, 18)));
                    }));
                }));
            }),
            new MonsterSpawn(56, new(10, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(56, new(4, 15)));
            }),
            new MonsterSpawn(56, new(9, 13)));
        r.SpawnExit(_room14, new(28, 14));
        r.SpawnPortal(_room14, new(1, 15), _room2, new(28, 15));
        r.Summon(_room14,
            new MonsterSpawn(58, new(23, 16)),
            new MonsterSpawn(58, new(20, 9)),
            new MonsterSpawn(58, new(21, 20)),
            new MonsterSpawn(58, new(19, 9)),
            new MonsterSpawn(58, new(16, 22)),
            new MonsterSpawn(58, new(13, 22), IsBonus: true),
            new MonsterSpawn(58, new(13, 9), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room14,
                new MonsterSpawn(141, new(8, 13)),
                new MonsterSpawn(141, new(10, 7)),
                new MonsterSpawn(141, new(10, 20), OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(59, new(17, 14), IsBonus: true));
                }));
            }));
        r.SummonNpcs(_room14, new NpcSpawn(320, new(18, 17), IsEscort: true), new NpcSpawn(320, new(15, 11), IsEscort: true), new NpcSpawn(320, new(16, 18), IsEscort: true), new NpcSpawn(320, new(12, 16), IsEscort: true), new NpcSpawn(320, new(12, 12), IsEscort: true));
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
            r.Dialog(_room2, 6104);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room8)
        {
            r.UnlockPortal(_room8, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room8, 6105);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room5, _room11);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room11, 6106);
            r.UnlockPortal(_room9, _room12);
        }
        else if (room == _room12)
        {
            r.UnlockPortal(_room12, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room12, 6107);
        }
        else if (room == _room13)
        {
            r.UnlockPortal(_room13, _room10);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room13, 6109);
        }
        else if (room == _room14)
        {
            r.UnlockExit(_room14);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room14, 6110);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room8 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room11 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room12 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room10, _room13);
        }
        else if (room == _room13 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room2, _room14);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace20();
