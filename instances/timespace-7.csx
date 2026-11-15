public class TimeSpace7 : InstanceScript
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

    public TimeSpace7()
    {
        Definition = InstanceDefinitionBuilder.Named(7, "The Demon Sculpture", "The Demon Sculpture")
            .ForLevels(16, 99)
            .StartingAt(10, 10)
            .Requiring(1012, 1)
            .WithDraw(7, 1)
            .WithDraw(1073, 1)
            .WithSpecialReward(1015, 2)
            .WithReward(2034, 1)
            .WithRoom(4100, out _room1, 0, 8)
            .WithRoom(4100, out _room2, 1, 8)
            .WithRoom(4101, out _room3, 1, 9)
            .WithRoom(4102, out _room4, 2, 9)
            .WithRoom(4103, out _room5, 3, 9)
            .WithRoom(4106, out _room6, 4, 9)
            .WithRoom(4106, out _room7, 4, 8)
            .WithRoom(4113, out _room8, 5, 8)
            .WithRoom(4113, out _room9, 4, 10)
            .WithRoom(4106, out _room10, 5, 10)
            .WithRoom(4100, out _room11, 5, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 14), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(63, new(8, 14), IsBonus: true),
            new MonsterSpawn(98, new(7, 16)));
        r.SpawnPortal(_room4, new(28, 14), _room5, new(1, 15), locked: false);
        r.SpawnPortal(_room4, new(1, 14), _room3, new(28, 15), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(55, new(14, 8), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(55, new(25, 16)));
            }),
            new MonsterSpawn(55, new(10, 19), IsBonus: true),
            new MonsterSpawn(55, new(10, 9)),
            new MonsterSpawn(55, new(4, 17), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(55, new(20, 19)));
            }));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 14), locked: false);
        r.Summon(_room5, new MonsterSpawn(27, new(12, 16), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(63, new(10, 13), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(63, new(18, 14), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(63, new(22, 12)));
                    }));
                }));
            }));
        r.SpawnPortal(_room6, new(14, 1), _room7, new(14, 28), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room11, new(1, 15));
        r.SpawnPortal(_room6, new(14, 28), _room9, new(14, 1), locked: false);
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room8, new(1, 15));
        r.SpawnPortal(_room7, new(14, 28), _room6, new(14, 1));
        r.Summon(_room7,
            new MonsterSpawn(97, new(22, 17)),
            new MonsterSpawn(97, new(16, 16), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(97, new(8, 15), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(130, new(20, 17)));
                }));
            }));
        r.SpawnPortal(_room8, new(1, 15), _room7, new(28, 15));
        r.PlaceButton(_room8, 1, new(15, 17), 1055, 1056);
        r.SpawnPortal(_room9, new(14, 1), _room6, new(14, 28));
        r.SpawnPortal(_room9, new(28, 15), _room10, new(1, 15), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(63, new(21, 10)),
            new MonsterSpawn(63, new(10, 11), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(63, new(22, 17)));
            }));
        r.SpawnPortal(_room10, new(1, 15), _room9, new(28, 15));
        r.PlaceButton(_room10, 1, new(15, 15), 1055, 1056);
        r.SpawnExit(_room11, new(28, 15), locked: false);
        r.SpawnPortal(_room11, new(1, 15), _room6, new(28, 15), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6030);
        }
        else if (room == _room6)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room6, 6031);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6033);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6033);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room11, 6036);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room7)
        {
            r.UnlockPortal(_room7, _room8);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room7, _room6);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(50));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room6, _room11);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room8 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room8, 6034);
            r.Summon(_room8,
                new MonsterSpawn(27, new(14, 12), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(27, new(18, 15), OnDeath: r =>
                    {
                        r.Summon(_room8,
                        new MonsterSpawn(130, new(18, 19)),
                        new MonsterSpawn(130, new(23, 15)));
                    }));
                }),
                new MonsterSpawn(27, new(11, 15)));
        }
        else if (room == _room10 && button == 1)
        {
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room10, 6035);
            r.Summon(_room10,
                new MonsterSpawn(130, new(13, 17), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(130, new(20, 12)));
                }),
                new MonsterSpawn(130, new(10, 20)));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace7();
