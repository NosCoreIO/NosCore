// The Maze of Pain
public class TimeSpace60 : InstanceScript
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

    public TimeSpace60()
    {
        Definition = InstanceDefinitionBuilder.Named(60, "You're finally here. Now the real adventure begins. This time-space is called the Maze of Pain; they say nobody has ever come back from it. Princess Catlotte is held prisoner here, so you absolutely must go. We believe in you!", "The Maze of Pain")
            .ForLevels(64, 99)
            .StartingAt(9, 6)
            .WithLives(1)
            .Requiring(1012, 6)
            .WithSpecialReward(216, 1)
            .WithReward(1013, 1)
            .WithReward(1087, 1)
            .WithRoom(4715, out _room1, 0, 5)
            .WithRoom(4705, out _room2, 0, 5)
            .WithRoom(4704, out _room3, 1, 5)
            .WithRoom(4705, out _room4, 2, 5)
            .WithRoom(4709, out _room5, 3, 5)
            .WithRoom(4707, out _room6, 4, 5)
            .WithRoom(4702, out _room7, 4, 6)
            .WithRoom(4702, out _room8, 4, 7)
            .WithRoom(4711, out _room9, 3, 7)
            .WithRoom(4707, out _room10, 2, 7)
            .WithRoom(4704, out _room11, 2, 6)
            .WithRoom(4702, out _room12, 3, 6)
            .WithRoom(2105, out _room13, 5, 7)
            .WithRoom(4707, out _room14, 6, 7)
            .WithRoom(4707, out _room15, 6, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(17, 9), _room2, new(0, 9), locked: false);
        r.SpawnPortal(_room2, new(0, 9), _room1, new(17, 9));
        r.SpawnPortal(_room2, new(17, 9), _room3, new(0, 9));
        r.PlaceButton(_room2, 1, new(10, 7), 1057, 1057);
        r.PlaceButton(_room2, 2, new(10, 5), 1057, 1057);
        r.PlaceButton(_room2, 3, new(10, 9), 1057, 1057);
        r.PlaceButton(_room2, 4, new(10, 11), 1057, 1057);
        r.SpawnPortal(_room3, new(0, 9), _room2, new(17, 9), locked: false);
        r.SpawnPortal(_room3, new(17, 9), _room4, new(0, 9), locked: false);
        r.SpawnPortal(_room4, new(0, 9), _room3, new(17, 9), locked: false);
        r.SpawnPortal(_room4, new(17, 9), _room5, new(0, 9), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(246, new(5, 10), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(237, new(12, 9), IsBonus: true),
                new MonsterSpawn(237, new(5, 9), IsBonus: true));
            }),
            new MonsterSpawn(261, new(12, 12)));
        r.SpawnPortal(_room5, new(0, 9), _room4, new(17, 9), locked: false);
        r.SpawnPortal(_room5, new(8, 17), _room6, new(8, 0), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(225, new(2, 7)),
            new MonsterSpawn(225, new(5, 11)),
            new MonsterSpawn(225, new(7, 9)),
            new MonsterSpawn(225, new(8, 12)),
            new MonsterSpawn(225, new(9, 15), IsBonus: true));
        r.SpawnPortal(_room6, new(8, 17), _room7, new(8, 0), locked: false);
        r.SpawnPortal(_room6, new(8, 0), _room5, new(8, 17), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(258, new(5, 12)),
            new MonsterSpawn(258, new(5, 13)),
            new MonsterSpawn(258, new(5, 14), IsBonus: true),
            new MonsterSpawn(258, new(6, 12)),
            new MonsterSpawn(258, new(6, 13)),
            new MonsterSpawn(258, new(6, 14)),
            new MonsterSpawn(243, new(8, 6)),
            new MonsterSpawn(243, new(9, 2)),
            new MonsterSpawn(243, new(11, 9)),
            new MonsterSpawn(243, new(9, 15)));
        r.SpawnPortal(_room7, new(0, 9), _room8, new(17, 9), locked: false);
        r.SpawnPortal(_room7, new(8, 0), _room6, new(8, 17), locked: false);
        r.SpawnPortal(_room8, new(0, 9), _room9, new(17, 9), locked: false);
        r.SpawnPortal(_room8, new(17, 9), _room7, new(0, 9), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(246, new(4, 9), IsBonus: true),
            new MonsterSpawn(246, new(10, 4)),
            new MonsterSpawn(246, new(14, 10)));
        r.SpawnPortal(_room9, new(17, 9), _room8, new(0, 9), locked: false);
        r.SpawnPortal(_room9, new(8, 0), _room10, new(8, 17), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(237, new(6, 3)),
            new MonsterSpawn(258, new(8, 8), IsBonus: true),
            new MonsterSpawn(237, new(9, 3), OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(237, new(15, 8)),
                new MonsterSpawn(237, new(13, 6)));
            }),
            new MonsterSpawn(258, new(10, 10)),
            new MonsterSpawn(237, new(11, 3), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(237, new(15, 10)));
            }));
        r.SpawnPortal(_room10, new(8, 17), _room9, new(8, 0), locked: false);
        r.SpawnPortal(_room10, new(8, 0), _room11, new(8, 17), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(234, new(6, 4)),
            new MonsterSpawn(234, new(6, 15)),
            new MonsterSpawn(234, new(7, 2), IsBonus: true),
            new MonsterSpawn(234, new(10, 9)),
            new MonsterSpawn(234, new(10, 15)),
            new MonsterSpawn(234, new(11, 12)));
        r.SpawnPortal(_room11, new(8, 17), _room11, new(8, 17), locked: false);
        r.SpawnPortal(_room12, new(17, 9), _room12, new(17, 9), locked: false);
        r.SpawnPortal(_room12, new(8, 0), _room11, new(8, 0), locked: false);
        r.SpawnPortal(_room13, new(7, 25), _room12, new(41, 0));
        r.SpawnPortal(_room13, new(24, 43), _room14, new(8, 0));
        r.Summon(_room13,
            new MonsterSpawn(214, new(21, 14), OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(218, new(22, 29), OnDeath: r =>
                {
                    r.Summon(_room13, new MonsterSpawn(218, new(25, 25)));
                }),
                new MonsterSpawn(218, new(21, 34)));
            }),
            new MonsterSpawn(214, new(19, 25), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(214, new(29, 43)));
            }),
            new MonsterSpawn(214, new(10, 10), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(218, new(36, 23)));
            }),
            new MonsterSpawn(221, new(34, 34), IsBonus: true, OnDeath: r =>
            {
                r.AddClockTime(TimeSpan.FromSeconds(200));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room13, _room14);
            }),
            new MonsterSpawn(214, new(18, 34)),
            new MonsterSpawn(214, new(23, 39)),
            new MonsterSpawn(214, new(25, 27)),
            new MonsterSpawn(214, new(29, 19)),
            new MonsterSpawn(214, new(40, 29)));
        r.SpawnPortal(_room14, new(8, 17), _room15, new(8, 0));
        r.SpawnPortal(_room14, new(8, 0), _room13, new(24, 43));
        r.Summon(_room14,
            new MonsterSpawn(262, new(12, 11), IsBonus: true),
            new MonsterSpawn(262, new(11, 15), IsBonus: true),
            new MonsterSpawn(262, new(11, 8), IsBonus: true),
            new MonsterSpawn(262, new(9, 14)),
            new MonsterSpawn(262, new(9, 10)),
            new MonsterSpawn(262, new(9, 4)),
            new MonsterSpawn(262, new(6, 13), IsBonus: true),
            new MonsterSpawn(262, new(6, 5)),
            new MonsterSpawn(261, new(11, 13)),
            new MonsterSpawn(261, new(10, 6)),
            new MonsterSpawn(261, new(7, 14), IsBonus: true),
            new MonsterSpawn(261, new(7, 4), IsBonus: true),
            new MonsterSpawn(243, new(9, 12)),
            new MonsterSpawn(243, new(9, 6), IsBonus: true),
            new MonsterSpawn(241, new(10, 9), IsBonus: true));
        r.SpawnExit(_room15, new(8, 17), locked: false);
        r.SpawnPortal(_room15, new(8, 0), _room14, new(8, 17), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(-0.1));
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room3, 6300);
        }
        else if (room == _room13)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room13, 6302);
        }
        else if (room == _room14)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room14, 6303);
            r.StartRoomClock(_room14, TimeSpan.FromSeconds(234));
        }
        else if (room == _room15)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room15, 6304);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room2, _room3);
        }
        else if (room == _room14)
        {
            r.StopRoomClock(_room14);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room14, _room15);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room2 && button == 1)
        {
            r.Message(Game18NConstString.NewGoal);
            r.AddClockTime(TimeSpan.FromSeconds(-0.1));
            r.Summon(_room2,
                        new MonsterSpawn(246, new(0, 12)),
                        new MonsterSpawn(246, new(2, 12)),
                        new MonsterSpawn(246, new(3, 8)),
                        new MonsterSpawn(246, new(4, 12)),
                        new MonsterSpawn(246, new(6, 8), IsBonus: true),
                        new MonsterSpawn(246, new(6, 12)),
                        new MonsterSpawn(246, new(8, 12), IsBonus: true),
                        new MonsterSpawn(246, new(9, 8)),
                        new MonsterSpawn(246, new(12, 12), IsBonus: true),
                        new MonsterSpawn(246, new(13, 8)),
                        new MonsterSpawn(246, new(15, 8)),
                        new MonsterSpawn(246, new(15, 12)));
            r.Message(Game18NConstString.DoorOpenedSomewhere);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace60();
