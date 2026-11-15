// The Pursuit
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
    private readonly int _room18;

    public TimeSpace30()
    {
        Definition = InstanceDefinitionBuilder.Named(30, "The Koaren camps at the Fernon Entrance are occupied and the Koarens can no longer flee. Is all this down to the Kenkos' betrayal? Lord Cuarry has promised a fine reward for capturing the Koarens, and many adventurers have gone to the Fernon Entrance to hunt them.", "The Pursuit")
            .ForLevels(40, 99)
            .StartingAt(15, 12)
            .Requiring(1012, 4)
            .WithDraw(74, 1)
            .WithDraw(82, 1)
            .WithDraw(90, 1)
            .WithSpecialReward(217, 1)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 0, 7)
            .WithRoom(4100, out _room2, 0, 7)
            .WithRoom(4101, out _room3, 0, 8)
            .WithRoom(4102, out _room4, 1, 8)
            .WithRoom(4102, out _room5, 1, 7)
            .WithRoom(4103, out _room6, 1, 6)
            .WithRoom(4106, out _room7, 2, 6)
            .WithRoom(4109, out _room8, 3, 6)
            .WithRoom(4106, out _room9, 4, 6)
            .WithRoom(4106, out _room10, 2, 7)
            .WithRoom(4110, out _room11, 3, 7)
            .WithRoom(4106, out _room12, 4, 7)
            .WithRoom(4107, out _room13, 2, 8)
            .WithRoom(4111, out _room14, 3, 8)
            .WithRoom(4107, out _room15, 4, 8)
            .WithRoom(4105, out _room16, 3, 9)
            .WithRoom(4103, out _room17, 2, 9)
            .WithRoom(4105, out _room18, 1, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(67, new(6, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(67, new(11, 22), IsBonus: true));
            }),
            new MonsterSpawn(67, new(16, 12), IsBonus: true),
            new MonsterSpawn(67, new(14, 20), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(67, new(9, 11), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(67, new(20, 10)));
                }));
            }),
            new MonsterSpawn(67, new(20, 14), IsBonus: true));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(138, new(11, 20)),
            new MonsterSpawn(82, new(11, 12), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(138, new(12, 10), IsBonus: true));
            }),
            new MonsterSpawn(82, new(18, 14), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(82, new(17, 16), IsBonus: true));
            }),
            new MonsterSpawn(138, new(22, 14)));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(28, 14), _room6, new(1, 15), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(17, new(7, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(67, new(20, 17)));
            }),
            new MonsterSpawn(17, new(19, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(67, new(16, 17), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(138, new(18, 12), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(138, new(8, 12), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(67, new(12, 18), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(138, new(13, 10), IsBonus: true));
                }));
            }),
            new MonsterSpawn(17, new(24, 14)));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 14), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(82, new(10, 14)),
            new MonsterSpawn(82, new(16, 21), IsBonus: true),
            new MonsterSpawn(60, new(20, 5), IsBonus: true),
            new MonsterSpawn(60, new(14, 8)),
            new MonsterSpawn(82, new(23, 13), IsBonus: true));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room8, new(1, 15), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(142, new(13, 7)),
            new MonsterSpawn(142, new(13, 13), IsBonus: true),
            new MonsterSpawn(142, new(10, 24), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(60, new(19, 8), IsBonus: true));
            }),
            new MonsterSpawn(142, new(15, 15), OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(67, new(9, 19), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(60, new(22, 17), IsBonus: true));
                }),
                new MonsterSpawn(67, new(9, 15)),
                new MonsterSpawn(67, new(9, 12), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(60, new(16, 22), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(60, new(22, 12)));
                    }));
                }));
            }));
        r.SpawnPortal(_room8, new(1, 15), _room9, new(28, 15));
        r.PlaceButton(_room8, 1, new(15, 15), 1057, 1057);
        r.SpawnPortal(_room9, new(1, 15), _room9, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(28, 15), _room8, new(1, 15), locked: false);
        r.SpawnPortal(_room10, new(1, 15), _room9, new(28, 0), locked: false);
        r.SpawnPortal(_room10, new(28, 15), _room11, new(1, 15), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(137, new(15, 12)),
            new MonsterSpawn(137, new(12, 4)),
            new MonsterSpawn(17, new(2, 15)),
            new MonsterSpawn(17, new(14, 14)),
            new MonsterSpawn(137, new(15, 5)),
            new MonsterSpawn(137, new(25, 22), IsBonus: true),
            new MonsterSpawn(17, new(17, 19)),
            new MonsterSpawn(17, new(25, 17), IsBonus: true));
        r.SpawnPortal(_room11, new(1, 15), _room12, new(28, 15));
        r.Summon(_room11,
            new MonsterSpawn(23, new(8, 5)),
            new MonsterSpawn(23, new(6, 23), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(67, new(18, 20)),
                new MonsterSpawn(67, new(16, 23), IsBonus: true));
            }),
            new MonsterSpawn(23, new(9, 7)),
            new MonsterSpawn(23, new(9, 21)),
            new MonsterSpawn(23, new(12, 18), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(143, new(12, 12), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(60, new(23, 16)));
                }));
            }),
            new MonsterSpawn(23, new(13, 9)),
            new MonsterSpawn(67, new(15, 5), IsBonus: true),
            new MonsterSpawn(23, new(15, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(143, new(11, 14), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(60, new(23, 13)));
                }));
            }),
            new MonsterSpawn(23, new(16, 12), IsBonus: true),
            new MonsterSpawn(67, new(18, 8)),
            new MonsterSpawn(67, new(17, 16), IsBonus: true));
        r.SpawnPortal(_room12, new(1, 15), _room12, new(1, 15), locked: false);
        r.SpawnPortal(_room12, new(28, 15), _room11, new(1, 15), locked: false);
        r.SpawnPortal(_room13, new(1, 14), _room12, new(28, 0), locked: false);
        r.SpawnPortal(_room13, new(28, 15), _room14, new(1, 15), locked: false);
        r.Summon(_room13,
            new MonsterSpawn(82, new(16, 13)),
            new MonsterSpawn(82, new(19, 22)),
            new MonsterSpawn(17, new(3, 14)),
            new MonsterSpawn(17, new(17, 22)),
            new MonsterSpawn(17, new(22, 22), IsBonus: true),
            new MonsterSpawn(17, new(20, 7)),
            new MonsterSpawn(17, new(24, 8)));
        r.SpawnPortal(_room14, new(1, 15), _room15, new(28, 15));
        r.SpawnExit(_room14, new(28, 15));
        r.Summon(_room14,
            new MonsterSpawn(67, new(10, 14), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(82, new(17, 23)));
            }),
            new MonsterSpawn(67, new(14, 11), IsBonus: true),
            new MonsterSpawn(67, new(14, 18), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(82, new(18, 15), IsBonus: true));
            }),
            new MonsterSpawn(67, new(14, 20), OnDeath: r =>
            {
                r.Summon(_room14,
                new MonsterSpawn(82, new(18, 21), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14,
                    new MonsterSpawn(82, new(18, 11)),
                    new MonsterSpawn(82, new(17, 9), IsBonus: true));
                }),
                new MonsterSpawn(82, new(18, 18)));
            }));
        r.SpawnPortal(_room15, new(14, 28), _room15, new(14, 28), locked: false);
        r.SpawnPortal(_room15, new(28, 15), _room14, new(1, 15), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room17, new(28, 15), locked: false);
        r.SpawnPortal(_room16, new(28, 15), _room15, new(5, 0), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(67, new(19, 8)),
            new MonsterSpawn(67, new(15, 25)),
            new MonsterSpawn(23, new(15, 9), IsBonus: true),
            new MonsterSpawn(23, new(15, 13)),
            new MonsterSpawn(23, new(15, 18), IsBonus: true),
            new MonsterSpawn(23, new(15, 22)));
        r.SpawnPortal(_room17, new(28, 15), _room18, new(1, 14), locked: false);
        r.SummonNpcs(_room17, new NpcSpawn(122, new(15, 17)));
        r.SpawnPortal(_room18, new(1, 14), _room17, new(28, 15), locked: false);
        r.SpawnPortal(_room18, new(28, 15), _room18, new(28, 15), locked: false);
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
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room2, 6137);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.NewGoal);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
        }
        else if (room == _room13)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room17)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room17, 6138);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room11)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room12);
        }
        else if (room == _room14)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room14, _room15);
            r.UnlockExit(_room14);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace30();
