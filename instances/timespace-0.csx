public class TimeSpace0 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace0()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "Time-Space Tutorial", "Time-Space Tutorial")
            .ForLevels(1, 99)
            .StartingAt(11, 10)
            .WithDraw(2023, 1)
            .WithDraw(2024, 1)
            .WithDraw(2025, 1)
            .WithDraw(2026, 1)
            .WithDraw(2027, 1)
            .WithSpecialReward(2016, 2)
            .WithReward(13, 1)
            .WithRoom(4100, out _room1, 3, 11)
            .WithRoom(4105, out _room2, 3, 10)
            .WithRoom(4114, out _room3, 3, 9)
            .WithRoom(4100, out _room4, 3, 8)
            .WithRoom(4101, out _room5, 3, 7)
            .WithRoom(4102, out _room6, 3, 6)
            .WithRoom(4103, out _room7, 3, 5)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.Summon(_room2, new MonsterSpawn(24, new(13, 14), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(24, new(17, 12)));
            }));
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28));
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1));
        r.Summon(_room3, new MonsterSpawn(333, new(12, 13)));
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1));
        r.PlaceButton(_room4, 1, new(14, 18), 1000, 1045);
        r.SpawnPortal(_room5, new(14, 1), _room6, new(14, 28));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1));
        r.Summon(_room5, new MonsterSpawn(333, new(11, 14), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(24, new(16, 17)));
            }));
        r.SpawnPortal(_room6, new(14, 1), _room7, new(14, 28));
        r.SpawnPortal(_room6, new(14, 28), _room5, new(14, 1));
        r.Summon(_room6,
            new MonsterSpawn(25, new(15, 12)),
            new MonsterSpawn(24, new(11, 14)));
        r.SummonNpcs(_room6, new NpcSpawn(320, new(14, 14)));
        r.SpawnExit(_room7, new(14, 1));
        r.SpawnPortal(_room7, new(14, 28), _room6, new(14, 1));
        r.Summon(_room7,
            new MonsterSpawn(24, new(19, 12)),
            new MonsterSpawn(333, new(15, 17)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
            r.Dialog(_room1, 8004);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 8005);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 8006);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 8008);
        }
        else if (room == _room5)
        {
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(120));
            r.Dialog(_room5, 8010);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 8013);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 8011);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room3, 8007);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 8009);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room5);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room7)
        {
            r.UnlockExit(_room7);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room7, 8012);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room4 && button == 1)
        {
            r.Summon(_room4,
                new MonsterSpawn(333, new(20, 15)),
                new MonsterSpawn(333, new(8, 15)));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace0();
