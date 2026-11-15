// Shanera's Dungeon: Part 1
public class TimeSpace57 : InstanceScript
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
    private readonly int _room46;
    private readonly int _room47;
    private readonly int _room48;

    public TimeSpace57()
    {
        Definition = InstanceDefinitionBuilder.Named(57, "Welcome to Shanera's Dungeon. A scout reports that the Kenkos brought Catlotte, the Catsy princess, here. But why? There's no time to lose: we must free her from the Kenkos' clutches. Go to her rescue!", "Shanera's Dungeon: Part 1")
            .ForLevels(62, 99)
            .StartingAt(12, 9)
            .WithLives(2)
            .Requiring(1012, 6)
            .WithSpecialReward(1011, 50)
            .WithReward(1033, 1)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 5)
            .WithRoom(4809, out _room2, 0, 5)
            .WithRoom(4802, out _room3, 1, 5)
            .WithRoom(4800, out _room4, 1, 6)
            .WithRoom(4815, out _room5, 2, 6)
            .WithRoom(4800, out _room6, 2, 5)
            .WithRoom(4812, out _room7, 3, 6)
            .WithRoom(4816, out _room8, 3, 5)
            .WithRoom(4812, out _room9, 4, 5)
            .WithRoom(4803, out _room10, 4, 6)
            .WithRoom(4803, out _room11, 2, 7)
            .WithRoom(4812, out _room12, 2, 8)
            .WithRoom(4803, out _room13, 1, 8)
            .WithRoom(4802, out _room14, 1, 7)
            .WithRoom(4801, out _room15, 3, 7)
            .WithRoom(4805, out _room16, 4, 7)
            .WithRoom(4809, out _room17, 5, 7)
            .WithRoom(4805, out _room18, 6, 7)
            .WithRoom(4805, out _room19, 4, 8)
            .WithRoom(4803, out _room20, 3, 8)
            .WithRoom(4806, out _room21, 1, 9)
            .WithRoom(4805, out _room22, 2, 9)
            .WithRoom(4805, out _room23, 3, 9)
            .WithRoom(4803, out _room24, 4, 9)
            .WithRoom(4812, out _room25, 5, 9)
            .WithRoom(4810, out _room26, 5, 8)
            .WithRoom(4809, out _room27, 6, 8)
            .WithRoom(4805, out _room28, 6, 9)
            .WithRoom(4803, out _room29, 7, 9)
            .WithRoom(4809, out _room30, 8, 9)
            .WithRoom(4814, out _room31, 8, 8)
            .WithRoom(4809, out _room32, 7, 8)
            .WithRoom(4807, out _room33, 9, 9)
            .WithRoom(4804, out _room34, 9, 8)
            .WithRoom(4814, out _room35, 9, 7)
            .WithRoom(4804, out _room36, 8, 7)
            .WithRoom(4814, out _room37, 9, 6)
            .WithRoom(4809, out _room38, 8, 6)
            .WithRoom(4807, out _room39, 10, 9)
            .WithRoom(4807, out _room40, 10, 8)
            .WithRoom(4807, out _room41, 10, 7)
            .WithRoom(4809, out _room42, 10, 6)
            .WithRoom(4803, out _room43, 10, 5)
            .WithRoom(4814, out _room44, 9, 5)
            .WithRoom(4803, out _room45, 8, 5)
            .WithRoom(4811, out _room46, 9, 4)
            .WithRoom(4812, out _room47, 8, 4)
            .WithRoom(4805, out _room48, 8, 3)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14), locked: false);
        r.SpawnPortal(_room2, new(15, 28), _room3, new(15, 0), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(247, new(14, 10)), new NpcSpawn(249, new(15, 17)), new NpcSpawn(247, new(16, 11)), new NpcSpawn(249, new(16, 15)));
        r.SpawnPortal(_room3, new(29, 14), _room4, new(1, 14), locked: false);
        r.SpawnPortal(_room3, new(15, 0), _room2, new(15, 28), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(236, new(13, 13), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(242, new(20, 15), IsBonus: true));
            }),
            new MonsterSpawn(245, new(19, 11), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(236, new(10, 16), IsBonus: true));
            }));
        r.SpawnPortal(_room4, new(1, 14), _room3, new(29, 14), locked: false);
        r.SpawnPortal(_room4, new(15, 1), _room5, new(15, 28), locked: false);
        r.SpawnPortal(_room5, new(15, 28), _room6, new(15, 1));
        r.Summon(_room5,
            new MonsterSpawn(242, new(11, 22)),
            new MonsterSpawn(242, new(12, 11), IsBonus: true),
            new MonsterSpawn(242, new(10, 15), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(225, new(15, 20), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(225, new(15, 17)));
                }),
                new MonsterSpawn(242, new(12, 10)),
                new MonsterSpawn(242, new(11, 7)),
                new MonsterSpawn(242, new(10, 7), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(225, new(15, 10), OnDeath: r =>
                    {
                        r.Summon(_room5,
                        new MonsterSpawn(246, new(18, 14)),
                        new MonsterSpawn(225, new(15, 7), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(242, new(10, 19)),
            new MonsterSpawn(242, new(12, 22)));
        r.SpawnPortal(_room6, new(28, 14), _room6, new(28, 14), locked: false);
        r.SpawnPortal(_room6, new(15, 1), _room5, new(15, 28), locked: false);
        r.SpawnPortal(_room7, new(15, 28), _room6, new(15, 0), locked: false);
        r.SpawnPortal(_room7, new(29, 14), _room8, new(1, 14), locked: false);
        r.Summon(_room7,
            new MonsterSpawn(236, new(10, 16), IsBonus: true),
            new MonsterSpawn(242, new(20, 15)),
            new MonsterSpawn(236, new(11, 15)),
            new MonsterSpawn(245, new(20, 11)));
        r.SpawnPortal(_room8, new(1, 14), _room9, new(29, 14));
        r.Summon(_room8,
            new MonsterSpawn(234, new(9, 19), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(243, new(17, 16), IsBonus: true));
            }),
            new MonsterSpawn(234, new(10, 9)),
            new MonsterSpawn(237, new(13, 21)),
            new MonsterSpawn(237, new(14, 16), OnDeath: r =>
            {
                r.Summon(_room8,
                new MonsterSpawn(237, new(14, 11), OnDeath: r =>
                {
                    r.Summon(_room8, new MonsterSpawn(243, new(17, 10)));
                }),
                new MonsterSpawn(237, new(13, 7)));
            }));
        r.SpawnPortal(_room9, new(15, 28), _room9, new(15, 28), locked: false);
        r.SpawnPortal(_room9, new(29, 14), _room8, new(1, 14), locked: false);
        r.SpawnPortal(_room10, new(1, 14), _room10, new(1, 14), locked: false);
        r.SpawnPortal(_room10, new(28, 14), _room9, new(1, 0), locked: false);
        r.SpawnPortal(_room11, new(1, 14), _room12, new(29, 14), locked: false);
        r.SpawnPortal(_room11, new(15, 0), _room10, new(15, 0), locked: false);
        r.Summon(_room11,
            new MonsterSpawn(207, new(13, 13), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(245, new(16, 10)));
            }),
            new MonsterSpawn(207, new(14, 8), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(245, new(17, 15)));
            }),
            new MonsterSpawn(207, new(14, 13)),
            new MonsterSpawn(207, new(13, 21), IsBonus: true));
        r.SpawnPortal(_room12, new(29, 14), _room13, new(1, 14), locked: false);
        r.SpawnPortal(_room13, new(1, 14), _room12, new(29, 14), locked: false);
        r.SpawnPortal(_room13, new(15, 0), _room13, new(15, 0), locked: false);
        r.SpawnPortal(_room14, new(15, 28), _room13, new(15, 0), locked: false);
        r.SpawnPortal(_room14, new(29, 14), _room14, new(29, 14), locked: false);
        r.SpawnPortal(_room15, new(1, 14), _room14, new(28, 0), locked: false);
        r.SpawnPortal(_room15, new(29, 14), _room16, new(1, 14), locked: false);
        r.SpawnPortal(_room16, new(1, 14), _room15, new(29, 14), locked: false);
        r.SpawnPortal(_room16, new(28, 14), _room17, new(1, 14), locked: false);
        r.Summon(_room16,
            new MonsterSpawn(236, new(6, 7), OnDeath: r =>
            {
                r.Summon(_room16, new MonsterSpawn(242, new(20, 15), OnDeath: r =>
                {
                    r.Summon(_room16, new MonsterSpawn(236, new(10, 16), IsBonus: true));
                }));
            }),
            new MonsterSpawn(245, new(20, 18), IsBonus: true));
        r.SpawnPortal(_room17, new(1, 14), _room18, new(28, 14), locked: false);
        r.SpawnPortal(_room18, new(1, 14), _room18, new(1, 14), locked: false);
        r.SpawnPortal(_room18, new(28, 14), _room17, new(1, 14), locked: false);
        r.SpawnPortal(_room19, new(1, 14), _room20, new(28, 14));
        r.SpawnPortal(_room19, new(28, 14), _room18, new(1, 0));
        r.Summon(_room19,
            new MonsterSpawn(242, new(12, 11)),
            new MonsterSpawn(242, new(12, 17)),
            new MonsterSpawn(242, new(12, 19), OnDeath: r =>
            {
                r.Summon(_room19, new MonsterSpawn(225, new(15, 17), OnDeath: r =>
                {
                    r.Summon(_room19, new MonsterSpawn(225, new(15, 10)));
                }));
            }),
            new MonsterSpawn(242, new(12, 22), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room19,
                new MonsterSpawn(225, new(15, 20)),
                new MonsterSpawn(242, new(12, 10), IsBonus: true),
                new MonsterSpawn(242, new(11, 7)),
                new MonsterSpawn(242, new(10, 7), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room19,
                    new MonsterSpawn(246, new(18, 14), IsBonus: true),
                    new MonsterSpawn(225, new(15, 7), IsBonus: true));
                }));
            }),
            new MonsterSpawn(242, new(15, 22)));
        r.SpawnPortal(_room20, new(1, 14), _room20, new(1, 14), locked: false);
        r.SpawnPortal(_room20, new(28, 14), _room19, new(1, 14), locked: false);
        r.SpawnPortal(_room21, new(1, 14), _room20, new(29, 0), locked: false);
        r.SpawnPortal(_room21, new(29, 14), _room22, new(1, 14), locked: false);
        r.SpawnPortal(_room22, new(1, 14), _room21, new(29, 14), locked: false);
        r.SpawnPortal(_room22, new(28, 14), _room23, new(1, 14), locked: false);
        r.Summon(_room22,
            new MonsterSpawn(236, new(21, 9), OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(236, new(10, 16), IsBonus: true));
            }),
            new MonsterSpawn(245, new(16, 18), OnDeath: r =>
            {
                r.Summon(_room22, new MonsterSpawn(242, new(20, 15)));
            }));
        r.SpawnPortal(_room23, new(1, 14), _room22, new(28, 14), locked: false);
        r.SpawnPortal(_room23, new(28, 14), _room24, new(1, 14), locked: false);
        r.SpawnPortal(_room24, new(1, 14), _room23, new(28, 14), locked: false);
        r.SpawnPortal(_room24, new(15, 0), _room25, new(15, 28), locked: false);
        r.Summon(_room24,
            new MonsterSpawn(207, new(21, 11)),
            new MonsterSpawn(207, new(13, 8), OnDeath: r =>
            {
                r.Summon(_room24, new MonsterSpawn(245, new(17, 15), OnDeath: r =>
                {
                    r.Summon(_room24, new MonsterSpawn(245, new(16, 10), IsBonus: true));
                }));
            }),
            new MonsterSpawn(207, new(23, 16), IsBonus: true),
            new MonsterSpawn(207, new(18, 22), IsBonus: true));
        r.SpawnPortal(_room25, new(15, 28), _room24, new(15, 0));
        r.SpawnPortal(_room25, new(29, 14), _room26, new(1, 14));
        r.Summon(_room25,
            new MonsterSpawn(242, new(11, 22)),
            new MonsterSpawn(242, new(12, 11)),
            new MonsterSpawn(242, new(12, 17), OnDeath: r =>
            {
                r.Summon(_room25,
                new MonsterSpawn(225, new(18, 20), OnDeath: r =>
                {
                    r.Summon(_room25, new MonsterSpawn(225, new(18, 17)));
                }),
                new MonsterSpawn(242, new(12, 10), OnDeath: r =>
                {
                    r.Summon(_room25, new MonsterSpawn(225, new(18, 10)));
                }),
                new MonsterSpawn(242, new(11, 7), IsBonus: true),
                new MonsterSpawn(242, new(10, 7), OnDeath: r =>
                {
                    r.Summon(_room25,
                    new MonsterSpawn(246, new(18, 14), IsBonus: true),
                    new MonsterSpawn(225, new(18, 7), IsBonus: true));
                }));
            }),
            new MonsterSpawn(242, new(12, 19)),
            new MonsterSpawn(242, new(12, 22)));
        r.SpawnPortal(_room26, new(1, 14), _room25, new(29, 14), locked: false);
        r.SpawnPortal(_room26, new(15, 0), _room27, new(15, 28), locked: false);
        r.SpawnPortal(_room27, new(1, 14), _room27, new(1, 14), locked: false);
        r.SpawnPortal(_room27, new(15, 28), _room26, new(15, 0), locked: false);
        r.SpawnPortal(_room28, new(1, 14), _room27, new(28, 0), locked: false);
        r.SpawnPortal(_room28, new(28, 14), _room29, new(1, 14), locked: false);
        r.SpawnPortal(_room29, new(1, 14), _room28, new(28, 14), locked: false);
        r.SpawnPortal(_room29, new(15, 0), _room30, new(15, 28), locked: false);
        r.Summon(_room29,
            new MonsterSpawn(245, new(16, 10), IsBonus: true),
            new MonsterSpawn(245, new(17, 15), IsBonus: true),
            new MonsterSpawn(207, new(16, 19), IsBonus: true),
            new MonsterSpawn(207, new(5, 5)),
            new MonsterSpawn(207, new(14, 17)),
            new MonsterSpawn(207, new(5, 12), IsBonus: true));
        r.SpawnPortal(_room30, new(1, 14), _room31, new(29, 14), locked: false);
        r.SpawnPortal(_room30, new(15, 28), _room29, new(15, 0), locked: false);
        r.SpawnPortal(_room31, new(29, 14), _room32, new(1, 14));
        r.Summon(_room31,
            new MonsterSpawn(234, new(9, 19), IsBonus: true),
            new MonsterSpawn(234, new(10, 9), OnDeath: r =>
            {
                r.Summon(_room31,
                new MonsterSpawn(237, new(14, 11), OnDeath: r =>
                {
                    r.Summon(_room31, new MonsterSpawn(243, new(17, 10)));
                }),
                new MonsterSpawn(237, new(13, 7)));
            }),
            new MonsterSpawn(237, new(13, 21), OnDeath: r =>
            {
                r.Summon(_room31, new MonsterSpawn(243, new(17, 16)));
            }),
            new MonsterSpawn(237, new(14, 16)));
        r.SpawnPortal(_room32, new(1, 14), _room31, new(29, 14), locked: false);
        r.SpawnPortal(_room32, new(15, 28), _room32, new(15, 28), locked: false);
        r.SpawnPortal(_room33, new(15, 28), _room32, new(15, 0));
        r.SpawnPortal(_room33, new(15, 0), _room34, new(15, 28), locked: false);
        r.SpawnPortal(_room34, new(1, 14), _room35, new(29, 14), locked: false);
        r.SpawnPortal(_room34, new(15, 28), _room33, new(15, 0), locked: false);
        r.SpawnPortal(_room35, new(29, 14), _room36, new(1, 14));
        r.Summon(_room35,
            new MonsterSpawn(242, new(11, 22), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room35,
                new MonsterSpawn(225, new(15, 20)),
                new MonsterSpawn(242, new(12, 10)),
                new MonsterSpawn(242, new(11, 7)),
                new MonsterSpawn(242, new(10, 7), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room35,
                    new MonsterSpawn(246, new(18, 14), IsBonus: true),
                    new MonsterSpawn(225, new(15, 7), IsBonus: true));
                }));
            }),
            new MonsterSpawn(242, new(12, 11), OnDeath: r =>
            {
                r.Summon(_room35, new MonsterSpawn(225, new(15, 17), OnDeath: r =>
                {
                    r.Summon(_room35, new MonsterSpawn(225, new(15, 10)));
                }));
            }),
            new MonsterSpawn(242, new(12, 17)),
            new MonsterSpawn(242, new(11, 19)),
            new MonsterSpawn(242, new(13, 22)));
        r.SpawnPortal(_room36, new(1, 14), _room35, new(29, 14), locked: false);
        r.SpawnPortal(_room36, new(15, 0), _room36, new(15, 0), locked: false);
        r.SpawnPortal(_room37, new(29, 14), _room38, new(1, 14));
        r.Summon(_room37,
            new MonsterSpawn(234, new(9, 19), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room37, new MonsterSpawn(243, new(17, 16)));
            }),
            new MonsterSpawn(234, new(12, 7), IsBonus: true),
            new MonsterSpawn(237, new(13, 22), OnDeath: r =>
            {
                r.Summon(_room37,
                new MonsterSpawn(237, new(14, 11), OnDeath: r =>
                {
                    r.Summon(_room37, new MonsterSpawn(243, new(17, 10)));
                }),
                new MonsterSpawn(237, new(13, 7), IsBonus: true));
            }),
            new MonsterSpawn(237, new(14, 16)));
        r.SpawnPortal(_room38, new(1, 14), _room37, new(29, 14), locked: false);
        r.SpawnPortal(_room38, new(15, 28), _room38, new(15, 28), locked: false);
        r.SpawnPortal(_room39, new(15, 28), _room38, new(15, 0), locked: false);
        r.SpawnPortal(_room39, new(15, 0), _room40, new(15, 28), locked: false);
        r.SpawnPortal(_room40, new(15, 28), _room39, new(15, 0));
        r.SpawnPortal(_room40, new(15, 0), _room41, new(15, 28));
        r.Summon(_room40,
            new MonsterSpawn(228, new(21, 9), IsBonus: true),
            new MonsterSpawn(228, new(17, 9), IsBonus: true),
            new MonsterSpawn(237, new(18, 18)),
            new MonsterSpawn(237, new(14, 18)),
            new MonsterSpawn(228, new(6, 9), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room40, new MonsterSpawn(225, new(10, 14)));
            }),
            new MonsterSpawn(237, new(8, 18)),
            new MonsterSpawn(228, new(12, 9), OnDeath: r =>
            {
                r.Summon(_room40, new MonsterSpawn(225, new(20, 13)));
            }),
            new MonsterSpawn(237, new(23, 18)));
        r.SpawnPortal(_room41, new(15, 28), _room40, new(15, 0), locked: false);
        r.SpawnPortal(_room41, new(15, 0), _room42, new(15, 28), locked: false);
        r.SpawnPortal(_room42, new(1, 14), _room43, new(28, 14), locked: false);
        r.SpawnPortal(_room42, new(15, 28), _room41, new(15, 0), locked: false);
        r.Summon(_room42,
            new MonsterSpawn(216, new(21, 13), IsBonus: true),
            new MonsterSpawn(216, new(20, 10), IsBonus: true),
            new MonsterSpawn(216, new(18, 18)),
            new MonsterSpawn(216, new(15, 20)),
            new MonsterSpawn(236, new(7, 18)),
            new MonsterSpawn(236, new(7, 8)),
            new MonsterSpawn(236, new(5, 11), IsBonus: true),
            new MonsterSpawn(236, new(6, 17)));
        r.SpawnPortal(_room43, new(1, 14), _room44, new(29, 14), locked: false);
        r.SpawnPortal(_room43, new(28, 14), _room42, new(1, 14), locked: false);
        r.SpawnPortal(_room44, new(29, 14), _room45, new(1, 14));
        r.Summon(_room44,
            new MonsterSpawn(228, new(6, 9)),
            new MonsterSpawn(237, new(8, 17), IsBonus: true, OnDeath: r =>
            {
                r.Message(Game18NConstString.ADoorHasOpened);
                r.UnlockPortal(_room44, _room45);
            }),
            new MonsterSpawn(228, new(12, 9)),
            new MonsterSpawn(237, new(23, 18), OnDeath: r =>
            {
                r.Summon(_room44,
                new MonsterSpawn(237, new(18, 18), OnDeath: r =>
                {
                    r.Summon(_room44,
                    new MonsterSpawn(231, new(22, 10)),
                    new MonsterSpawn(228, new(21, 9), OnDeath: r =>
                    {
                        r.Summon(_room44, new MonsterSpawn(225, new(20, 13)));
                    }),
                    new MonsterSpawn(228, new(17, 9), OnDeath: r =>
                    {
                        r.Summon(_room44, new MonsterSpawn(225, new(16, 14)));
                    }));
                }),
                new MonsterSpawn(237, new(14, 18)));
            }));
        r.SpawnPortal(_room45, new(1, 14), _room44, new(29, 14), locked: false);
        r.SpawnPortal(_room45, new(15, 0), _room45, new(15, 0));
        r.SpawnPortal(_room46, new(29, 14), _room45, new(1, 0), locked: false);
        r.SpawnPortal(_room46, new(15, 0), _room47, new(15, 28), locked: false);
        r.SpawnPortal(_room47, new(15, 28), _room46, new(15, 0), locked: false);
        r.SpawnPortal(_room47, new(29, 14), _room48, new(1, 14), locked: false);
        r.SpawnPortal(_room48, new(1, 14), _room47, new(29, 14), locked: false);
        r.SpawnExit(_room48, new(28, 14), locked: false);
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
            r.Dialog(_room2, 6289);
        }
        else if (room == _room5)
        {
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(150));
        }
        else if (room == _room33)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room33, _room32);
        }
        else if (room == _room37)
        {
            r.StartRoomClock(_room37, TimeSpan.FromSeconds(100));
        }
        else if (room == _room40)
        {
            r.StartRoomClock(_room40, TimeSpan.FromSeconds(180));
        }
        else if (room == _room45)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room45, _room45);
        }
        else if (room == _room48)
        {
            r.Dialog(_room48, 6291);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room5)
        {
            r.StopRoomClock(_room5);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room6);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
        }
        else if (room == _room19)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room19, _room20);
            r.UnlockPortal(_room19, _room18);
        }
        else if (room == _room25)
        {
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room25, _room26);
        }
        else if (room == _room31)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room31, _room32);
        }
        else if (room == _room35)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room35, _room36);
        }
        else if (room == _room37)
        {
            r.StopRoomClock(_room37);
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room37, _room38);
        }
        else if (room == _room40)
        {
            r.StopRoomClock(_room40);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room40, _room39);
            r.UnlockPortal(_room40, _room41);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace57();
