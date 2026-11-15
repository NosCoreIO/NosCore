public class TimeSpace22 : InstanceScript
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

    public TimeSpace22()
    {
        Definition = InstanceDefinitionBuilder.Named(22, "A New Companion", "A New Companion")
            .ForLevels(32, 99)
            .StartingAt(8, 10)
            .Requiring(1012, 3)
            .WithLives(3)
            .WithDraw(180, 1)
            .WithDraw(181, 1)
            .WithSpecialReward(1028, 10)
            .WithReward(2131, 1)
            .WithReward(1087, 1)
            .WithRoom(5203, out _room1, 6, 9)
            .WithRoom(5203, out _room2, 5, 9)
            .WithRoom(5203, out _room3, 4, 9)
            .WithRoom(5203, out _room4, 4, 8)
            .WithRoom(5203, out _room5, 3, 8)
            .WithRoom(5203, out _room6, 2, 8)
            .WithRoom(5203, out _room7, 2, 7)
            .WithRoom(5203, out _room8, 3, 7)
            .WithRoom(5203, out _room9, 4, 7)
            .WithRoom(5203, out _room10, 5, 7)
            .WithRoom(5203, out _room11, 5, 6)
            .WithRoom(5203, out _room12, 6, 6)
            .WithRoom(5203, out _room13, 7, 6)
            .WithRoom(5203, out _room14, 7, 7)
            .WithRoom(5203, out _room15, 7, 8)
            .WithRoom(5203, out _room16, 7, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(1, 12), _room2, new(23, 12), locked: false);
        r.SpawnPortal(_room2, new(23, 12), _room1, new(1, 12));
        r.SpawnPortal(_room2, new(1, 12), _room3, new(23, 12));
        r.Summon(_room2,
            new MonsterSpawn(114, new(15, 21)),
            new MonsterSpawn(114, new(15, 12), OnDeath: r =>
            {
                r.Summon(_room2,
                new MonsterSpawn(114, new(6, 12)),
                new MonsterSpawn(114, new(9, 13)),
                new MonsterSpawn(114, new(9, 17)));
            }),
            new MonsterSpawn(120, new(15, 9)),
            new MonsterSpawn(120, new(13, 17), OnDeath: r =>
            {
                r.Summon(_room2,
                new MonsterSpawn(120, new(6, 14)),
                new MonsterSpawn(120, new(9, 15)));
            }),
            new MonsterSpawn(114, new(10, 12)));
        r.SummonNpcs(_room2, new NpcSpawn(311, new(15, 22)), new NpcSpawn(311, new(15, 19)), new NpcSpawn(311, new(15, 14)), new NpcSpawn(311, new(15, 11)), new NpcSpawn(311, new(15, 8)), new NpcSpawn(311, new(15, 7)));
        r.SpawnPortal(_room3, new(12, 1), _room4, new(12, 23), locked: false);
        r.SpawnPortal(_room3, new(23, 12), _room2, new(1, 12));
        r.SpawnPortal(_room4, new(12, 23), _room3, new(12, 1));
        r.SpawnPortal(_room4, new(1, 12), _room5, new(23, 12));
        r.Summon(_room4,
            new MonsterSpawn(120, new(15, 20)),
            new MonsterSpawn(120, new(15, 13)),
            new MonsterSpawn(120, new(15, 9)),
            new MonsterSpawn(117, new(11, 17)),
            new MonsterSpawn(114, new(11, 8)),
            new MonsterSpawn(114, new(10, 20)),
            new MonsterSpawn(117, new(9, 23)),
            new MonsterSpawn(114, new(9, 10)),
            new MonsterSpawn(117, new(9, 6), IsBonus: true),
            new MonsterSpawn(114, new(8, 17)),
            new MonsterSpawn(117, new(8, 12)));
        r.SummonNpcs(_room4, new NpcSpawn(311, new(14, 23)), new NpcSpawn(311, new(14, 20)), new NpcSpawn(311, new(14, 16)), new NpcSpawn(311, new(14, 13)), new NpcSpawn(311, new(14, 9)), new NpcSpawn(311, new(14, 6)));
        r.SpawnPortal(_room5, new(23, 12), _room4, new(1, 12), locked: false);
        r.SpawnPortal(_room5, new(1, 12), _room6, new(23, 12), locked: false);
        r.SpawnPortal(_room6, new(12, 1), _room7, new(12, 23));
        r.SpawnPortal(_room6, new(23, 12), _room5, new(1, 12));
        r.Summon(_room6,
            new MonsterSpawn(120, new(14, 19)),
            new MonsterSpawn(120, new(14, 11)),
            new MonsterSpawn(114, new(11, 22)),
            new MonsterSpawn(114, new(11, 21)),
            new MonsterSpawn(114, new(11, 17)),
            new MonsterSpawn(114, new(11, 14)),
            new MonsterSpawn(114, new(11, 10)),
            new MonsterSpawn(114, new(11, 7)),
            new MonsterSpawn(120, new(8, 17)),
            new MonsterSpawn(120, new(8, 15), IsBonus: true),
            new MonsterSpawn(120, new(8, 11)),
            new MonsterSpawn(117, new(6, 8)),
            new MonsterSpawn(117, new(5, 13)),
            new MonsterSpawn(117, new(5, 11)),
            new MonsterSpawn(117, new(4, 13)));
        r.SummonNpcs(_room6, new NpcSpawn(311, new(15, 23)), new NpcSpawn(311, new(15, 22)), new NpcSpawn(311, new(15, 21)), new NpcSpawn(311, new(15, 18)), new NpcSpawn(311, new(15, 14)), new NpcSpawn(311, new(15, 9)), new NpcSpawn(311, new(15, 7)), new NpcSpawn(311, new(15, 6)));
        r.SpawnPortal(_room7, new(23, 12), _room8, new(1, 12), locked: false);
        r.SpawnPortal(_room7, new(12, 23), _room6, new(12, 1), locked: false);
        r.SpawnPortal(_room8, new(23, 12), _room9, new(1, 12));
        r.SpawnPortal(_room8, new(1, 12), _room7, new(23, 12));
        r.Summon(_room8,
            new MonsterSpawn(114, new(22, 13)),
            new MonsterSpawn(114, new(22, 12)),
            new MonsterSpawn(114, new(22, 10)),
            new MonsterSpawn(114, new(20, 13)),
            new MonsterSpawn(117, new(19, 12)),
            new MonsterSpawn(117, new(14, 23)),
            new MonsterSpawn(120, new(14, 21)),
            new MonsterSpawn(120, new(14, 19)),
            new MonsterSpawn(120, new(14, 15)),
            new MonsterSpawn(120, new(14, 12), IsBonus: true),
            new MonsterSpawn(120, new(14, 8)),
            new MonsterSpawn(117, new(14, 8)),
            new MonsterSpawn(117, new(14, 6)));
        r.SummonNpcs(_room8, new NpcSpawn(311, new(12, 16)), new NpcSpawn(311, new(11, 13)), new NpcSpawn(311, new(10, 23)), new NpcSpawn(311, new(10, 6)), new NpcSpawn(311, new(9, 21)), new NpcSpawn(311, new(9, 18)), new NpcSpawn(311, new(9, 15)), new NpcSpawn(311, new(9, 11)), new NpcSpawn(311, new(9, 8)));
        r.SpawnPortal(_room9, new(23, 12), _room10, new(1, 12), locked: false);
        r.SpawnPortal(_room9, new(1, 12), _room8, new(23, 12), locked: false);
        r.SpawnPortal(_room10, new(12, 1), _room11, new(12, 23));
        r.SpawnPortal(_room10, new(1, 12), _room9, new(23, 12));
        r.Summon(_room10,
            new MonsterSpawn(120, new(22, 13), IsBonus: true),
            new MonsterSpawn(120, new(20, 14)),
            new MonsterSpawn(120, new(13, 10)),
            new MonsterSpawn(120, new(12, 21)),
            new MonsterSpawn(120, new(9, 14)));
        r.SummonNpcs(_room10, new NpcSpawn(311, new(23, 14)), new NpcSpawn(311, new(20, 13)), new NpcSpawn(311, new(15, 18)), new NpcSpawn(311, new(14, 23)), new NpcSpawn(311, new(15, 19)), new NpcSpawn(311, new(11, 8)), new NpcSpawn(311, new(10, 18)), new NpcSpawn(311, new(7, 15)), new NpcSpawn(311, new(9, 11)), new NpcSpawn(311, new(9, 7)), new NpcSpawn(311, new(9, 6)));
        r.SpawnPortal(_room11, new(23, 12), _room12, new(1, 12));
        r.SpawnPortal(_room11, new(12, 23), _room10, new(12, 1));
        r.Summon(_room11,
            new MonsterSpawn(338, new(14, 19)),
            new MonsterSpawn(338, new(14, 18)),
            new MonsterSpawn(338, new(14, 10)),
            new MonsterSpawn(338, new(13, 16)),
            new MonsterSpawn(338, new(13, 14)),
            new MonsterSpawn(338, new(13, 12)));
        r.SummonNpcs(_room11, new NpcSpawn(318, new(10, 15), IsEscort: true));
        r.SpawnPortal(_room12, new(23, 12), _room13, new(1, 12));
        r.SpawnPortal(_room12, new(1, 12), _room11, new(23, 12));
        r.Summon(_room12,
            new MonsterSpawn(338, new(12, 22)),
            new MonsterSpawn(338, new(10, 19), IsBonus: true),
            new MonsterSpawn(338, new(13, 16)),
            new MonsterSpawn(338, new(10, 11)),
            new MonsterSpawn(338, new(12, 9)));
        r.SpawnPortal(_room13, new(12, 23), _room14, new(12, 1));
        r.SpawnPortal(_room13, new(1, 12), _room12, new(23, 12));
        r.Summon(_room13,
            new MonsterSpawn(338, new(13, 15)),
            new MonsterSpawn(338, new(13, 9), OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(338, new(12, 13)),
                new MonsterSpawn(338, new(12, 21)));
            }),
            new MonsterSpawn(338, new(12, 19), OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(338, new(13, 17)));
            }),
            new MonsterSpawn(338, new(12, 12)));
        r.SpawnPortal(_room14, new(12, 1), _room13, new(12, 23));
        r.SpawnPortal(_room14, new(12, 23), _room15, new(12, 1));
        r.Summon(_room14,
            new MonsterSpawn(338, new(14, 21), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(338, new(19, 11)));
            }),
            new MonsterSpawn(338, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(338, new(15, 18), OnDeath: r =>
                {
                    r.Summon(_room14, new MonsterSpawn(338, new(20, 13), IsBonus: true));
                }));
            }),
            new MonsterSpawn(338, new(10, 21), OnDeath: r =>
            {
                r.Summon(_room14,
                new MonsterSpawn(338, new(23, 12)),
                new MonsterSpawn(338, new(23, 13)));
            }),
            new MonsterSpawn(338, new(7, 16)));
        r.SpawnPortal(_room15, new(12, 1), _room14, new(12, 23));
        r.SpawnPortal(_room15, new(12, 23), _room16, new(12, 1));
        r.Summon(_room15,
            new MonsterSpawn(338, new(13, 17), OnDeath: r =>
            {
                r.Summon(_room15, new MonsterSpawn(338, new(13, 20), OnDeath: r =>
                {
                    r.Summon(_room15,
                    new MonsterSpawn(338, new(17, 12)),
                    new MonsterSpawn(338, new(18, 13), OnDeath: r =>
                    {
                        r.Summon(_room15, new MonsterSpawn(338, new(23, 12)));
                    }),
                    new MonsterSpawn(338, new(21, 12)));
                }));
            }),
            new MonsterSpawn(338, new(9, 20), IsBonus: true),
            new MonsterSpawn(338, new(9, 17)));
        r.SpawnPortal(_room16, new(12, 1), _room15, new(12, 23));
        r.SpawnExit(_room16, new(12, 23));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6114);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6114);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6114);
        }
        else if (room == _room8)
        {
            r.Dialog(_room8, 6115);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6117);
        }
        else if (room == _room16)
        {
            r.UnlockExit(_room16);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.UnlockPortal(_room2, _room1);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room3);
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room5);
            r.UnlockPortal(_room6, _room7);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room7);
            r.UnlockPortal(_room8, _room9);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room11);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room10, 6116);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room12);
            r.AddClockTime(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room12)
        {
            r.UnlockPortal(_room12, _room13);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room13)
        {
            r.UnlockPortal(_room13, _room14);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room14)
        {
            r.UnlockPortal(_room14, _room15);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room15)
        {
            r.UnlockPortal(_room15, _room16);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace22();
