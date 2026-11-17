public class TimeSpace519 : InstanceScript
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

    public TimeSpace519()
    {
        Definition = InstanceDefinitionBuilder.Named(519, "Dust King's Heart", "Dust King's Heart")
            .ForLevels(36, 99)
            .StartingAt(14, 12)
            .WithLives(1)
            .WithObjective(ObjectiveKind.KillMonster, 73, 1)
            .WithRoom(4100, out _room1, 3, 11)
            .WithRoom(4101, out _room2, 3, 10)
            .WithRoom(4102, out _room3, 3, 9)
            .WithRoom(4103, out _room4, 4, 9)
            .WithRoom(4101, out _room5, 4, 8)
            .WithRoom(4102, out _room6, 3, 8)
            .WithRoom(4105, out _room7, 2, 8)
            .WithRoom(4103, out _room8, 3, 7)
            .WithRoom(4107, out _room9, 3, 6)
            .WithRoom(4111, out _room10, 2, 6)
            .WithRoom(4111, out _room11, 4, 6)
            .WithRoom(4106, out _room12, 3, 5)
            .WithRoom(4109, out _room13, 2, 9)
            .WithRoom(4107, out _room14, 3, 4)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SetMonsterLocker(_room7, 1, 139);
        r.SetMonsterLocker(_room11, 1, 144);
        r.SetMonsterLocker(_room14, 1, 73);
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(1, 15), locked: false);
        r.SpawnPortal(_room3, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room13, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(65, new(17, 17)),
            new MonsterSpawn(65, new(17, 12)),
            new MonsterSpawn(59, new(12, 16)),
            new MonsterSpawn(59, new(12, 15)),
            new MonsterSpawn(65, new(8, 15)),
            new MonsterSpawn(65, new(8, 10)));
        r.SpawnPortal(_room4, new(14, 1), _room5, new(14, 28), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 14), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(59, new(21, 16)),
            new MonsterSpawn(59, new(17, 13), IsBonus: true),
            new MonsterSpawn(137, new(15, 18)),
            new MonsterSpawn(22, new(15, 13)),
            new MonsterSpawn(137, new(14, 11)),
            new MonsterSpawn(22, new(12, 15)),
            new MonsterSpawn(59, new(4, 14)));
        r.SpawnPortal(_room5, new(14, 28), _room4, new(14, 1), locked: false);
        r.SpawnPortal(_room5, new(1, 14), _room6, new(28, 14), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(137, new(20, 13)),
            new MonsterSpawn(22, new(19, 15)),
            new MonsterSpawn(142, new(13, 11)),
            new MonsterSpawn(142, new(14, 19)),
            new MonsterSpawn(137, new(13, 17)),
            new MonsterSpawn(137, new(13, 12)),
            new MonsterSpawn(22, new(10, 15)));
        r.SpawnPortal(_room6, new(14, 1), _room8, new(14, 28), locked: false);
        r.SpawnPortal(_room6, new(28, 14), _room5, new(1, 14), locked: false);
        r.SpawnPortal(_room6, new(1, 14), _room7, new(28, 15), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(67, new(24, 14)),
            new MonsterSpawn(142, new(19, 11)),
            new MonsterSpawn(142, new(18, 16), IsBonus: true),
            new MonsterSpawn(67, new(14, 21)),
            new MonsterSpawn(142, new(15, 12)),
            new MonsterSpawn(137, new(12, 16)),
            new MonsterSpawn(137, new(11, 14)),
            new MonsterSpawn(142, new(12, 12)));
        r.SpawnPortal(_room7, new(28, 15), _room6, new(1, 14));
        r.Summon(_room7,
            new MonsterSpawn(138, new(19, 16)),
            new MonsterSpawn(138, new(16, 13)),
            new MonsterSpawn(84, new(15, 17)),
            new MonsterSpawn(84, new(14, 12)),
            new MonsterSpawn(84, new(13, 20)),
            new MonsterSpawn(138, new(13, 16), IsBonus: true),
            new MonsterSpawn(84, new(13, 10)),
            new MonsterSpawn(139, new(9, 14)));
        r.SpawnPortal(_room8, new(14, 1), _room9, new(14, 28));
        r.SpawnPortal(_room8, new(14, 28), _room6, new(14, 1));
        r.Summon(_room8,
            new MonsterSpawn(138, new(23, 18)),
            new MonsterSpawn(67, new(21, 12)),
            new MonsterSpawn(142, new(21, 21), IsBonus: true),
            new MonsterSpawn(138, new(15, 16)),
            new MonsterSpawn(142, new(16, 8)),
            new MonsterSpawn(142, new(14, 19)),
            new MonsterSpawn(67, new(14, 12)),
            new MonsterSpawn(142, new(13, 8), IsBonus: true),
            new MonsterSpawn(142, new(10, 9)),
            new MonsterSpawn(138, new(9, 17)));
        r.SpawnPortal(_room9, new(14, 1), _room12, new(14, 28), locked: false);
        r.SpawnPortal(_room9, new(28, 15), _room11, new(1, 15), locked: false);
        r.SpawnPortal(_room9, new(14, 28), _room8, new(14, 1), locked: false);
        r.SpawnPortal(_room9, new(1, 14), _room10, new(28, 15), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(67, new(24, 15)),
            new MonsterSpawn(138, new(19, 19)),
            new MonsterSpawn(138, new(17, 14)),
            new MonsterSpawn(67, new(14, 9), IsBonus: true),
            new MonsterSpawn(67, new(15, 22)),
            new MonsterSpawn(138, new(14, 13)),
            new MonsterSpawn(67, new(12, 14)));
        r.SpawnPortal(_room10, new(28, 15), _room9, new(1, 14));
        r.PlaceButton(_room10, 1, new(4, 23), 1000, 1045);
        r.Summon(_room10,
            new MonsterSpawn(84, new(19, 15)),
            new MonsterSpawn(84, new(18, 11)),
            new MonsterSpawn(84, new(17, 23)),
            new MonsterSpawn(67, new(13, 11), IsBonus: true),
            new MonsterSpawn(67, new(12, 17)),
            new MonsterSpawn(67, new(10, 14)),
            new MonsterSpawn(144, new(9, 12)));
        r.SpawnPortal(_room11, new(1, 15), _room9, new(28, 15));
        r.Summon(_room11,
            new MonsterSpawn(143, new(16, 19)),
            new MonsterSpawn(143, new(16, 10)),
            new MonsterSpawn(143, new(13, 12)),
            new MonsterSpawn(143, new(11, 20), IsBonus: true),
            new MonsterSpawn(143, new(11, 14)),
            new MonsterSpawn(143, new(10, 10)),
            new MonsterSpawn(144, new(20, 14)));
        r.SpawnPortal(_room12, new(14, 1), _room14, new(14, 28));
        r.SpawnPortal(_room12, new(14, 28), _room9, new(14, 1), locked: false);
        r.SpawnPortal(_room13, new(28, 15), _room3, new(1, 14));
        r.Summon(_room13,
            new MonsterSpawn(137, new(19, 18)),
            new MonsterSpawn(137, new(19, 14)),
            new MonsterSpawn(137, new(19, 11)),
            new MonsterSpawn(82, new(16, 22)),
            new MonsterSpawn(82, new(16, 19)),
            new MonsterSpawn(82, new(16, 11), IsBonus: true),
            new MonsterSpawn(82, new(16, 7)),
            new MonsterSpawn(60, new(9, 13)),
            new MonsterSpawn(60, new(9, 11)));
        r.SpawnPortal(_room14, new(14, 28), _room12, new(14, 1));
        r.Summon(_room14,
            new MonsterSpawn(139, new(22, 15)),
            new MonsterSpawn(84, new(18, 15), IsBonus: true),
            new MonsterSpawn(84, new(15, 15)),
            new MonsterSpawn(143, new(9, 15)),
            new MonsterSpawn(143, new(6, 15)),
            new MonsterSpawn(73, new(16, 10)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(300));
        }
        else if (room == _room8)
        {
            r.StartRoomClock(_room8, TimeSpan.FromSeconds(100));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room7)
        {
            r.UnlockPortal(_room7, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.UnlockPortal(_room3, _room13);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room9);
            r.UnlockPortal(_room8, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.StopRoomClock(_room8);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            OpenLastRoom(r);
        }
        else if (room == _room13)
        {
            r.UnlockPortal(_room13, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            OpenLastRoom(r);
        }
        else if (room == _room14)
        {
            r.UnlockPortal(_room14, _room12);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.NewGoal);
            r.Message(Game18NConstString.QuestComplete);
            r.Succeed();
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room10 && button == 1)
        {
            r.UnlockPortal(_room10, _room9);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
            OpenLastRoom(r);
        }
    }

    // The last room opens once both side missions are done and the lever is pulled, in any order.
    private void OpenLastRoom(IInstanceRun r)
    {
        if (r.LockerLeft(_room11) == 0 && r.MonstersAlive(_room13) == 0 && r.IsButtonOn(_room10, 1))
        {
            r.UnlockPortal(_room12, _room14);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace519();
