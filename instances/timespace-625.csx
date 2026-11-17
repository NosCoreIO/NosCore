public class TimeSpace625 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace625()
    {
        Definition = InstanceDefinitionBuilder.Named(625, "Gate 2", "Gate 2")
            .ForLevels(30, 99)
            .StartingAt(15, 9)
            .WithLives(3)
            .WithRoom(4201, out _room1, 4, 11)
            .WithRoom(4206, out _room2, 4, 10)
            .WithRoom(4206, out _room3, 4, 9)
            .WithRoom(4206, out _room4, 4, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28));
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1));
        r.Summon(_room2,
            new MonsterSpawn(380, new(12, 18), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(380, new(18, 16), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(380, new(11, 10), IsBonus: true));
                }));
            }),
            new MonsterSpawn(380, new(7, 13), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(380, new(21, 14), OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(380, new(17, 10), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28));
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1));
        r.Summon(_room3,
            new MonsterSpawn(380, new(18, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(380, new(11, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(380, new(11, 11), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room3, new MonsterSpawn(380, new(18, 11), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(380, new(18, 13), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(380, new(11, 9)),
                    new MonsterSpawn(380, new(18, 8), IsBonus: true));
                }));
            }),
            new MonsterSpawn(380, new(11, 15)));
        r.SpawnExit(_room4, new(14, 1));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1));
        r.Summon(_room4,
            new MonsterSpawn(380, new(18, 17), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(380, new(12, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(380, new(15, 9), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(380, new(14, 19)));
                    }));
                }),
                new MonsterSpawn(380, new(18, 13), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(380, new(15, 20), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4,
                        new MonsterSpawn(380, new(9, 15), IsBonus: true),
                        new MonsterSpawn(380, new(21, 15)));
                    }));
                }));
            }),
            new MonsterSpawn(380, new(11, 17), IsBonus: true));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(250));
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(150));
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(120));
        }
        else if (room == _room4)
        {
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(90));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.UnlockPortal(_room2, _room1);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room2);
        }
        else if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room2);
            r.AddClockTime(TimeSpan.FromSeconds(50));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room3);
        }
        else if (room == _room4)
        {
            r.UnlockExit(_room4);
            r.UnlockPortal(_room4, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room4);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace625();
