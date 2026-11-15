public class TimeSpace1 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace1()
    {
        Definition = InstanceDefinitionBuilder.Named(1, "The Time-Space", "The Time-Space")
            .ForLevels(5, 99)
            .StartingAt(15, 12)
            .WithDraw(2015, 2)
            .WithDraw(2023, 10)
            .WithSpecialReward(202, 1)
            .WithReward(3, 1)
            .WithRoom(4100, out _room1, 0, 8)
            .WithRoom(4101, out _room2, 1, 8)
            .WithRoom(4102, out _room3, 2, 8)
            .WithRoom(4103, out _room4, 3, 8)
            .WithRoom(4104, out _room5, 3, 9)
            .WithRoom(4105, out _room6, 2, 9)
            .WithRoom(4106, out _room7, 1, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15));
        r.Summon(_room3, new MonsterSpawn(30, new(9, 14), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(30, new(19, 15), OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(26, new(20, 20), OnDeath: r =>
                    {
                        r.Summon(_room3, new MonsterSpawn(31, new(16, 9)));
                    }));
                }));
            }));
        r.SummonNpcs(_room3, new NpcSpawn(320, new(11, 15)));
        r.SpawnPortal(_room4, new(14, 28), _room5, new(14, 1));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 14));
        r.Summon(_room4, new MonsterSpawn(26, new(12, 11), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(31, new(16, 7), OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(31, new(9, 14)));
                }));
            }));
        r.SummonNpcs(_room4, new NpcSpawn(320, new(7, 16)));
        r.SpawnPortal(_room5, new(14, 1), _room4, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room6, new(1, 14), _room7, new(28, 15));
        r.Summon(_room6, new MonsterSpawn(31, new(19, 14), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(26, new(18, 21)),
                new MonsterSpawn(26, new(20, 8)));
            }));
        r.SpawnPortal(_room7, new(28, 15), _room6, new(1, 14), locked: false);
        r.SpawnExit(_room7, new(1, 15));
        r.PlaceButton(_room7, 1, new(15, 15), 1000, 1045);
        r.Summon(_room7, new MonsterSpawn(26, new(14, 21), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(31, new(16, 6), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(26, new(13, 8)));
                }));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6000);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6001);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6002);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6004);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room6, 6003);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room7 && button == 1)
        {
            r.UnlockExit(_room7);
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace1();
