// The Third Path
public class TimeSpace631 : InstanceScript
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

    public TimeSpace631()
    {
        Definition = InstanceDefinitionBuilder.Named(631, "", "The Third Path")
            .ForLevels(47, 99)
            .StartingAt(8, 12)
            .WithLives(1)
            .Requiring(1012, 1)
            .WithRoom(4416, out _room1, 6, 6)
            .WithRoom(4409, out _room2, 6, 6)
            .WithRoom(4402, out _room3, 5, 6)
            .WithRoom(4406, out _room4, 4, 6)
            .WithRoom(4401, out _room5, 4, 7)
            .WithRoom(4410, out _room6, 3, 7)
            .WithRoom(4401, out _room7, 3, 8)
            .WithRoom(4404, out _room8, 2, 7)
            .WithRoom(4411, out _room9, 2, 6)
            .WithRoom(4404, out _room10, 1, 6)
            .WithRoom(4403, out _room11, 2, 5)
            .WithRoom(4405, out _room12, 3, 5)
            .WithRoom(4403, out _room13, 4, 5)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(1, 12), _room2, new(23, 11), locked: false);
        r.SpawnPortal(_room2, new(1, 11), _room3, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 11), _room1, new(1, 12), locked: false);
        r.SpawnPortal(_room3, new(12, 23), _room4, new(12, 1), locked: false);
        r.SpawnPortal(_room3, new(23, 12), _room2, new(1, 11), locked: false);
        r.SpawnPortal(_room4, new(1, 12), _room5, new(23, 11));
        r.SpawnPortal(_room4, new(12, 1), _room3, new(12, 23));
        r.Summon(_room4,
            new MonsterSpawn(147, new(9, 7)),
            new MonsterSpawn(147, new(12, 9)),
            new MonsterSpawn(147, new(16, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(150, new(14, 15), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(153, new(11, 15), IsBonus: true),
                    new MonsterSpawn(150, new(6, 10), IsBonus: true));
                }),
                new MonsterSpawn(153, new(7, 12), IsBonus: true));
            }));
        r.SpawnPortal(_room5, new(12, 23), _room6, new(12, 1), locked: false);
        r.SpawnPortal(_room5, new(23, 11), _room4, new(1, 12), locked: false);
        r.SpawnExit(_room6, new(12, 23), locked: false);
        r.SpawnPortal(_room6, new(12, 1), _room7, new(12, 23), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(150, new(11, 22), IsBonus: true),
            new MonsterSpawn(153, new(12, 11)),
            new MonsterSpawn(150, new(13, 22), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(147, new(6, 14)),
                new MonsterSpawn(147, new(6, 11), IsBonus: true),
                new MonsterSpawn(147, new(6, 8), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room6,
                    new MonsterSpawn(147, new(19, 11), IsBonus: true),
                    new MonsterSpawn(147, new(18, 13), IsBonus: true),
                    new MonsterSpawn(147, new(18, 8)));
                }));
            }));
        r.SpawnPortal(_room7, new(1, 11), _room7, new(1, 11), locked: false);
        r.SpawnPortal(_room7, new(12, 23), _room6, new(12, 1), locked: false);
        r.SpawnPortal(_room8, new(1, 12), _room9, new(23, 11), locked: false);
        r.SpawnPortal(_room8, new(12, 23), _room7, new(12, 0), locked: false);
        r.SpawnExit(_room9, new(1, 11), locked: false);
        r.SpawnPortal(_room9, new(23, 11), _room10, new(1, 12));
        r.Summon(_room9,
            new MonsterSpawn(147, new(10, 11), IsBonus: true),
            new MonsterSpawn(147, new(10, 12)),
            new MonsterSpawn(147, new(15, 11)),
            new MonsterSpawn(147, new(15, 12), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(150, new(22, 11), OnDeath: r =>
                {
                    r.Summon(_room9,
                    new MonsterSpawn(153, new(16, 16)),
                    new MonsterSpawn(153, new(16, 8)),
                    new MonsterSpawn(153, new(10, 16)),
                    new MonsterSpawn(153, new(9, 8), OnDeath: r =>
                    {
                        r.Summon(_room9, new MonsterSpawn(150, new(5, 11), IsBonus: true));
                    }));
                }));
            }));
        r.SpawnPortal(_room10, new(1, 12), _room9, new(23, 11), locked: false);
        r.SpawnPortal(_room10, new(12, 1), _room10, new(12, 1), locked: false);
        r.SpawnPortal(_room11, new(1, 12), _room10, new(23, 0), locked: false);
        r.SpawnPortal(_room11, new(23, 12), _room12, new(1, 12), locked: false);
        r.SpawnPortal(_room12, new(1, 12), _room13, new(23, 12));
        r.Summon(_room12,
            new MonsterSpawn(147, new(5, 9), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(147, new(11, 19), IsBonus: true),
                new MonsterSpawn(147, new(5, 11)));
            }),
            new MonsterSpawn(150, new(6, 15), OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(147, new(9, 18)));
            }));
        r.SpawnPortal(_room13, new(23, 12), _room13, new(23, 12), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(600));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room13);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace631();
