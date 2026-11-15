public class TimeSpace8 : InstanceScript
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

    public TimeSpace8()
    {
        Definition = InstanceDefinitionBuilder.Named(8, "Nasty Imps", "Nasty Imps")
            .ForLevels(17, 99)
            .StartingAt(14, 10)
            .Requiring(1012, 1)
            .WithDraw(1031, 1)
            .WithDraw(1032, 1)
            .WithSpecialReward(1017, 2)
            .WithReward(2028, 2)
            .WithRoom(4100, out _room1, 0, 8)
            .WithRoom(4101, out _room2, 0, 9)
            .WithRoom(4101, out _room3, 0, 10)
            .WithRoom(4102, out _room4, 1, 10)
            .WithRoom(4103, out _room5, 2, 10)
            .WithRoom(4106, out _room6, 3, 10)
            .WithRoom(4106, out _room7, 2, 11)
            .WithRoom(4113, out _room8, 3, 11)
            .WithRoom(4112, out _room9, 4, 11)
            .WithRoom(4100, out _room10, 5, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(180));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room2, new(14, 28));
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 14));
        r.Summon(_room3,
            new MonsterSpawn(130, new(21, 9), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(130, new(24, 15)));
            }),
            new MonsterSpawn(130, new(18, 18)),
            new MonsterSpawn(72, new(19, 14), IsBonus: true));
        r.SpawnPortal(_room4, new(28, 14), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(1, 14), _room3, new(28, 15), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(55, new(15, 21), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(55, new(15, 9)));
            }),
            new MonsterSpawn(55, new(13, 9), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(130, new(17, 19)));
            }),
            new MonsterSpawn(55, new(12, 21)),
            new MonsterSpawn(72, new(15, 14)));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15));
        r.SpawnPortal(_room5, new(14, 28), _room7, new(14, 1));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 14));
        r.Summon(_room5,
            new MonsterSpawn(98, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(130, new(23, 17), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(130, new(21, 12)));
                }));
            }),
            new MonsterSpawn(72, new(17, 14), IsBonus: true));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15), locked: false);
        r.PlaceButton(_room6, 1, new(15, 14), 1000, 1045);
        r.SpawnPortal(_room7, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room7, new(28, 15), _room8, new(1, 15));
        r.Summon(_room7,
            new MonsterSpawn(63, new(7, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(130, new(17, 18), OnDeath: r =>
                {
                    r.Summon(_room7, new MonsterSpawn(63, new(20, 9)));
                }));
            }),
            new MonsterSpawn(72, new(15, 14)));
        r.SummonNpcs(_room7, new NpcSpawn(320, new(15, 7)));
        r.SpawnPortal(_room8, new(28, 15), _room9, new(1, 15));
        r.SpawnPortal(_room8, new(1, 15), _room7, new(28, 15));
        r.Summon(_room8,
            new MonsterSpawn(129, new(15, 21), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(129, new(10, 15), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(130, new(19, 7)));
                }),
                new MonsterSpawn(129, new(16, 8)));
            }),
            new MonsterSpawn(72, new(19, 15)),
            new MonsterSpawn(72, new(19, 10), IsBonus: true));
        r.SpawnPortal(_room9, new(28, 14), _room10, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(1, 15), _room8, new(28, 15));
        r.SpawnExit(_room10, new(28, 15));
        r.SpawnPortal(_room10, new(1, 15), _room9, new(28, 14));
        r.Summon(_room10, new MonsterSpawn(72, new(15, 10), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(72, new(17, 14)));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6038);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6040);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6042);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6044);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6048);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room2);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room3, 6039);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6041);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room5, 6043);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room8);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room7, 6045);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room8, 6049);
        }
        else if (room == _room10)
        {
            r.UnlockExit(_room10);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room10, 6051);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room6 && button == 1)
        {
            r.Message(Game18NConstString.LeverHasBeenPulled);
            r.UnlockPortal(_room5, _room7);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace8();
