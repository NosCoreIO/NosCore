public class TimeSpace13 : InstanceScript
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

    public TimeSpace13()
    {
        Definition = InstanceDefinitionBuilder.Named(13, "A Bold Warrior: Part 2", "A Bold Warrior: Part 2")
            .ForLevels(23, 99)
            .StartingAt(8, 9)
            .Requiring(1012, 2)
            .WithDraw(223, 1)
            .WithDraw(230, 1)
            .WithDraw(231, 1)
            .WithDraw(232, 1)
            .WithDraw(233, 1)
            .WithSpecialReward(1032, 2)
            .WithReward(507, 1)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 0, 9)
            .WithRoom(4101, out _room2, 1, 9)
            .WithRoom(4102, out _room3, 2, 9)
            .WithRoom(4103, out _room4, 3, 9)
            .WithRoom(4111, out _room5, 4, 9)
            .WithRoom(4106, out _room6, 3, 8)
            .WithRoom(4112, out _room7, 4, 8)
            .WithRoom(4107, out _room8, 2, 8)
            .WithRoom(4108, out _room9, 2, 7)
            .WithRoom(4110, out _room10, 2, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(200));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(56, new(20, 17), IsBonus: true),
            new MonsterSpawn(56, new(17, 10)),
            new MonsterSpawn(56, new(6, 14)));
        r.SpawnPortal(_room4, new(14, 1), _room6, new(14, 28));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 14));
        r.Summon(_room4,
            new MonsterSpawn(15, new(16, 14), IsBonus: true),
            new MonsterSpawn(58, new(13, 17)),
            new MonsterSpawn(15, new(10, 12)));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15), locked: false);
        r.SpawnPortal(_room6, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room6, new(1, 15), _room8, new(28, 15));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15));
        r.Summon(_room7,
            new MonsterSpawn(136, new(19, 22), IsBonus: true),
            new MonsterSpawn(136, new(15, 12)),
            new MonsterSpawn(15, new(8, 14)));
        r.SpawnPortal(_room8, new(14, 1), _room9, new(14, 28), locked: false);
        r.SpawnPortal(_room8, new(28, 15), _room6, new(1, 15), locked: false);
        r.SummonNpcs(_room8, new NpcSpawn(336, new(14, 16)));
        r.SpawnPortal(_room9, new(14, 5), _room10, new(14, 28));
        r.SpawnPortal(_room9, new(14, 28), _room8, new(14, 1), locked: false);
        r.PlaceButton(_room9, 1, new(14, 15), 1000, 1045);
        r.SpawnPortal(_room10, new(14, 28), _room9, new(14, 5), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(57, new(22, 14)),
            new MonsterSpawn(57, new(10, 9), OnDeath: r =>
            {
                r.Summon(_room10,
                new MonsterSpawn(57, new(10, 19), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(58, new(20, 15)));
                }),
                new MonsterSpawn(57, new(14, 8), OnDeath: r =>
                {
                    r.Summon(_room10, new MonsterSpawn(58, new(9, 16)));
                }));
            }),
            new MonsterSpawn(57, new(8, 13)));
        r.SummonNpcs(_room10, new NpcSpawn(317, new(14, 14)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6070);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6071);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6072);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6073);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room6);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(70));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room6, _room8);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room10, 6074);
            r.Succeed();
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room9 && button == 1)
        {
            r.UnlockPortal(_room9, _room10);
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace13();
