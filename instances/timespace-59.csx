// Shanera's Dungeon: Part 3
public class TimeSpace59 : InstanceScript
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
    private readonly int _room19;

    public TimeSpace59()
    {
        Definition = InstanceDefinitionBuilder.Named(59, "Cenil and Catrisha have joined the fight! Only yesterday they were enemies, but now they share one goal. Will they be reconciled for good?", "Shanera's Dungeon: Part 3")
            .ForLevels(64, 99)
            .StartingAt(15, 7)
            .WithLives(1)
            .Requiring(1012, 6)
            .WithSpecialReward(2048, 30)
            .WithReward(1032, 1)
            .WithReward(1087, 1)
            .WithRoom(4800, out _room1, 0, 4)
            .WithRoom(2100, out _room2, 0, 4)
            .WithRoom(4807, out _room3, 0, 5)
            .WithRoom(4807, out _room4, 0, 6)
            .WithRoom(4807, out _room5, 0, 7)
            .WithRoom(4811, out _room6, 0, 8)
            .WithRoom(4805, out _room7, 0, 9)
            .WithRoom(4805, out _room8, 1, 9)
            .WithRoom(4805, out _room9, 2, 9)
            .WithRoom(4805, out _room10, 3, 9)
            .WithRoom(4805, out _room11, 4, 9)
            .WithRoom(4805, out _room12, 5, 9)
            .WithRoom(4805, out _room13, 6, 9)
            .WithRoom(4805, out _room14, 7, 9)
            .WithRoom(4810, out _room15, 8, 9)
            .WithRoom(4807, out _room16, 9, 9)
            .WithRoom(4807, out _room17, 9, 8)
            .WithRoom(4807, out _room18, 9, 7)
            .WithRoom(4807, out _room19, 9, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(15, 28), _room2, new(34, 11), locked: false);
        r.SpawnPortal(_room2, new(32, 59), _room3, new(15, 0));
        r.SpawnPortal(_room2, new(34, 11), _room1, new(15, 28));
        r.Summon(_room2,
            new MonsterSpawn(315, new(34, 41), IsBonus: true, OnDeath: r =>
            {
                r.Dialog(_room2, 6297);
                r.AddClockTime(TimeSpan.FromSeconds(120));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room2, _room3);
                r.UnlockPortal(_room2, _room1);
            }),
            new MonsterSpawn(122, new(17, 27), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(44, 34), OnDeath: r =>
                {
                    r.Summon(_room2,
                    new MonsterSpawn(122, new(30, 21)),
                    new MonsterSpawn(122, new(25, 20), IsBonus: true));
                }));
            }),
            new MonsterSpawn(122, new(25, 27), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(25, 34), OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(122, new(52, 39), OnDeath: r =>
                    {
                        r.Summon(_room2,
                        new MonsterSpawn(122, new(45, 20)),
                        new MonsterSpawn(122, new(40, 21)),
                        new MonsterSpawn(122, new(35, 21)));
                    }));
                }));
            }),
            new MonsterSpawn(122, new(29, 27), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(56, 47)));
            }),
            new MonsterSpawn(122, new(35, 27), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(31, 33), OnDeath: r =>
                {
                    r.Summon(_room2,
                    new MonsterSpawn(122, new(15, 37), IsBonus: true),
                    new MonsterSpawn(122, new(11, 43)));
                }));
            }),
            new MonsterSpawn(122, new(41, 27), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(48, 27), IsBonus: true));
            }),
            new MonsterSpawn(122, new(44, 27), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(122, new(54, 27), OnDeath: r =>
                {
                    r.Summon(_room2, new MonsterSpawn(122, new(50, 35)));
                }));
            }));
        r.SpawnPortal(_room3, new(15, 28), _room4, new(15, 0), locked: false);
        r.SpawnPortal(_room3, new(15, 0), _room2, new(32, 59), locked: false);
        r.SpawnPortal(_room4, new(15, 28), _room5, new(15, 0));
        r.SpawnPortal(_room4, new(15, 0), _room3, new(15, 28));
        r.Summon(_room4,
            new MonsterSpawn(240, new(10, 10)),
            new MonsterSpawn(243, new(11, 20), IsBonus: true),
            new MonsterSpawn(240, new(14, 10), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(243, new(19, 21), IsBonus: true));
            }),
            new MonsterSpawn(243, new(14, 20)),
            new MonsterSpawn(228, new(15, 17), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(265, new(21, 15)));
            }),
            new MonsterSpawn(240, new(18, 10)),
            new MonsterSpawn(240, new(21, 10), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(265, new(9, 15), IsBonus: true));
            }));
        r.SpawnPortal(_room5, new(15, 28), _room6, new(15, 0), locked: false);
        r.SpawnPortal(_room5, new(15, 0), _room4, new(15, 28), locked: false);
        r.SpawnPortal(_room6, new(29, 14), _room7, new(1, 14));
        r.SpawnPortal(_room6, new(15, 0), _room5, new(15, 28));
        r.Summon(_room6,
            new MonsterSpawn(205, new(13, 7)),
            new MonsterSpawn(240, new(16, 11), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(246, new(13, 18)),
                new MonsterSpawn(246, new(11, 15), IsBonus: true));
            }),
            new MonsterSpawn(240, new(17, 13)),
            new MonsterSpawn(240, new(18, 11), OnDeath: r =>
            {
                r.Summon(_room6,
                new MonsterSpawn(243, new(17, 18), IsBonus: true),
                new MonsterSpawn(243, new(14, 14)));
            }),
            new MonsterSpawn(240, new(19, 13), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(243, new(11, 11)));
            }),
            new MonsterSpawn(205, new(21, 9)),
            new MonsterSpawn(205, new(22, 15), IsBonus: true));
        r.SpawnPortal(_room7, new(1, 14), _room6, new(29, 14), locked: false);
        r.SpawnPortal(_room7, new(28, 14), _room8, new(1, 14), locked: false);
        r.SpawnPortal(_room8, new(1, 14), _room7, new(28, 14), locked: false);
        r.SpawnPortal(_room8, new(28, 14), _room9, new(1, 14), locked: false);
        r.SpawnPortal(_room9, new(1, 14), _room8, new(28, 14));
        r.SpawnPortal(_room9, new(28, 14), _room10, new(1, 14));
        r.Summon(_room9,
            new MonsterSpawn(235, new(10, 20)),
            new MonsterSpawn(235, new(11, 8)),
            new MonsterSpawn(235, new(13, 8)),
            new MonsterSpawn(235, new(13, 20)),
            new MonsterSpawn(238, new(15, 10)),
            new MonsterSpawn(238, new(15, 13)),
            new MonsterSpawn(238, new(15, 15)),
            new MonsterSpawn(238, new(15, 17)),
            new MonsterSpawn(238, new(15, 20), IsBonus: true),
            new MonsterSpawn(235, new(16, 8)),
            new MonsterSpawn(235, new(17, 20), IsBonus: true),
            new MonsterSpawn(246, new(18, 11)),
            new MonsterSpawn(246, new(18, 15)),
            new MonsterSpawn(235, new(21, 9)),
            new MonsterSpawn(235, new(19, 19)));
        r.SpawnPortal(_room10, new(1, 14), _room9, new(28, 14), locked: false);
        r.SpawnPortal(_room10, new(28, 14), _room11, new(1, 14), locked: false);
        r.SpawnPortal(_room11, new(1, 14), _room10, new(28, 14));
        r.SpawnPortal(_room11, new(28, 14), _room12, new(1, 14));
        r.Summon(_room11,
            new MonsterSpawn(240, new(10, 10), OnDeath: r =>
            {
                r.Summon(_room11,
                new MonsterSpawn(231, new(13, 16), OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(265, new(9, 15), OnDeath: r =>
                    {
                        r.Summon(_room11, new MonsterSpawn(265, new(21, 15), IsBonus: true));
                    }));
                }),
                new MonsterSpawn(243, new(19, 21), IsBonus: true));
            }),
            new MonsterSpawn(243, new(13, 18), IsBonus: true),
            new MonsterSpawn(240, new(14, 10), IsBonus: true),
            new MonsterSpawn(243, new(14, 20)),
            new MonsterSpawn(228, new(15, 17)),
            new MonsterSpawn(240, new(18, 10)),
            new MonsterSpawn(240, new(21, 10), IsBonus: true));
        r.SpawnPortal(_room12, new(1, 14), _room11, new(28, 14));
        r.SpawnPortal(_room12, new(28, 14), _room13, new(1, 14));
        r.Summon(_room12,
            new MonsterSpawn(246, new(13, 18)),
            new MonsterSpawn(246, new(11, 15)),
            new MonsterSpawn(243, new(11, 11)),
            new MonsterSpawn(243, new(17, 18)),
            new MonsterSpawn(243, new(14, 14)),
            new MonsterSpawn(205, new(15, 9)),
            new MonsterSpawn(240, new(16, 11)),
            new MonsterSpawn(240, new(17, 13)),
            new MonsterSpawn(240, new(18, 11)),
            new MonsterSpawn(240, new(19, 13)),
            new MonsterSpawn(205, new(21, 9)),
            new MonsterSpawn(205, new(22, 15), IsBonus: true));
        r.SpawnPortal(_room13, new(1, 14), _room12, new(28, 14), locked: false);
        r.SpawnPortal(_room13, new(28, 14), _room14, new(1, 14), locked: false);
        r.SpawnPortal(_room14, new(1, 14), _room13, new(28, 14), locked: false);
        r.SpawnPortal(_room14, new(28, 14), _room15, new(1, 14), locked: false);
        r.SpawnPortal(_room15, new(1, 14), _room14, new(28, 14));
        r.SpawnPortal(_room15, new(15, 0), _room16, new(15, 28));
        r.Summon(_room15,
            new MonsterSpawn(265, new(21, 15)),
            new MonsterSpawn(231, new(9, 15)),
            new MonsterSpawn(265, new(9, 15)),
            new MonsterSpawn(243, new(19, 21)),
            new MonsterSpawn(240, new(10, 10)),
            new MonsterSpawn(243, new(11, 20)),
            new MonsterSpawn(240, new(14, 10)),
            new MonsterSpawn(243, new(16, 19)),
            new MonsterSpawn(228, new(15, 17)),
            new MonsterSpawn(240, new(18, 10), IsBonus: true),
            new MonsterSpawn(240, new(21, 10), IsBonus: true));
        r.SpawnPortal(_room16, new(15, 28), _room15, new(15, 0), locked: false);
        r.SpawnPortal(_room16, new(15, 0), _room17, new(15, 28), locked: false);
        r.SpawnPortal(_room17, new(15, 28), _room16, new(15, 0));
        r.SpawnPortal(_room17, new(15, 0), _room18, new(15, 28));
        r.Summon(_room17,
            new MonsterSpawn(243, new(7, 7)),
            new MonsterSpawn(243, new(12, 17), IsBonus: true),
            new MonsterSpawn(243, new(13, 8)),
            new MonsterSpawn(258, new(14, 16)),
            new MonsterSpawn(258, new(14, 17)),
            new MonsterSpawn(258, new(14, 18), OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(243, new(19, 16)));
            }),
            new MonsterSpawn(258, new(15, 16)),
            new MonsterSpawn(258, new(15, 17), OnDeath: r =>
            {
                r.Summon(_room17,
                new MonsterSpawn(243, new(22, 9), IsBonus: true),
                new MonsterSpawn(243, new(18, 8), IsBonus: true));
            }),
            new MonsterSpawn(258, new(15, 18), OnDeath: r =>
            {
                r.Summon(_room17, new MonsterSpawn(243, new(9, 9), IsBonus: true));
            }),
            new MonsterSpawn(243, new(21, 9)));
        r.SpawnPortal(_room18, new(15, 28), _room17, new(15, 0), locked: false);
        r.SpawnPortal(_room18, new(15, 0), _room19, new(15, 28), locked: false);
        r.SpawnPortal(_room19, new(15, 28), _room18, new(15, 0), locked: false);
        r.SpawnExit(_room19, new(15, 0), locked: false);
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
            r.Dialog(_room2, 6296);
        }
        else if (room == _room19)
        {
            r.Dialog(_room19, 6298);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
        }
        else if (room == _room6)
        {
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room6, _room7);
        }
        else if (room == _room9)
        {
            r.AddClockTime(TimeSpan.FromSeconds(90));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room11)
        {
            r.AddClockTime(TimeSpan.FromSeconds(90));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room12);
        }
        else if (room == _room12)
        {
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room13);
        }
        else if (room == _room15)
        {
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room15, _room16);
        }
        else if (room == _room17)
        {
            r.AddClockTime(TimeSpan.FromSeconds(70));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room17, _room18);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace59();
