public class TimeSpace28 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;
    private readonly int _room8;

    public TimeSpace28()
    {
        Definition = InstanceDefinitionBuilder.Named(28, "Le choix des Kenkos", "Le choix des Kenkos")
            .ForLevels(38, 99)
            .StartingAt(9, 8)
            .Requiring(1012, 3)
            .WithLives(1)
            .WithDraw(711, 1)
            .WithDraw(1027, 10)
            .WithSpecialReward(1011, 50)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 7)
            .WithRoom(4209, out _room2, 1, 7)
            .WithRoom(4207, out _room3, 1, 8)
            .WithRoom(4209, out _room4, 2, 8)
            .WithRoom(4206, out _room5, 2, 9)
            .WithRoom(4207, out _room6, 2, 10)
            .WithRoom(4210, out _room7, 3, 10)
            .WithRoom(4203, out _room8, 3, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(100));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room3, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room2, new(14, 28));
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15));
        r.PlaceButton(_room3, 1, new(16, 15), 1000, 1045);
        r.Summon(_room3, new MonsterSpawn(145, new(20, 16), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(59, new(6, 13)),
                new MonsterSpawn(59, new(7, 18)),
                new MonsterSpawn(59, new(9, 14)),
                new MonsterSpawn(59, new(10, 7)),
                new MonsterSpawn(59, new(10, 12)),
                new MonsterSpawn(59, new(10, 16)),
                new MonsterSpawn(59, new(10, 21), IsBonus: true),
                new MonsterSpawn(59, new(12, 19)),
                new MonsterSpawn(59, new(13, 10)),
                new MonsterSpawn(59, new(13, 13)),
                new MonsterSpawn(59, new(15, 19)));
            }));
        r.SpawnPortal(_room4, new(14, 28), _room5, new(14, 1));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15));
        r.PlaceButton(_room4, 1, new(15, 15), 1000, 1045);
        r.Summon(_room4, new MonsterSpawn(145, new(13, 14), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(137, new(4, 15)),
                new MonsterSpawn(137, new(6, 10)),
                new MonsterSpawn(137, new(7, 20)),
                new MonsterSpawn(137, new(8, 15)),
                new MonsterSpawn(137, new(9, 8)),
                new MonsterSpawn(137, new(10, 14)),
                new MonsterSpawn(137, new(10, 21)),
                new MonsterSpawn(137, new(11, 9)),
                new MonsterSpawn(137, new(13, 19)),
                new MonsterSpawn(137, new(14, 12)),
                new MonsterSpawn(137, new(14, 23), IsBonus: true),
                new MonsterSpawn(137, new(15, 6)),
                new MonsterSpawn(137, new(15, 9)),
                new MonsterSpawn(137, new(16, 19)),
                new MonsterSpawn(137, new(17, 9)));
            }));
        r.SpawnPortal(_room5, new(14, 1), _room4, new(14, 28));
        r.SpawnPortal(_room5, new(14, 28), _room6, new(14, 1));
        r.PlaceButton(_room5, 1, new(12, 15), 1000, 1045);
        r.Summon(_room5,
            new MonsterSpawn(145, new(17, 16), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(23, new(10, 10)),
                new MonsterSpawn(23, new(10, 12)),
                new MonsterSpawn(23, new(10, 15)),
                new MonsterSpawn(23, new(10, 18)),
                new MonsterSpawn(23, new(10, 22)),
                new MonsterSpawn(23, new(11, 10)),
                new MonsterSpawn(23, new(12, 22)),
                new MonsterSpawn(23, new(14, 10)),
                new MonsterSpawn(23, new(15, 22)),
                new MonsterSpawn(23, new(17, 10)),
                new MonsterSpawn(23, new(18, 22)),
                new MonsterSpawn(23, new(19, 22)));
            }),
            new MonsterSpawn(145, new(13, 14), IsBonus: true));
        r.SpawnPortal(_room6, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15));
        r.PlaceButton(_room6, 1, new(15, 16), 1000, 1045);
        r.Summon(_room6,
            new MonsterSpawn(145, new(17, 15)),
            new MonsterSpawn(145, new(15, 12)),
            new MonsterSpawn(145, new(12, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(137, new(5, 10)),
                new MonsterSpawn(137, new(5, 16)),
                new MonsterSpawn(137, new(8, 13)),
                new MonsterSpawn(137, new(8, 16)),
                new MonsterSpawn(137, new(8, 18)),
                new MonsterSpawn(137, new(8, 23)),
                new MonsterSpawn(137, new(9, 8)),
                new MonsterSpawn(137, new(10, 10)),
                new MonsterSpawn(137, new(11, 20)),
                new MonsterSpawn(137, new(13, 6)),
                new MonsterSpawn(137, new(13, 12), IsBonus: true),
                new MonsterSpawn(137, new(13, 17)),
                new MonsterSpawn(137, new(14, 9)),
                new MonsterSpawn(137, new(14, 21)),
                new MonsterSpawn(137, new(14, 24)),
                new MonsterSpawn(137, new(16, 7)));
            }));
        r.SpawnPortal(_room7, new(15, 1), _room8, new(14, 28));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15));
        r.PlaceButton(_room7, 1, new(16, 16), 1000, 1045);
        r.Summon(_room7,
            new MonsterSpawn(145, new(18, 18), IsBonus: true),
            new MonsterSpawn(145, new(19, 13)),
            new MonsterSpawn(145, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(67, new(8, 11)),
                new MonsterSpawn(67, new(8, 13)),
                new MonsterSpawn(67, new(8, 17)),
                new MonsterSpawn(67, new(9, 20)),
                new MonsterSpawn(67, new(11, 8)),
                new MonsterSpawn(67, new(13, 13)),
                new MonsterSpawn(67, new(13, 17)),
                new MonsterSpawn(67, new(14, 8)),
                new MonsterSpawn(67, new(16, 22)),
                new MonsterSpawn(67, new(17, 13)),
                new MonsterSpawn(67, new(18, 16)));
            }),
            new MonsterSpawn(145, new(10, 12)));
        r.SpawnExit(_room8, new(15, 4), locked: false);
        r.SpawnPortal(_room8, new(14, 28), _room7, new(15, 1), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Message(_room2, Game18NConstString.NewGoal);
            r.Dialog(_room2, 6129);
        }
        else if (room == _room8)
        {
            r.Message(_room8, Game18NConstString.NewGoal);
            r.Dialog(_room8, 6130);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(_room3, Game18NConstString.ADoorHasOpened);
            r.Message(_room3, Game18NConstString.NewGoal);
        }
        else if (room == _room4 && button == 1)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(_room4, Game18NConstString.ADoorHasOpened);
            r.Message(_room4, Game18NConstString.NewGoal);
        }
        else if (room == _room5 && button == 1)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(_room5, Game18NConstString.ADoorHasOpened);
            r.Message(_room5, Game18NConstString.NewGoal);
        }
        else if (room == _room6 && button == 1)
        {
            r.UnlockPortal(_room6, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(_room6, Game18NConstString.ADoorHasOpened);
            r.Message(_room6, Game18NConstString.NewGoal);
        }
        else if (room == _room7 && button == 1)
        {
            r.UnlockPortal(_room7, _room8);
            r.Message(_room7, Game18NConstString.ADoorHasOpened);
            r.Message(_room7, Game18NConstString.NewGoal);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace28();
