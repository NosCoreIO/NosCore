// Shanera's Dungeon: Part 2
public class TimeSpace58 : InstanceScript
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
    private readonly int _room20;
    private readonly int _room21;
    private readonly int _room22;
    private readonly int _room23;
    private readonly int _room24;
    private readonly int _room25;
    private readonly int _room26;
    private readonly int _room27;
    private readonly int _room28;
    private readonly int _room29;
    private readonly int _room30;
    private readonly int _room31;
    private readonly int _room32;
    private readonly int _room33;
    private readonly int _room34;
    private readonly int _room35;
    private readonly int _room36;
    private readonly int _room37;
    private readonly int _room38;
    private readonly int _room39;
    private readonly int _room40;
    private readonly int _room41;
    private readonly int _room42;
    private readonly int _room43;
    private readonly int _room44;
    private readonly int _room45;

    public TimeSpace58()
    {
        Definition = InstanceDefinitionBuilder.Named(58, "Korrkoaren and Princess Catrisha have signed a peace treaty! The old grudges between Kovolts and Catsys may soon be forgotten. What will become of Princess Catlotte? Will you save her?", "Shanera's Dungeon: Part 2")
            .ForLevels(63, 99)
            .StartingAt(11, 10)
            .Requiring(1012, 6)
            .WithSpecialReward(2048, 30)
            .WithReward(1034, 1)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 5)
            .WithRoom(4809, out _room2, 0, 5)
            .WithRoom(4802, out _room3, 1, 5)
            .WithRoom(4804, out _room4, 1, 6)
            .WithRoom(4815, out _room5, 2, 6)
            .WithRoom(4804, out _room6, 2, 5)
            .WithRoom(4803, out _room7, 2, 7)
            .WithRoom(4812, out _room8, 2, 8)
            .WithRoom(4803, out _room9, 1, 8)
            .WithRoom(4802, out _room10, 1, 7)
            .WithRoom(4810, out _room11, 3, 8)
            .WithRoom(4812, out _room12, 4, 8)
            .WithRoom(4805, out _room13, 4, 7)
            .WithRoom(4812, out _room14, 5, 7)
            .WithRoom(4806, out _room15, 1, 9)
            .WithRoom(4806, out _room16, 2, 9)
            .WithRoom(4806, out _room17, 3, 9)
            .WithRoom(4803, out _room18, 4, 9)
            .WithRoom(4812, out _room19, 5, 9)
            .WithRoom(4810, out _room20, 5, 8)
            .WithRoom(4809, out _room21, 6, 8)
            .WithRoom(4805, out _room22, 6, 7)
            .WithRoom(4805, out _room23, 6, 9)
            .WithRoom(4803, out _room24, 7, 9)
            .WithRoom(4809, out _room25, 8, 9)
            .WithRoom(4814, out _room26, 8, 8)
            .WithRoom(4809, out _room27, 7, 8)
            .WithRoom(4807, out _room28, 9, 9)
            .WithRoom(4804, out _room29, 9, 8)
            .WithRoom(4814, out _room30, 9, 7)
            .WithRoom(4804, out _room31, 8, 7)
            .WithRoom(4805, out _room32, 9, 6)
            .WithRoom(4811, out _room33, 8, 6)
            .WithRoom(4805, out _room34, 7, 6)
            .WithRoom(4807, out _room35, 10, 9)
            .WithRoom(4807, out _room36, 10, 8)
            .WithRoom(4807, out _room37, 10, 7)
            .WithRoom(4809, out _room38, 10, 6)
            .WithRoom(4803, out _room39, 10, 5)
            .WithRoom(4805, out _room40, 9, 5)
            .WithRoom(4801, out _room41, 8, 5)
            .WithRoom(4812, out _room42, 7, 5)
            .WithRoom(4813, out _room43, 6, 5)
            .WithRoom(4812, out _room44, 6, 6)
            .WithRoom(4812, out _room45, 9, 4)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14), locked: false);
        r.SpawnPortal(_room2, new(15, 28), _room3, new(15, 0), locked: false);
        r.SpawnPortal(_room3, new(29, 14), _room4, new(1, 14), locked: false);
        r.SpawnPortal(_room3, new(15, 0), _room2, new(15, 28), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(237, new(12, 15)),
            new MonsterSpawn(237, new(17, 20), IsBonus: true),
            new MonsterSpawn(205, new(18, 13)));
        r.SpawnPortal(_room4, new(1, 14), _room3, new(29, 14), locked: false);
        r.SpawnPortal(_room4, new(15, 0), _room5, new(15, 28), locked: false);
        r.SpawnPortal(_room5, new(15, 28), _room6, new(15, 0));
        r.Summon(_room5,
            new MonsterSpawn(245, new(8, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(245, new(22, 12), IsBonus: true));
            }),
            new MonsterSpawn(245, new(10, 12)),
            new MonsterSpawn(225, new(12, 16), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(245, new(20, 13)));
            }),
            new MonsterSpawn(225, new(16, 16)),
            new MonsterSpawn(225, new(18, 16), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(234, new(15, 12), IsBonus: true),
                new MonsterSpawn(234, new(14, 12), OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(243, new(21, 19), IsBonus: true),
                    new MonsterSpawn(243, new(9, 18)));
                }));
            }));
        r.SpawnPortal(_room6, new(15, 28), _room6, new(15, 28), locked: false);
        r.SpawnPortal(_room6, new(15, 0), _room5, new(15, 28), locked: false);
        r.SpawnPortal(_room7, new(1, 14), _room8, new(29, 14), locked: false);
        r.SpawnPortal(_room7, new(15, 0), _room6, new(15, 0), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(243, new(9, 11), IsBonus: true),
            new MonsterSpawn(243, new(11, 13)),
            new MonsterSpawn(237, new(15, 3), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(237, new(17, 14)));
            }),
            new MonsterSpawn(243, new(18, 14)));
        r.SpawnPortal(_room8, new(29, 14), _room9, new(1, 14), locked: false);
        r.SpawnPortal(_room9, new(1, 14), _room8, new(29, 14), locked: false);
        r.SpawnPortal(_room9, new(15, 0), _room9, new(15, 0), locked: false);
        r.SpawnPortal(_room10, new(15, 28), _room9, new(15, 0), locked: false);
        r.SpawnPortal(_room10, new(29, 14), _room10, new(29, 14), locked: false);
        r.SpawnPortal(_room11, new(1, 14), _room10, new(29, 0), locked: false);
        r.SpawnPortal(_room11, new(15, 0), _room12, new(15, 28), locked: false);
        r.SpawnPortal(_room12, new(15, 28), _room11, new(15, 0));
        r.SpawnPortal(_room12, new(29, 14), _room13, new(1, 14));
        r.Summon(_room12,
            new MonsterSpawn(237, new(13, 5), IsBonus: true),
            new MonsterSpawn(237, new(18, 7), IsBonus: true),
            new MonsterSpawn(237, new(19, 21)),
            new MonsterSpawn(237, new(22, 18), OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(228, new(11, 19), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(231, new(12, 19), OnDeath: r =>
                    {
                        r.Summon(_room12, new MonsterSpawn(265, new(20, 12), OnDeath: r =>
                        {
                            r.Summon(_room12, new MonsterSpawn(265, new(9, 14)));
                        }));
                    }));
                }),
                new MonsterSpawn(228, new(11, 17), OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(228, new(11, 9), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room13, new(1, 14), _room14, new(29, 14), locked: false);
        r.SpawnPortal(_room14, new(15, 28), _room14, new(15, 28), locked: false);
        r.SpawnPortal(_room14, new(29, 14), _room13, new(1, 14), locked: false);
        r.SpawnPortal(_room15, new(1, 14), _room14, new(29, 0), locked: false);
        r.SpawnPortal(_room15, new(29, 14), _room16, new(1, 14), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room15, new(29, 14));
        r.SpawnPortal(_room16, new(29, 14), _room17, new(1, 14));
        r.Summon(_room16,
            new MonsterSpawn(237, new(19, 7)),
            new MonsterSpawn(237, new(19, 5), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room16,
                new MonsterSpawn(237, new(15, 9), IsBonus: true),
                new MonsterSpawn(237, new(14, 13)),
                new MonsterSpawn(237, new(14, 10), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room16,
                    new MonsterSpawn(237, new(19, 21), IsBonus: true),
                    new MonsterSpawn(237, new(17, 20)),
                    new MonsterSpawn(237, new(15, 18)),
                    new MonsterSpawn(237, new(14, 17)));
                }));
            }));
        r.SpawnPortal(_room17, new(1, 14), _room16, new(29, 14), locked: false);
        r.SpawnPortal(_room17, new(29, 14), _room18, new(1, 14), locked: false);
        r.SpawnPortal(_room18, new(1, 14), _room17, new(29, 14), locked: false);
        r.SpawnPortal(_room18, new(15, 0), _room19, new(15, 28), locked: false);
        r.Summon(_room18,
            new MonsterSpawn(237, new(23, 15), IsBonus: true),
            new MonsterSpawn(237, new(21, 21), IsBonus: true),
            new MonsterSpawn(205, new(22, 16), IsBonus: true));
        r.SpawnPortal(_room19, new(15, 28), _room18, new(15, 0));
        r.SpawnPortal(_room19, new(29, 14), _room20, new(1, 14), locked: false);
        r.SpawnPortal(_room20, new(1, 14), _room19, new(29, 14), locked: false);
        r.SpawnPortal(_room20, new(15, 0), _room21, new(15, 28), locked: false);
        r.Summon(_room20,
            new MonsterSpawn(243, new(16, 17), OnDeath: r =>
            {
                r.Summon(_room20, new MonsterSpawn(237, new(17, 14)));
            }),
            new MonsterSpawn(237, new(15, 8), IsBonus: true),
            new MonsterSpawn(243, new(16, 13), OnDeath: r =>
            {
                r.Summon(_room20, new MonsterSpawn(243, new(9, 11)));
            }));
        r.SpawnPortal(_room21, new(1, 14), _room22, new(28, 14), locked: false);
        r.SpawnPortal(_room21, new(15, 28), _room20, new(15, 0), locked: false);
        r.Summon(_room21,
            new MonsterSpawn(237, new(15, 21)),
            new MonsterSpawn(237, new(16, 18), IsBonus: true),
            new MonsterSpawn(205, new(21, 13), IsBonus: true));
        r.SpawnPortal(_room22, new(1, 14), _room22, new(1, 14), locked: false);
        r.SpawnPortal(_room22, new(28, 14), _room21, new(1, 14), locked: false);
        r.SpawnPortal(_room23, new(1, 14), _room22, new(28, 0), locked: false);
        r.SpawnPortal(_room23, new(28, 14), _room24, new(1, 14), locked: false);
        r.Summon(_room23,
            new MonsterSpawn(237, new(5, 16), IsBonus: true),
            new MonsterSpawn(237, new(17, 19)),
            new MonsterSpawn(205, new(22, 9), IsBonus: true));
        r.SpawnPortal(_room24, new(1, 14), _room23, new(28, 14), locked: false);
        r.SpawnPortal(_room24, new(15, 0), _room25, new(15, 28), locked: false);
        r.Summon(_room24,
            new MonsterSpawn(243, new(10, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room24, new MonsterSpawn(243, new(9, 11)));
            }),
            new MonsterSpawn(237, new(4, 4), OnDeath: r =>
            {
                r.Summon(_room24, new MonsterSpawn(237, new(17, 14), IsBonus: true));
            }),
            new MonsterSpawn(243, new(17, 24), IsBonus: true));
        r.SpawnPortal(_room25, new(1, 14), _room26, new(29, 14), locked: false);
        r.SpawnPortal(_room25, new(15, 28), _room24, new(15, 0), locked: false);
        r.SpawnPortal(_room26, new(29, 14), _room27, new(1, 14));
        r.Summon(_room26,
            new MonsterSpawn(237, new(18, 6), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room26,
                new MonsterSpawn(237, new(15, 9)),
                new MonsterSpawn(237, new(14, 13)),
                new MonsterSpawn(237, new(14, 10), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room26,
                    new MonsterSpawn(237, new(19, 21)),
                    new MonsterSpawn(237, new(17, 20), IsBonus: true),
                    new MonsterSpawn(237, new(15, 18)),
                    new MonsterSpawn(237, new(14, 17), IsBonus: true));
                }));
            }),
            new MonsterSpawn(237, new(20, 6)));
        r.SpawnPortal(_room27, new(1, 14), _room26, new(29, 14), locked: false);
        r.SpawnPortal(_room27, new(15, 28), _room27, new(15, 28), locked: false);
        r.SpawnPortal(_room28, new(15, 28), _room27, new(15, 0), locked: false);
        r.SpawnPortal(_room28, new(15, 0), _room29, new(15, 28), locked: false);
        r.Summon(_room28,
            new MonsterSpawn(243, new(5, 4), IsBonus: true),
            new MonsterSpawn(237, new(26, 15), OnDeath: r =>
            {
                r.Summon(_room28, new MonsterSpawn(243, new(9, 11)));
            }),
            new MonsterSpawn(243, new(21, 5), OnDeath: r =>
            {
                r.Summon(_room28, new MonsterSpawn(237, new(17, 14)));
            }));
        r.SpawnPortal(_room29, new(1, 14), _room30, new(29, 14), locked: false);
        r.SpawnPortal(_room29, new(15, 28), _room28, new(15, 0), locked: false);
        r.SpawnPortal(_room30, new(29, 14), _room31, new(1, 14));
        r.Summon(_room30,
            new MonsterSpawn(220, new(13, 14), IsBonus: true),
            new MonsterSpawn(218, new(13, 22)),
            new MonsterSpawn(218, new(14, 7)),
            new MonsterSpawn(218, new(16, 8)),
            new MonsterSpawn(218, new(16, 20)),
            new MonsterSpawn(218, new(17, 12)),
            new MonsterSpawn(218, new(17, 17)));
        r.SpawnPortal(_room31, new(1, 14), _room30, new(29, 14), locked: false);
        r.SpawnPortal(_room31, new(15, 0), _room31, new(15, 0), locked: false);
        r.SpawnPortal(_room32, new(1, 14), _room33, new(29, 14), locked: false);
        r.SpawnPortal(_room32, new(28, 14), _room31, new(1, 0), locked: false);
        r.SpawnPortal(_room33, new(29, 14), _room34, new(1, 14), locked: false);
        r.Summon(_room33,
            new MonsterSpawn(243, new(26, 23), OnDeath: r =>
            {
                r.Summon(_room33, new MonsterSpawn(237, new(17, 14)));
            }),
            new MonsterSpawn(237, new(4, 6), IsBonus: true),
            new MonsterSpawn(243, new(20, 14), OnDeath: r =>
            {
                r.Summon(_room33, new MonsterSpawn(243, new(9, 11)));
            }));
        r.SpawnPortal(_room34, new(1, 14), _room33, new(29, 14), locked: false);
        r.SpawnPortal(_room34, new(28, 14), _room34, new(28, 14), locked: false);
        r.SpawnPortal(_room35, new(15, 28), _room34, new(15, 0), locked: false);
        r.SpawnPortal(_room35, new(15, 0), _room36, new(15, 28), locked: false);
        r.SpawnPortal(_room36, new(15, 28), _room35, new(15, 0));
        r.SpawnPortal(_room36, new(15, 0), _room37, new(15, 28));
        r.Summon(_room36,
            new MonsterSpawn(237, new(15, 5), IsBonus: true),
            new MonsterSpawn(237, new(18, 7), OnDeath: r =>
            {
                r.Summon(_room36, new MonsterSpawn(231, new(11, 19), OnDeath: r =>
                {
                    r.Summon(_room36, new MonsterSpawn(265, new(20, 12), IsBonus: true));
                }));
            }),
            new MonsterSpawn(237, new(19, 21)),
            new MonsterSpawn(237, new(22, 18), OnDeath: r =>
            {
                r.Summon(_room36,
                new MonsterSpawn(228, new(11, 19), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room36, new MonsterSpawn(228, new(11, 9)));
                }),
                new MonsterSpawn(228, new(11, 17), OnDeath: r =>
                {
                    r.Summon(_room36, new MonsterSpawn(265, new(9, 14), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room37, new(15, 28), _room36, new(15, 0), locked: false);
        r.SpawnPortal(_room37, new(15, 0), _room38, new(15, 28), locked: false);
        r.SpawnPortal(_room38, new(1, 14), _room39, new(28, 14), locked: false);
        r.SpawnPortal(_room38, new(15, 28), _room37, new(15, 0), locked: false);
        r.Summon(_room38,
            new MonsterSpawn(243, new(9, 11), OnDeath: r =>
            {
                r.Summon(_room38, new MonsterSpawn(237, new(17, 14)));
            }),
            new MonsterSpawn(243, new(18, 7)),
            new MonsterSpawn(237, new(26, 5), IsBonus: true),
            new MonsterSpawn(243, new(15, 15)));
        r.SpawnPortal(_room39, new(1, 14), _room40, new(28, 14), locked: false);
        r.SpawnPortal(_room39, new(28, 14), _room38, new(1, 14), locked: false);
        r.SpawnPortal(_room40, new(1, 14), _room41, new(29, 14), locked: false);
        r.SpawnPortal(_room40, new(28, 14), _room39, new(1, 14), locked: false);
        r.Summon(_room40,
            new MonsterSpawn(243, new(7, 4), IsBonus: true),
            new MonsterSpawn(237, new(14, 10), OnDeath: r =>
            {
                r.Summon(_room40, new MonsterSpawn(237, new(17, 14), IsBonus: true));
            }),
            new MonsterSpawn(243, new(15, 22), OnDeath: r =>
            {
                r.Summon(_room40, new MonsterSpawn(243, new(9, 11)));
            }));
        r.SpawnPortal(_room41, new(1, 14), _room42, new(29, 14), locked: false);
        r.SpawnPortal(_room41, new(29, 14), _room40, new(1, 14), locked: false);
        r.SpawnPortal(_room42, new(15, 28), _room43, new(15, 0), locked: false);
        r.SpawnPortal(_room42, new(29, 14), _room41, new(1, 14), locked: false);
        r.SpawnPortal(_room43, new(15, 0), _room44, new(15, 28));
        r.Summon(_room43,
            new MonsterSpawn(245, new(22, 12)),
            new MonsterSpawn(234, new(15, 12)),
            new MonsterSpawn(234, new(14, 12), OnDeath: r =>
            {
                r.Summon(_room43,
                new MonsterSpawn(243, new(21, 19)),
                new MonsterSpawn(243, new(9, 18), IsBonus: true));
            }),
            new MonsterSpawn(245, new(20, 13)),
            new MonsterSpawn(245, new(8, 12)),
            new MonsterSpawn(245, new(9, 13), IsBonus: true),
            new MonsterSpawn(225, new(12, 16)),
            new MonsterSpawn(225, new(16, 16)),
            new MonsterSpawn(225, new(18, 17), IsBonus: true));
        r.SpawnPortal(_room44, new(15, 28), _room43, new(15, 0), locked: false);
        r.SpawnPortal(_room44, new(29, 14), _room44, new(29, 14), locked: false);
        r.SpawnPortal(_room45, new(15, 28), _room44, new(15, 0), locked: false);
        r.SpawnExit(_room45, new(29, 14), locked: false);
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
            r.Dialog(_room2, 6292);
        }
        else if (room == _room45)
        {
            r.Dialog(_room45, 6295);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room5)
        {
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room11);
            r.UnlockPortal(_room12, _room13);
        }
        else if (room == _room16)
        {
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room16, _room15);
            r.UnlockPortal(_room16, _room17);
        }
        else if (room == _room26)
        {
            r.AddClockTime(TimeSpan.FromSeconds(300));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room26, _room27);
        }
        else if (room == _room30)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room30, _room31);
        }
        else if (room == _room36)
        {
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room36, _room35);
            r.UnlockPortal(_room36, _room37);
        }
        else if (room == _room43)
        {
            r.AddClockTime(TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room43, _room44);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace58();
