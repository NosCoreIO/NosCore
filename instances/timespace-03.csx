public class TimeSpace3 : InstanceScript
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

    // Both keys of a room land on one of these pairs of spots, picked per run. The first key of
    // a pair is the one that opens the way on.
    private static readonly (Spot, Spot)[] FirstKeySpots = [(new(20, 11), new(24, 14)), (new(19, 6), new(13, 19)), (new(22, 8), new(23, 14))];
    private static readonly (Spot, Spot)[] LastKeySpots = [(new(26, 16), new(3, 19)), (new(18, 27), new(13, 11)), (new(4, 21), new(12, 4))];

    public TimeSpace3()
    {
        Definition = InstanceDefinitionBuilder.Named(3, "A Difficult Decision", "A Difficult Decision")
            .ForLevels(10, 99)
            .StartingAt(9, 11)
            .Requiring(1012, 1)
            .WithDraw(205, 1)
            .WithDraw(207, 1)
            .WithDraw(208, 1)
            .WithDraw(209, 1)
            .WithDraw(210, 1)
            .WithSpecialReward(203, 1)
            .WithReward(10, 1)
            .WithRoom(4100, out _room1, 3, 7)
            .WithRoom(4113, out _room2, 3, 8)
            .WithRoom(4113, out _room3, 4, 8)
            .WithRoom(4106, out _room4, 4, 9)
            .WithRoom(4106, out _room5, 3, 9)
            .WithRoom(4104, out _room6, 2, 9)
            .WithRoom(4110, out _room7, 2, 10)
            .WithRoom(4110, out _room8, 1, 10)
            .WithRoom(4103, out _room9, 1, 9)
            .WithRoom(4104, out _room10, 2, 8)
            .WithRoom(4103, out _room11, 1, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(400));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15));
        r.SpawnPortal(_room2, new(1, 15), _room10, new(28, 15));
        var (eastKey, westKey) = FirstKeySpots[Random.Shared.Next(FirstKeySpots.Length)];
        r.PlaceButton(_room2, 1, eastKey, 1057, 1057, kind: ButtonKind.Key);
        r.PlaceButton(_room2, 2, westKey, 1057, 1057, kind: ButtonKind.Key);
        r.SpawnPortal(_room3, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.SpawnPortal(_room4, new(14, 1), _room3, new(14, 28));
        r.SpawnPortal(_room4, new(1, 15), _room5, new(28, 15));
        r.Summon(_room4,
            new MonsterSpawn(54, new(13, 9), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(54, new(12, 20)));
            }),
            new MonsterSpawn(54, new(11, 10), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(54, new(8, 14)));
            }));
        r.SpawnPortal(_room5, new(28, 15), _room4, new(1, 15), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room6, new(14, 28), _room7, new(14, 1));
        r.Summon(_room6,
            new MonsterSpawn(9, new(23, 18)),
            new MonsterSpawn(54, new(13, 10), IsBonus: true),
            new MonsterSpawn(9, new(12, 13), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(9, new(18, 12)),
                new MonsterSpawn(54, new(19, 10)));
            }));
        r.SpawnPortal(_room7, new(14, 1), _room6, new(14, 28));
        r.SpawnPortal(_room7, new(1, 15), _room8, new(28, 15));
        r.Summon(_room7,
            new MonsterSpawn(54, new(12, 10)),
            new MonsterSpawn(54, new(8, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(54, new(19, 17), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(54, new(19, 11), OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(129, new(10, 12), OnDeath: r =>
                        {
                            r.Summon(_room7, new MonsterSpawn(129, new(11, 18)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room8, new(14, 1), _room9, new(14, 28));
        r.SpawnPortal(_room8, new(28, 15), _room7, new(1, 15));
        r.Summon(_room8,
            new MonsterSpawn(54, new(21, 20), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(129, new(18, 19), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(54, new(18, 23)));
                }));
            }),
            new MonsterSpawn(54, new(9, 15), IsBonus: true));
        r.SpawnPortal(_room9, new(14, 1), _room11, new(14, 28));
        r.SpawnPortal(_room9, new(14, 28), _room8, new(14, 1));
        r.SpawnExit(_room9, new(1, 15));
        var (exitKey, trapKey) = LastKeySpots[Random.Shared.Next(LastKeySpots.Length)];
        r.PlaceButton(_room9, 1, exitKey, 1057, 1057, kind: ButtonKind.Key);
        r.PlaceButton(_room9, 2, trapKey, 1057, 1057, kind: ButtonKind.Key);
        r.SpawnPortal(_room10, new(28, 15), _room2, new(1, 15));
        r.SpawnPortal(_room10, new(1, 15), _room11, new(28, 15));
        r.Summon(_room10, new MonsterSpawn(54, new(18, 9), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(54, new(20, 20), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(96, new(9, 19), OnDeath: r =>
                    {
                        r.Summon(_room10, new MonsterSpawn(96, new(10, 11), OnDeath: r =>
                        {
                            r.Summon(_room10, new MonsterSpawn(129, new(10, 16)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room11, new(28, 15), _room10, new(1, 15));
        r.SpawnPortal(_room11, new(14, 28), _room9, new(14, 1));
        r.Summon(_room11,
            new MonsterSpawn(129, new(17, 19), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(129, new(20, 21), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(129, new(16, 11)));
                }));
            }),
            new MonsterSpawn(129, new(6, 18)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6012);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(70));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room8);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room9)
        {
            r.UnlockExit(_room9);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room11);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room2 && button == 1)
        {
            r.RemoveButton(_room2, 2);
            r.UnlockPortal(_room2, _room3);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.FindKey);
        }
        else if (room == _room2 && button == 2)
        {
            r.RemoveButton(_room2, 1);
            r.UnlockPortal(_room2, _room10);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.FindKey);
        }
        else if (room == _room9 && button == 1)
        {
            r.RemoveButton(_room9, 2);
            r.UnlockExit(_room9);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.FindKey);
        }
        else if (room == _room9 && button == 2)
        {
            r.RemoveButton(_room9, 1);
            r.Message(Game18NConstString.DoorOpenedSomewhere);
            r.Message(Game18NConstString.FindKey);
            r.Summon(_room9,
                new MonsterSpawn(54, new(17, 11)),
                new MonsterSpawn(54, new(13, 19), OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(129, new(21, 19), OnDeath: r =>
                    {
                        r.Summon(_room9, new MonsterSpawn(54, new(13, 10), OnDeath: r =>
                        {
                            r.Summon(_room9, new MonsterSpawn(129, new(15, 22)));
                        }));
                    }));
                }));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace3();
