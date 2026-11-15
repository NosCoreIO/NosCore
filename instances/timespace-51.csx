// Kenel against Catlotte
public class TimeSpace51 : InstanceScript
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

    public TimeSpace51()
    {
        Definition = InstanceDefinitionBuilder.Named(51, "Could Cenil really be Princess Catlotte? What will happen if Kenel finds her first? Better not to think about it. Set off and find the princess!", "Kenel against Catlotte")
            .ForLevels(58, 99)
            .StartingAt(14, 13)
            .WithLives(1)
            .Requiring(1012, 5)
            .WithSpecialReward(1014, 99)
            .WithReward(2003, 4)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 6)
            .WithRoom(4809, out _room2, 0, 6)
            .WithRoom(4802, out _room3, 1, 6)
            .WithRoom(4805, out _room4, 1, 7)
            .WithRoom(4816, out _room5, 2, 7)
            .WithRoom(4805, out _room6, 3, 7)
            .WithRoom(4811, out _room7, 1, 8)
            .WithRoom(4810, out _room8, 1, 9)
            .WithRoom(4812, out _room9, 2, 9)
            .WithRoom(4809, out _room10, 2, 8)
            .WithRoom(4811, out _room11, 3, 8)
            .WithRoom(4810, out _room12, 3, 9)
            .WithRoom(4807, out _room13, 4, 9)
            .WithRoom(4807, out _room14, 4, 8)
            .WithRoom(4809, out _room15, 4, 7)
            .WithRoom(4806, out _room16, 4, 6)
            .WithRoom(4811, out _room17, 3, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14), locked: false);
        r.SpawnPortal(_room2, new(15, 28), _room3, new(15, 0), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(315, new(10, 13)));
        r.SpawnPortal(_room3, new(29, 14), _room4, new(1, 14), locked: false);
        r.SpawnPortal(_room3, new(15, 0), _room2, new(15, 28), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(235, new(21, 18), IsBonus: true),
            new MonsterSpawn(244, new(16, 15), IsBonus: true),
            new MonsterSpawn(235, new(15, 20), IsBonus: true),
            new MonsterSpawn(244, new(18, 15)),
            new MonsterSpawn(235, new(21, 17), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(241, new(17, 8)));
            }));
        r.SpawnPortal(_room4, new(1, 14), _room3, new(29, 14), locked: false);
        r.SpawnPortal(_room4, new(28, 14), _room5, new(1, 14), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(235, new(14, 22), OnDeath: r =>
            {
                r.Summon(_room4,
                new MonsterSpawn(235, new(21, 18), IsBonus: true),
                new MonsterSpawn(244, new(16, 15)));
            }),
            new MonsterSpawn(244, new(20, 17), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(241, new(17, 8)));
            }),
            new MonsterSpawn(235, new(22, 16), IsBonus: true));
        r.SpawnPortal(_room5, new(1, 14), _room6, new(28, 14));
        r.Summon(_room5,
            new MonsterSpawn(241, new(8, 13), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(244, new(15, 15), IsBonus: true));
            }),
            new MonsterSpawn(241, new(13, 18), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(241, new(20, 17), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(218, new(12, 10)));
                }));
            }),
            new MonsterSpawn(244, new(20, 11), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(218, new(19, 14), IsBonus: true),
                new MonsterSpawn(218, new(16, 9)));
            }));
        r.SummonNpcs(_room5, new NpcSpawn(323, new(5, 14)));
        r.SpawnPortal(_room6, new(1, 14), _room6, new(1, 14), locked: false);
        r.SpawnPortal(_room6, new(28, 14), _room5, new(1, 14), locked: false);
        r.SpawnPortal(_room7, new(29, 14), _room8, new(1, 14), locked: false);
        r.SpawnPortal(_room7, new(15, 0), _room6, new(15, 0), locked: false);
        r.SpawnPortal(_room8, new(1, 14), _room7, new(29, 14), locked: false);
        r.SpawnPortal(_room8, new(15, 0), _room9, new(15, 28), locked: false);
        r.Summon(_room8,
            new MonsterSpawn(235, new(12, 21), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(235, new(21, 18)),
                new MonsterSpawn(244, new(16, 15), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(241, new(17, 8)));
                }));
            }),
            new MonsterSpawn(244, new(19, 18), IsBonus: true),
            new MonsterSpawn(235, new(19, 17)));
        r.SpawnPortal(_room9, new(15, 28), _room8, new(15, 0), locked: false);
        r.SpawnPortal(_room9, new(29, 14), _room10, new(1, 14), locked: false);
        r.Summon(_room9,
            new MonsterSpawn(235, new(19, 23), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9,
                new MonsterSpawn(235, new(21, 18), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room9, new MonsterSpawn(241, new(17, 8), IsBonus: true));
                }),
                new MonsterSpawn(244, new(16, 15)));
            }),
            new MonsterSpawn(244, new(21, 8)),
            new MonsterSpawn(235, new(19, 15)));
        r.SpawnPortal(_room10, new(1, 14), _room9, new(29, 14));
        r.SpawnPortal(_room10, new(15, 28), _room11, new(15, 0));
        r.Summon(_room10,
            new MonsterSpawn(244, new(13, 15), IsBonus: true),
            new MonsterSpawn(241, new(7, 13)),
            new MonsterSpawn(241, new(11, 17), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(241, new(20, 17), OnDeath: r =>
                {
                    r.Summon(_room10,
                    new MonsterSpawn(218, new(19, 14), IsBonus: true),
                    new MonsterSpawn(218, new(16, 9)));
                }));
            }),
            new MonsterSpawn(244, new(20, 11), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(218, new(12, 10), IsBonus: true));
            }));
        r.SummonNpcs(_room10, new NpcSpawn(323, new(8, 14)));
        r.SpawnPortal(_room11, new(29, 14), _room12, new(1, 14), locked: false);
        r.SpawnPortal(_room11, new(15, 0), _room10, new(15, 28), locked: false);
        r.SpawnPortal(_room12, new(1, 14), _room11, new(29, 14), locked: false);
        r.SpawnPortal(_room12, new(15, 0), _room13, new(15, 28), locked: false);
        r.Summon(_room12,
            new MonsterSpawn(235, new(18, 22), OnDeath: r =>
            {
                r.Summon(_room12, new MonsterSpawn(241, new(17, 8)));
            }),
            new MonsterSpawn(244, new(22, 13)),
            new MonsterSpawn(235, new(12, 22), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(235, new(21, 18), IsBonus: true),
                new MonsterSpawn(244, new(16, 15)));
            }));
        r.SpawnPortal(_room13, new(15, 28), _room12, new(15, 0));
        r.SpawnPortal(_room13, new(15, 0), _room14, new(15, 28));
        r.Summon(_room13,
            new MonsterSpawn(241, new(20, 17), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13,
                new MonsterSpawn(218, new(19, 14)),
                new MonsterSpawn(218, new(16, 9)));
            }),
            new MonsterSpawn(244, new(13, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room13, new MonsterSpawn(218, new(12, 10), IsBonus: true));
            }),
            new MonsterSpawn(241, new(10, 12)),
            new MonsterSpawn(241, new(13, 18)),
            new MonsterSpawn(244, new(20, 11), IsBonus: true));
        r.SummonNpcs(_room13, new NpcSpawn(122, new(11, 21)), new NpcSpawn(329, new(14, 22)), new NpcSpawn(122, new(15, 20)), new NpcSpawn(122, new(17, 21)));
        r.SpawnPortal(_room14, new(15, 28), _room13, new(15, 0));
        r.SpawnPortal(_room14, new(15, 0), _room15, new(15, 28));
        r.Summon(_room14,
            new MonsterSpawn(244, new(13, 15), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(241, new(20, 17), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room14,
                    new MonsterSpawn(218, new(19, 14), IsBonus: true),
                    new MonsterSpawn(218, new(16, 9), IsBonus: true));
                }));
            }),
            new MonsterSpawn(241, new(10, 12), OnDeath: r =>
            {
                r.Summon(_room14, new MonsterSpawn(218, new(12, 10), IsBonus: true));
            }),
            new MonsterSpawn(241, new(13, 18)),
            new MonsterSpawn(244, new(20, 11), IsBonus: true));
        r.SummonNpcs(_room14, new NpcSpawn(323, new(15, 21)));
        r.SpawnPortal(_room15, new(1, 14), _room16, new(29, 14), locked: false);
        r.SpawnPortal(_room15, new(15, 28), _room14, new(15, 0), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room17, new(29, 14), locked: false);
        r.SpawnPortal(_room16, new(29, 14), _room15, new(1, 14), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(241, new(17, 8), IsBonus: true),
            new MonsterSpawn(235, new(21, 18)),
            new MonsterSpawn(244, new(16, 15)),
            new MonsterSpawn(235, new(16, 20)),
            new MonsterSpawn(244, new(23, 12)),
            new MonsterSpawn(235, new(12, 13), IsBonus: true));
        r.SpawnPortal(_room17, new(29, 14), _room16, new(1, 14));
        r.SpawnExit(_room17, new(15, 0));
        r.Summon(_room17, new MonsterSpawn(329, new(14, 16), IsBonus: true));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(240));
            r.Dialog(_room2, 6253);
        }
        else if (room == _room17)
        {
            r.Dialog(_room17, 6259);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room5)
        {
            r.Dialog(_room5, 6254);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room10)
        {
            r.Dialog(_room10, 6256);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room11);
        }
        else if (room == _room13)
        {
            r.Dialog(_room13, 6257);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room13, _room12);
            r.UnlockPortal(_room13, _room14);
        }
        else if (room == _room14)
        {
            r.Dialog(_room14, 6258);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room14, _room13);
            r.UnlockPortal(_room14, _room15);
        }
        else if (room == _room17)
        {
            r.Dialog(_room17, 6260);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockExit(_room17);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace51();
