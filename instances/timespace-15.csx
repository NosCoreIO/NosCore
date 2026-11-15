public class TimeSpace15 : InstanceScript
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

    public TimeSpace15()
    {
        Definition = InstanceDefinitionBuilder.Named(15, "A Bold Warrior: Part 3", "A Bold Warrior: Part 3")
            .ForLevels(25, 99)
            .StartingAt(9, 7)
            .Requiring(1012, 2)
            .WithDraw(20, 1)
            .WithDraw(34, 1)
            .WithDraw(48, 1)
            .WithSpecialReward(800, 1)
            .WithReward(1013, 1)
            .WithReward(1087, 1)
            .WithRoom(4014, out _room1, 2, 11)
            .WithRoom(4004, out _room2, 3, 11)
            .WithRoom(4012, out _room3, 4, 11)
            .WithRoom(4012, out _room4, 4, 10)
            .WithRoom(4012, out _room5, 4, 9)
            .WithRoom(4012, out _room6, 4, 8)
            .WithRoom(4012, out _room7, 4, 7)
            .WithRoom(4012, out _room8, 4, 6)
            .WithRoom(4012, out _room9, 4, 5)
            .WithRoom(4012, out _room10, 4, 4)
            .WithRoom(4012, out _room11, 4, 3)
            .WithRoom(4012, out _room12, 4, 2)
            .WithRoom(4004, out _room13, 5, 2)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(1300));
        r.SpawnPortal(_room1, new(17, 9), _room2, new(0, 9), locked: false);
        r.SpawnPortal(_room2, new(17, 9), _room3, new(0, 9), locked: false);
        r.SpawnPortal(_room2, new(0, 9), _room1, new(17, 9), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(317, new(3, 8)));
        r.SpawnPortal(_room3, new(8, 0), _room4, new(8, 17));
        r.SpawnPortal(_room3, new(0, 9), _room2, new(17, 9));
        r.Summon(_room3,
            new MonsterSpawn(109, new(11, 6), IsBonus: true),
            new MonsterSpawn(111, new(9, 14)),
            new MonsterSpawn(109, new(9, 12)),
            new MonsterSpawn(108, new(8, 7)),
            new MonsterSpawn(105, new(10, 13)),
            new MonsterSpawn(105, new(8, 14)),
            new MonsterSpawn(108, new(10, 13)));
        r.SummonNpcs(_room3, new NpcSpawn(320, new(8, 13)), new NpcSpawn(320, new(8, 3)), new NpcSpawn(320, new(8, 9)), new NpcSpawn(320, new(7, 5)), new NpcSpawn(320, new(6, 6)), new NpcSpawn(320, new(5, 10)), new NpcSpawn(320, new(5, 8)), new NpcSpawn(337, new(3, 9)));
        r.SpawnPortal(_room4, new(8, 0), _room5, new(8, 17));
        r.SpawnPortal(_room4, new(8, 17), _room3, new(8, 0));
        r.Summon(_room4,
            new MonsterSpawn(109, new(11, 9)),
            new MonsterSpawn(109, new(11, 6), IsBonus: true),
            new MonsterSpawn(111, new(9, 3)),
            new MonsterSpawn(108, new(8, 9)),
            new MonsterSpawn(108, new(8, 7)));
        r.SummonNpcs(_room4, new NpcSpawn(320, new(8, 13)), new NpcSpawn(320, new(8, 3)), new NpcSpawn(320, new(7, 11)), new NpcSpawn(320, new(7, 5)), new NpcSpawn(320, new(6, 6)), new NpcSpawn(320, new(5, 10)), new NpcSpawn(320, new(5, 8)));
        r.SpawnPortal(_room5, new(8, 0), _room6, new(8, 17));
        r.SpawnPortal(_room5, new(8, 17), _room4, new(8, 0));
        r.Summon(_room5,
            new MonsterSpawn(109, new(9, 5)),
            new MonsterSpawn(111, new(9, 3)),
            new MonsterSpawn(108, new(8, 9)),
            new MonsterSpawn(108, new(8, 7)));
        r.SummonNpcs(_room5, new NpcSpawn(320, new(8, 13)), new NpcSpawn(320, new(8, 3)), new NpcSpawn(320, new(7, 11)), new NpcSpawn(320, new(7, 5)), new NpcSpawn(320, new(6, 6)), new NpcSpawn(320, new(5, 10)), new NpcSpawn(320, new(5, 8)));
        r.SpawnPortal(_room6, new(8, 0), _room7, new(8, 17));
        r.SpawnPortal(_room6, new(8, 17), _room5, new(8, 0));
        r.Summon(_room6,
            new MonsterSpawn(108, new(16, 9)),
            new MonsterSpawn(105, new(13, 7), IsBonus: true),
            new MonsterSpawn(111, new(11, 11)),
            new MonsterSpawn(105, new(11, 7)),
            new MonsterSpawn(111, new(11, 6)),
            new MonsterSpawn(111, new(3, 8), IsBonus: true),
            new MonsterSpawn(108, new(2, 9)),
            new MonsterSpawn(105, new(11, 10)),
            new MonsterSpawn(105, new(10, 12)),
            new MonsterSpawn(108, new(11, 10)),
            new MonsterSpawn(105, new(3, 7)),
            new MonsterSpawn(105, new(2, 8)),
            new MonsterSpawn(108, new(4, 9)),
            new MonsterSpawn(105, new(12, 7)),
            new MonsterSpawn(105, new(11, 5)),
            new MonsterSpawn(108, new(11, 6)));
        r.SummonNpcs(_room6, new NpcSpawn(320, new(14, 9)), new NpcSpawn(320, new(12, 10)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(320, new(9, 10)), new NpcSpawn(320, new(9, 7)), new NpcSpawn(320, new(7, 7)), new NpcSpawn(320, new(6, 10)), new NpcSpawn(320, new(4, 9)), new NpcSpawn(320, new(6, 7)));
        r.SpawnPortal(_room7, new(8, 0), _room8, new(8, 17));
        r.SpawnPortal(_room7, new(8, 17), _room6, new(8, 0));
        r.Summon(_room7,
            new MonsterSpawn(111, new(11, 11)),
            new MonsterSpawn(105, new(11, 7)),
            new MonsterSpawn(111, new(11, 6)),
            new MonsterSpawn(111, new(10, 9)),
            new MonsterSpawn(111, new(6, 5)),
            new MonsterSpawn(108, new(2, 9)),
            new MonsterSpawn(105, new(10, 10)),
            new MonsterSpawn(105, new(11, 11)),
            new MonsterSpawn(108, new(10, 9)),
            new MonsterSpawn(105, new(8, 9)),
            new MonsterSpawn(105, new(7, 7)),
            new MonsterSpawn(108, new(9, 7)));
        r.SummonNpcs(_room7, new NpcSpawn(320, new(14, 9)), new NpcSpawn(320, new(12, 10)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(320, new(9, 10)), new NpcSpawn(320, new(9, 7)), new NpcSpawn(320, new(7, 7)), new NpcSpawn(320, new(6, 10)), new NpcSpawn(320, new(4, 9)), new NpcSpawn(320, new(3, 7)));
        r.SpawnPortal(_room8, new(8, 0), _room9, new(8, 17));
        r.SpawnPortal(_room8, new(8, 17), _room7, new(8, 0));
        r.Summon(_room8,
            new MonsterSpawn(109, new(9, 5)),
            new MonsterSpawn(111, new(9, 3)),
            new MonsterSpawn(108, new(8, 9)),
            new MonsterSpawn(108, new(8, 7)),
            new MonsterSpawn(105, new(8, 2)),
            new MonsterSpawn(105, new(9, 2)),
            new MonsterSpawn(108, new(9, 3)));
        r.SummonNpcs(_room8, new NpcSpawn(320, new(8, 13)), new NpcSpawn(320, new(8, 3)), new NpcSpawn(320, new(7, 11)), new NpcSpawn(320, new(7, 5)), new NpcSpawn(320, new(6, 6)), new NpcSpawn(320, new(5, 10)), new NpcSpawn(320, new(5, 8)));
        r.SpawnPortal(_room9, new(8, 0), _room10, new(8, 17));
        r.SpawnPortal(_room9, new(8, 17), _room8, new(8, 0));
        r.Summon(_room9,
            new MonsterSpawn(108, new(16, 9), IsBonus: true),
            new MonsterSpawn(105, new(13, 7)),
            new MonsterSpawn(111, new(11, 11)),
            new MonsterSpawn(105, new(11, 7)),
            new MonsterSpawn(111, new(6, 12)),
            new MonsterSpawn(111, new(6, 5)),
            new MonsterSpawn(108, new(2, 9)),
            new MonsterSpawn(105, new(11, 12)),
            new MonsterSpawn(105, new(11, 10)),
            new MonsterSpawn(108, new(12, 11)));
        r.SummonNpcs(_room9, new NpcSpawn(320, new(14, 9)), new NpcSpawn(320, new(12, 10)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(320, new(9, 10)), new NpcSpawn(320, new(9, 7)), new NpcSpawn(320, new(7, 7)), new NpcSpawn(320, new(6, 10)), new NpcSpawn(320, new(4, 9)), new NpcSpawn(320, new(3, 7)));
        r.SpawnPortal(_room10, new(8, 0), _room11, new(8, 17));
        r.SpawnPortal(_room10, new(8, 17), _room9, new(8, 0));
        r.Summon(_room10,
            new MonsterSpawn(108, new(16, 9)),
            new MonsterSpawn(105, new(13, 7)),
            new MonsterSpawn(111, new(11, 11), IsBonus: true),
            new MonsterSpawn(111, new(6, 12)),
            new MonsterSpawn(111, new(6, 5)),
            new MonsterSpawn(108, new(2, 9)),
            new MonsterSpawn(105, new(14, 9)),
            new MonsterSpawn(105, new(13, 9)),
            new MonsterSpawn(108, new(14, 9)),
            new MonsterSpawn(105, new(11, 7)),
            new MonsterSpawn(105, new(11, 5)),
            new MonsterSpawn(108, new(10, 6)));
        r.SummonNpcs(_room10, new NpcSpawn(320, new(14, 9)), new NpcSpawn(320, new(12, 10)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(320, new(9, 10)), new NpcSpawn(320, new(9, 7)), new NpcSpawn(320, new(7, 7)), new NpcSpawn(320, new(6, 10)), new NpcSpawn(320, new(4, 9)), new NpcSpawn(320, new(3, 7)));
        r.SpawnPortal(_room11, new(8, 0), _room12, new(8, 17));
        r.SpawnPortal(_room11, new(8, 17), _room10, new(8, 0));
        r.Summon(_room11,
            new MonsterSpawn(108, new(16, 9)),
            new MonsterSpawn(111, new(10, 9), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(105, new(5, 9)),
                new MonsterSpawn(105, new(5, 8)),
                new MonsterSpawn(108, new(4, 9), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(112, new(8, 9)));
                }));
            }),
            new MonsterSpawn(105, new(10, 8), IsBonus: true),
            new MonsterSpawn(111, new(9, 4)),
            new MonsterSpawn(111, new(6, 12)),
            new MonsterSpawn(111, new(6, 5)),
            new MonsterSpawn(108, new(2, 9)),
            new MonsterSpawn(105, new(9, 8)),
            new MonsterSpawn(105, new(9, 10)),
            new MonsterSpawn(108, new(11, 10)),
            new MonsterSpawn(105, new(9, 7)),
            new MonsterSpawn(105, new(9, 5)),
            new MonsterSpawn(108, new(10, 6)));
        r.SummonNpcs(_room11, new NpcSpawn(320, new(14, 9)), new NpcSpawn(320, new(12, 10)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(320, new(9, 10)), new NpcSpawn(320, new(9, 7)), new NpcSpawn(320, new(7, 7)), new NpcSpawn(320, new(6, 10)), new NpcSpawn(320, new(4, 9)), new NpcSpawn(320, new(3, 7)));
        r.SpawnPortal(_room12, new(17, 9), _room13, new(0, 9), locked: false);
        r.SpawnPortal(_room12, new(8, 17), _room11, new(8, 0), locked: false);
        r.SummonNpcs(_room12, new NpcSpawn(320, new(12, 8)), new NpcSpawn(320, new(11, 10)), new NpcSpawn(111, new(10, 8)), new NpcSpawn(320, new(10, 6)), new NpcSpawn(320, new(9, 12)), new NpcSpawn(111, new(9, 8)), new NpcSpawn(337, new(8, 11)), new NpcSpawn(111, new(8, 9)), new NpcSpawn(111, new(8, 7)), new NpcSpawn(320, new(8, 5)), new NpcSpawn(111, new(7, 10)), new NpcSpawn(111, new(7, 8)), new NpcSpawn(320, new(6, 11)), new NpcSpawn(320, new(8, 6)), new NpcSpawn(320, new(5, 8)));
        r.SpawnExit(_room13, new(17, 9), locked: false);
        r.SpawnPortal(_room13, new(0, 9), _room12, new(17, 9), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6189);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6190);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6190);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6190);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6190);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6190);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.AsgobasInstantCombatAboutToStart);
            r.Dialog(_room8, 6190);
        }
        else if (room == _room9)
        {
            r.Dialog(_room9, 6190);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6191);
        }
        else if (room == _room11)
        {
            r.Dialog(_room11, 6192);
        }
        else if (room == _room12)
        {
            r.Dialog(_room12, 6193);
        }
        else if (room == _room13)
        {
            r.Dialog(_room13, 6194);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room2);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.UnlockPortal(_room5, _room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.UnlockPortal(_room6, _room5);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room7)
        {
            r.UnlockPortal(_room7, _room8);
            r.UnlockPortal(_room7, _room6);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room7);
            r.UnlockPortal(_room8, _room9);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room10);
            r.UnlockPortal(_room9, _room8);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room10)
        {
            r.UnlockPortal(_room10, _room9);
            r.UnlockPortal(_room10, _room11);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room11)
        {
            r.UnlockPortal(_room11, _room10);
            r.UnlockPortal(_room11, _room12);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace15();
