public class TimeSpace17 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;
    private readonly int _room8;

    public TimeSpace17()
    {
        Definition = InstanceDefinitionBuilder.Named(17, "Becoming a Warrior", "Becoming a Warrior")
            .ForLevels(27, 99)
            .StartingAt(7, 9)
            .Requiring(1012, 2)
            .WithDraw(163, 1)
            .WithDraw(169, 1)
            .WithDraw(175, 1)
            .WithSpecialReward(2101, 40)
            .WithReward(2099, 2)
            .WithReward(1087, 1)
            .WithRoom(4203, out _room1, 0, 8)
            .WithRoom(4207, out _room2, 0, 9)
            .WithRoom(4213, out _room3, 1, 9)
            .WithRoom(4201, out _room4, 1, 10)
            .WithRoom(4211, out _room5, 2, 9)
            .WithRoom(4203, out _room6, 2, 8)
            .WithRoom(4209, out _room7, 3, 9)
            .WithRoom(4206, out _room8, 3, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(140));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room3, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room4, new(14, 1), _room3, new(14, 28));
        r.Summon(_room4,
            new MonsterSpawn(66, new(17, 13), IsBonus: true),
            new MonsterSpawn(66, new(9, 15)));
        r.SummonNpcs(_room4, new NpcSpawn(318, new(11, 15)));
        r.SpawnPortal(_room5, new(14, 1), _room6, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(29, 15), _room7, new(1, 15), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room3, new(28, 15), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(58, new(25, 13), IsBonus: true),
            new MonsterSpawn(58, new(20, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(58, new(12, 20)));
            }),
            new MonsterSpawn(58, new(14, 8), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(58, new(6, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room6, new(14, 28), _room5, new(14, 1));
        r.Summon(_room6, new MonsterSpawn(318, new(14, 12)));
        r.SpawnPortal(_room7, new(14, 28), _room8, new(14, 1), locked: false);
        r.SpawnPortal(_room7, new(1, 15), _room5, new(29, 15), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(58, new(21, 13), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(66, new(16, 12)));
            }),
            new MonsterSpawn(58, new(15, 15), OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(58, new(7, 14)),
                new MonsterSpawn(58, new(10, 10)));
            }),
            new MonsterSpawn(58, new(12, 19), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(66, new(19, 18), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(66, new(12, 12)));
                }));
            }));
        r.SpawnPortal(_room8, new(14, 1), _room7, new(14, 28), locked: false);
        r.SpawnExit(_room8, new(14, 28));
        r.Summon(_room8, new MonsterSpawn(318, new(12, 16)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6083);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6085);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6087);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.UnlockPortal(_room4, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6084);
            r.UnlockPortal(_room3, _room5);
        }
        else if (room == _room7)
        {
            r.UnlockExit(_room8);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace17();
