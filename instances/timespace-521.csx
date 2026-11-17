public class TimeSpace521 : InstanceScript
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
    private readonly int _room14;

    public TimeSpace521()
    {
        Definition = InstanceDefinitionBuilder.Named(521, "Powerless Dust King", "Powerless Dust King")
            .ForLevels(36, 99)
            .StartingAt(12, 12)
            .WithLives(1)
            .WithObjective(ObjectiveKind.KillMonster, 281, 1)
            .WithRoom(4100, out _room1, 3, 11)
            .WithRoom(4101, out _room2, 3, 10)
            .WithRoom(4102, out _room3, 4, 10)
            .WithRoom(4101, out _room4, 4, 9)
            .WithRoom(4104, out _room5, 4, 8)
            .WithRoom(4105, out _room6, 3, 8)
            .WithRoom(4106, out _room7, 4, 7)
            .WithRoom(4107, out _room8, 3, 7)
            .WithRoom(4106, out _room9, 3, 6)
            .WithRoom(4111, out _room10, 3, 5)
            .WithRoom(4110, out _room11, 4, 5)
            .WithRoom(4112, out _room12, 3, 4)
            .WithRoom(4111, out _room13, 4, 4)
            .WithRoom(4112, out _room14, 4, 3)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SetMonsterLocker(_room14, 1, 281);
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(14, 1), _room4, new(14, 28), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(59, new(21, 15)),
            new MonsterSpawn(59, new(17, 11)),
            new MonsterSpawn(59, new(16, 8)),
            new MonsterSpawn(59, new(14, 5)),
            new MonsterSpawn(59, new(12, 9)),
            new MonsterSpawn(59, new(4, 17)));
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28));
        r.SpawnPortal(_room4, new(14, 28), _room3, new(14, 1));
        r.Summon(_room4,
            new MonsterSpawn(142, new(21, 20)),
            new MonsterSpawn(59, new(20, 14)),
            new MonsterSpawn(142, new(20, 9)),
            new MonsterSpawn(142, new(18, 16), IsBonus: true),
            new MonsterSpawn(67, new(16, 12)),
            new MonsterSpawn(59, new(14, 20)),
            new MonsterSpawn(59, new(14, 9)),
            new MonsterSpawn(67, new(11, 16)),
            new MonsterSpawn(59, new(10, 14)),
            new MonsterSpawn(67, new(10, 13)));
        r.SpawnPortal(_room5, new(14, 1), _room7, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room6, new(28, 15), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(67, new(24, 14)),
            new MonsterSpawn(67, new(19, 13), IsBonus: true),
            new MonsterSpawn(67, new(12, 11)),
            new MonsterSpawn(84, new(16, 8)),
            new MonsterSpawn(84, new(12, 18)),
            new MonsterSpawn(67, new(8, 14)));
        r.SpawnPortal(_room6, new(28, 15), _room5, new(1, 15));
        r.Summon(_room6,
            new MonsterSpawn(60, new(21, 11)),
            new MonsterSpawn(60, new(20, 19)),
            new MonsterSpawn(138, new(17, 15)),
            new MonsterSpawn(138, new(14, 14)),
            new MonsterSpawn(138, new(13, 17)),
            new MonsterSpawn(60, new(13, 10), IsBonus: true),
            new MonsterSpawn(60, new(10, 17)));
        r.SpawnPortal(_room7, new(14, 28), _room5, new(14, 1), locked: false);
        r.SpawnPortal(_room7, new(1, 15), _room8, new(28, 15), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(85, new(20, 17)),
            new MonsterSpawn(132, new(14, 15)),
            new MonsterSpawn(85, new(12, 15)),
            new MonsterSpawn(85, new(6, 17)));
        r.SpawnPortal(_room8, new(14, 1), _room9, new(14, 28));
        r.SpawnPortal(_room8, new(28, 15), _room7, new(1, 15), locked: false);
        r.PlaceButton(_room8, 1, new(23, 15), 1000, 1045);
        r.Summon(_room8,
            new MonsterSpawn(141, new(22, 15)),
            new MonsterSpawn(141, new(19, 17)),
            new MonsterSpawn(141, new(19, 11)),
            new MonsterSpawn(141, new(16, 13)),
            new MonsterSpawn(141, new(15, 18)),
            new MonsterSpawn(141, new(13, 14)),
            new MonsterSpawn(141, new(10, 11)),
            new MonsterSpawn(141, new(11, 17)));
        r.SpawnPortal(_room9, new(14, 1), _room10, new(14, 28));
        r.SpawnPortal(_room9, new(14, 28), _room8, new(14, 1));
        r.Summon(_room9,
            new MonsterSpawn(62, new(20, 19)),
            new MonsterSpawn(62, new(15, 13)),
            new MonsterSpawn(62, new(14, 17)));
        r.SpawnPortal(_room10, new(14, 1), _room12, new(14, 28), locked: false);
        r.SpawnPortal(_room10, new(28, 15), _room11, new(1, 15), locked: false);
        r.SpawnPortal(_room10, new(14, 28), _room9, new(14, 1), locked: false);
        r.Summon(_room10,
            new MonsterSpawn(142, new(23, 15), IsBonus: true),
            new MonsterSpawn(142, new(19, 12)),
            new MonsterSpawn(142, new(17, 17)),
            new MonsterSpawn(138, new(14, 10)),
            new MonsterSpawn(138, new(12, 19)),
            new MonsterSpawn(138, new(11, 9)),
            new MonsterSpawn(142, new(9, 15)));
        r.SpawnPortal(_room11, new(1, 15), _room10, new(28, 15));
        r.Summon(_room11,
            new MonsterSpawn(138, new(21, 14), IsBonus: true),
            new MonsterSpawn(22, new(16, 18)),
            new MonsterSpawn(22, new(16, 12)),
            new MonsterSpawn(138, new(15, 15)),
            new MonsterSpawn(22, new(14, 18)),
            new MonsterSpawn(22, new(14, 12)),
            new MonsterSpawn(138, new(6, 15)));
        r.SpawnPortal(_room12, new(28, 14), _room13, new(1, 15), locked: false);
        r.SpawnPortal(_room12, new(14, 28), _room10, new(14, 1), locked: false);
        r.SpawnPortal(_room13, new(14, 1), _room14, new(14, 28));
        r.SpawnPortal(_room13, new(1, 15), _room12, new(28, 14));
        r.Summon(_room13,
            new MonsterSpawn(73, new(21, 14)),
            new MonsterSpawn(85, new(19, 17)),
            new MonsterSpawn(73, new(14, 19)),
            new MonsterSpawn(132, new(14, 10)),
            new MonsterSpawn(85, new(12, 19)),
            new MonsterSpawn(73, new(4, 14)));
        r.SpawnPortal(_room14, new(14, 28), _room13, new(14, 1));
        r.Summon(_room14,
            new MonsterSpawn(142, new(24, 12)),
            new MonsterSpawn(142, new(19, 15), IsBonus: true),
            new MonsterSpawn(67, new(19, 10)),
            new MonsterSpawn(137, new(9, 15)),
            new MonsterSpawn(67, new(10, 11)),
            new MonsterSpawn(137, new(5, 12)),
            new MonsterSpawn(281, new(14, 9)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(450));
        }
        else if (room == _room4)
        {
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(100));
        }
        else if (room == _room9)
        {
            r.StartRoomClock(_room9, TimeSpan.FromSeconds(100));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room4);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room5);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room10);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room9);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room10);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room13)
        {
            r.UnlockPortal(_room13, _room14);
            r.AddClockTime(TimeSpan.FromSeconds(600));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room14)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room14, 6331);
            r.Succeed();
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room8 && button == 1)
        {
            r.UnlockPortal(_room8, _room9);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace521();
