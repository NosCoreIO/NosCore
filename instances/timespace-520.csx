public class TimeSpace520 : InstanceScript
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
    private readonly int _room15;
    private readonly int _room16;
    private readonly int _room17;
    private readonly int _room18;

    public TimeSpace520()
    {
        Definition = InstanceDefinitionBuilder.Named(520, "Dust King's Bone", "Dust King's Bone")
            .ForLevels(36, 99)
            .StartingAt(28, 15)
            .WithLives(1)
            .WithObjective(ObjectiveKind.KillMonster, 74, 1)
            .WithObjective(ObjectiveKind.UseObject, 1048, 3)
            .WithRoom(4100, out _room1, 3, 11)
            .WithRoom(4101, out _room2, 3, 10)
            .WithRoom(4102, out _room3, 2, 10)
            .WithRoom(4104, out _room4, 1, 10)
            .WithRoom(4103, out _room5, 4, 10)
            .WithRoom(4108, out _room6, 5, 10)
            .WithRoom(4106, out _room7, 3, 9)
            .WithRoom(4105, out _room8, 2, 9)
            .WithRoom(4107, out _room9, 4, 9)
            .WithRoom(4101, out _room10, 3, 8)
            .WithRoom(4102, out _room11, 3, 7)
            .WithRoom(4112, out _room12, 2, 7)
            .WithRoom(4113, out _room13, 2, 6)
            .WithRoom(4105, out _room14, 2, 5)
            .WithRoom(4111, out _room15, 4, 7)
            .WithRoom(4112, out _room16, 4, 6)
            .WithRoom(4112, out _room17, 4, 5)
            .WithRoom(4101, out _room18, 3, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SetMonsterLocker(_room14, 1, 62);
        r.SetMonsterLocker(_room18, 1, 74);
        r.SpawnPortal(_room1, new(14, 1), _room2, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room7, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room5, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(14, 28), _room1, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room3, new(28, 14), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room3, new(1, 14), _room4, new(28, 15));
        r.PlaceButton(_room3, 1, new(14, 15), 1000, 1045);
        r.Summon(_room3,
            new MonsterSpawn(59, new(19, 19)),
            new MonsterSpawn(59, new(19, 12)),
            new MonsterSpawn(59, new(18, 15)),
            new MonsterSpawn(137, new(13, 10)),
            new MonsterSpawn(137, new(12, 22)),
            new MonsterSpawn(59, new(10, 17)),
            new MonsterSpawn(59, new(6, 18), IsBonus: true));
        r.SpawnPortal(_room4, new(28, 15), _room3, new(1, 14));
        r.PlaceButton(_room4, 1, new(15, 15), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room4,
            new MonsterSpawn(59, new(17, 19)),
            new MonsterSpawn(59, new(16, 13)),
            new MonsterSpawn(59, new(16, 9)),
            new MonsterSpawn(59, new(15, 19)),
            new MonsterSpawn(141, new(12, 20)),
            new MonsterSpawn(141, new(10, 14)),
            new MonsterSpawn(141, new(12, 11)),
            new MonsterSpawn(67, new(8, 18)),
            new MonsterSpawn(67, new(8, 13), IsBonus: true));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(5, 15));
        r.SpawnPortal(_room5, new(1, 15), _room2, new(28, 15), locked: false);
        r.PlaceButton(_room5, 1, new(16, 16), 1000, 1045);
        r.Summon(_room5,
            new MonsterSpawn(137, new(22, 14)),
            new MonsterSpawn(137, new(20, 11)),
            new MonsterSpawn(59, new(19, 18)),
            new MonsterSpawn(59, new(16, 18)),
            new MonsterSpawn(59, new(12, 15)),
            new MonsterSpawn(137, new(11, 9)),
            new MonsterSpawn(59, new(9, 17), IsBonus: true),
            new MonsterSpawn(137, new(7, 12)));
        r.SpawnPortal(_room6, new(5, 15), _room5, new(28, 15));
        r.Summon(_room6,
            new MonsterSpawn(23, new(17, 16)),
            new MonsterSpawn(67, new(16, 17)),
            new MonsterSpawn(67, new(16, 14)),
            new MonsterSpawn(138, new(16, 8)),
            new MonsterSpawn(138, new(15, 20)),
            new MonsterSpawn(67, new(14, 17)),
            new MonsterSpawn(23, new(13, 16)),
            new MonsterSpawn(67, new(13, 14)),
            new MonsterSpawn(59, new(8, 16)),
            new MonsterSpawn(59, new(11, 11)));
        r.SpawnPortal(_room7, new(14, 1), _room10, new(14, 28), locked: false);
        r.SpawnPortal(_room7, new(28, 15), _room9, new(1, 14), locked: false);
        r.SpawnPortal(_room7, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room7, new(1, 15), _room8, new(28, 15), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(142, new(20, 13)),
            new MonsterSpawn(142, new(20, 17)),
            new MonsterSpawn(103, new(13, 16)),
            new MonsterSpawn(142, new(15, 15)),
            new MonsterSpawn(103, new(14, 12)),
            new MonsterSpawn(142, new(11, 17)),
            new MonsterSpawn(142, new(9, 12)));
        r.SpawnPortal(_room8, new(28, 15), _room7, new(1, 15));
        r.Summon(_room8,
            new MonsterSpawn(141, new(17, 17)),
            new MonsterSpawn(141, new(17, 11)),
            new MonsterSpawn(143, new(16, 19)),
            new MonsterSpawn(143, new(15, 15)),
            new MonsterSpawn(143, new(15, 9)),
            new MonsterSpawn(138, new(13, 11)),
            new MonsterSpawn(138, new(12, 17)),
            new MonsterSpawn(83, new(10, 14)));
        r.SpawnPortal(_room9, new(1, 14), _room7, new(28, 15));
        r.PlaceButton(_room9, 1, new(15, 15), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room9,
            new MonsterSpawn(84, new(19, 13)),
            new MonsterSpawn(84, new(20, 16), IsBonus: true),
            new MonsterSpawn(138, new(20, 18)),
            new MonsterSpawn(138, new(18, 13)),
            new MonsterSpawn(138, new(16, 10)),
            new MonsterSpawn(138, new(15, 22)),
            new MonsterSpawn(142, new(12, 22)),
            new MonsterSpawn(142, new(12, 18)),
            new MonsterSpawn(142, new(12, 14)),
            new MonsterSpawn(142, new(12, 9)));
        r.SpawnPortal(_room10, new(14, 1), _room11, new(14, 28));
        r.SpawnPortal(_room10, new(14, 28), _room7, new(14, 1), locked: false);
        r.SpawnPortal(_room11, new(14, 1), _room18, new(14, 28));
        r.SpawnPortal(_room11, new(28, 14), _room15, new(1, 15), locked: false);
        r.SpawnPortal(_room11, new(14, 28), _room10, new(14, 1), locked: false);
        r.SpawnPortal(_room11, new(1, 14), _room12, new(28, 14), locked: false);
        r.SpawnPortal(_room12, new(14, 1), _room13, new(14, 28), locked: false);
        r.SpawnPortal(_room12, new(28, 14), _room11, new(1, 14), locked: false);
        r.Summon(_room12,
            new MonsterSpawn(23, new(18, 17)),
            new MonsterSpawn(23, new(18, 13)),
            new MonsterSpawn(23, new(14, 18)),
            new MonsterSpawn(23, new(13, 13), IsBonus: true));
        r.SpawnPortal(_room13, new(14, 1), _room14, new(14, 28), locked: false);
        r.SpawnPortal(_room13, new(14, 28), _room12, new(14, 1));
        r.SpawnPortal(_room14, new(14, 28), _room13, new(14, 1));
        r.Summon(_room14,
            new MonsterSpawn(61, new(23, 15)),
            new MonsterSpawn(60, new(21, 12)),
            new MonsterSpawn(60, new(17, 15)),
            new MonsterSpawn(61, new(14, 15)),
            new MonsterSpawn(60, new(10, 16), IsBonus: true),
            new MonsterSpawn(60, new(8, 12)),
            new MonsterSpawn(61, new(5, 17)),
            new MonsterSpawn(62, new(13, 12), IsBonus: true));
        r.SpawnPortal(_room15, new(14, 1), _room16, new(14, 28), locked: false);
        r.SpawnPortal(_room15, new(1, 15), _room11, new(28, 14), locked: false);
        r.Summon(_room15,
            new MonsterSpawn(83, new(16, 11)),
            new MonsterSpawn(83, new(13, 20)),
            new MonsterSpawn(83, new(7, 14)));
        r.SpawnPortal(_room16, new(14, 1), _room17, new(14, 28), locked: false);
        r.SpawnPortal(_room16, new(14, 28), _room15, new(14, 1), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(82, new(23, 13)),
            new MonsterSpawn(144, new(18, 10)),
            new MonsterSpawn(144, new(15, 15)),
            new MonsterSpawn(82, new(15, 19)),
            new MonsterSpawn(144, new(13, 14)),
            new MonsterSpawn(82, new(10, 20)),
            new MonsterSpawn(82, new(8, 15)));
        r.SpawnPortal(_room17, new(14, 28), _room16, new(14, 1));
        r.PlaceButton(_room17, 1, new(15, 15), 1048, 1049, kind: ButtonKind.Sculpture);
        r.Summon(_room17,
            new MonsterSpawn(67, new(21, 17)),
            new MonsterSpawn(67, new(22, 11)),
            new MonsterSpawn(143, new(18, 14), IsBonus: true),
            new MonsterSpawn(67, new(17, 21)),
            new MonsterSpawn(67, new(16, 11)),
            new MonsterSpawn(143, new(15, 18)),
            new MonsterSpawn(143, new(12, 13)),
            new MonsterSpawn(67, new(10, 16)),
            new MonsterSpawn(67, new(6, 11)));
        r.SpawnPortal(_room18, new(14, 28), _room11, new(14, 1));
        r.Summon(_room18,
            new MonsterSpawn(61, new(21, 14), IsBonus: true),
            new MonsterSpawn(139, new(16, 16)),
            new MonsterSpawn(61, new(14, 25)),
            new MonsterSpawn(139, new(11, 16)),
            new MonsterSpawn(61, new(6, 13)),
            new MonsterSpawn(74, new(14, 9)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(420));
        }
        else if (room == _room6)
        {
            r.StartRoomClock(_room6, TimeSpan.FromSeconds(120));
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Dismiss(_room6, 23);
                r.Dismiss(_room6, 59);
                r.Dismiss(_room6, 67);
                r.Dismiss(_room6, 138);
                r.UnlockPortal(_room6, _room5);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(Game18NConstString.QuickMissionComplete);
                r.Message(Game18NConstString.NewGoal);
                r.StopRoomClock(_room6);
                if (r.IsButtonOn(_room4, 1))
                {
                    r.UnlockPortal(_room10, _room11);
                }
            });
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room8)
        {
            r.UnlockPortal(_room8, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room14)
        {
            r.UnlockPortal(_room14, _room13);
            r.AddClockTime(TimeSpan.FromSeconds(300));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Summon(_room13,
                new MonsterSpawn(61, new(20, 14)),
                new MonsterSpawn(61, new(19, 18)),
                new MonsterSpawn(69, new(17, 15)),
                new MonsterSpawn(69, new(16, 7)),
                new MonsterSpawn(61, new(12, 17)),
                new MonsterSpawn(69, new(10, 13)));
        }
        else if (room == _room13)
        {
            r.UnlockPortal(_room13, _room12);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room18)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Succeed();
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room4 && button == 1)
        {
            r.UnlockPortal(_room4, _room3);
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
            // The way north also needs the survival room behind the east lever to have run out.
            if (r.IsPortalOpen(_room6, _room5))
            {
                r.UnlockPortal(_room10, _room11);
            }
        }
        else if (room == _room5 && button == 1)
        {
            r.UnlockPortal(_room5, _room6);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room9 && button == 1)
        {
            r.UnlockPortal(_room9, _room7);
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
        }
        else if (room == _room17 && button == 1)
        {
            r.UnlockPortal(_room17, _room16);
            r.Message(Game18NConstString.ObjectHasBeenMoved);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.NewGoal);
            r.UnlockPortal(_room11, _room18);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace520();
