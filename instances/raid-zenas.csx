using System.Linq;

// The hall, the two start cells, the monsters, the tests and their timings, the portals and the loot follow a
// packet trace of the raid. The whole raid is one map: each test opens a portal that carries the team on.
public class ZenasRaid : InstanceScript
{
    private const short Zenas = 2504;
    private const short Crusader = 2505;
    private const short GuardianKnight = 2506;
    private const short GuardianCommander = 2507;
    private const short GuardianMage = 2508;
    private const short Priest = 2509;
    private const short Initiate = 2549;
    private const short PriestessKelly = 2550;
    private const short PriestessMolly = 2551;
    private const short ArchangelLucifer = 2553;

    private static readonly TimeSpan SecondTestDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ThirdTestDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PrayerLength = TimeSpan.FromSeconds(60);

    private static readonly Spot[] CrusaderSpots =
        [new(151, 208), new(147, 217), new(151, 226), new(168, 209), new(160, 229), new(168, 225), new(172, 217)];

    // The guards that fall on the praying priestess, wave by wave, from the crusaders' cells.
    private static readonly (short VNum, short X, short Y)[][] PrayerWaves =
    [
        [(GuardianKnight, 151, 226), (GuardianCommander, 168, 209), (GuardianMage, 168, 225)],
        [(GuardianKnight, 172, 217), (GuardianCommander, 147, 217), (GuardianMage, 151, 208)],
        [(GuardianKnight, 151, 208), (GuardianCommander, 172, 217), (GuardianMage, 151, 226)],
        [(GuardianKnight, 168, 225), (GuardianCommander, 168, 209), (GuardianMage, 168, 225)],
        [(GuardianKnight, 172, 217), (GuardianCommander, 172, 217), (GuardianMage, 147, 217)],
        [(GuardianKnight, 160, 229), (GuardianCommander, 172, 217), (GuardianMage, 168, 225)],
        [(GuardianKnight, 151, 208), (GuardianCommander, 168, 209), (GuardianMage, 168, 225)]
    ];

    private readonly int _hall;

    public ZenasRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(23, "raid-zenas", "raid-zenas")
            .ForLevels(85, 99)
            .ForTeams(5, 15)
            .Requiring(5883, 1)
            .StartingAt(90, 103)
            .AlsoStartingAt(245, 102)
            .WithRoom(2602, out _hall, 0, 0)
            .WithReward(9844, 1)
            .WithRaidHudValue(20)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromHours(1));
        run.Summon(_hall, new MonsterSpawn(Zenas, new Spot(160, 128), IsBoss: true));
        run.Summon(_hall, Pack(GuardianKnight, (90, 261), (77, 214), (69, 235), (249, 236), (248, 208), (222, 265), (218, 239), (206, 204), (199, 254), (185, 227), (180, 290), (171, 291), (164, 291), (157, 291), (147, 291), (103, 194), (132, 229), (111, 205), (106, 245), (101, 193)));
        run.Summon(_hall, Pack(GuardianCommander, (97, 227), (87, 261), (79, 195), (76, 193), (71, 232), (71, 196), (69, 192), (66, 234), (247, 233), (222, 262), (221, 268), (220, 238), (209, 202), (207, 206), (202, 257), (195, 278), (189, 193), (188, 230), (143, 244), (134, 228), (134, 201), (132, 282), (132, 232), (118, 240), (114, 257), (108, 204)));
        run.Summon(_hall, Pack(GuardianMage, (92, 260), (87, 241), (68, 232), (253, 215), (180, 254), (153, 132), (212, 160), (246, 236), (220, 242), (217, 235), (199, 256), (196, 251), (185, 230), (140, 244), (132, 228), (132, 198), (130, 231), (116, 255), (113, 259), (113, 205), (109, 206), (100, 276), (100, 225)));
        run.Summon(_hall, Pack(Priest, (98, 229), (69, 237), (233, 246), (224, 267), (222, 288), (222, 285), (221, 240), (221, 235), (218, 288), (202, 185), (196, 254), (142, 242), (139, 247), (232, 97), (131, 201), (117, 257), (116, 259), (116, 216), (110, 290), (110, 208), (109, 286), (105, 287), (101, 174)));

        run.At(TimeSpan.FromSeconds(10), r => r.Message(Game18NConstString.ZenasFirstTestWillStart));
        run.At(TimeSpan.FromSeconds(30), FirstTest);
    }

    // Guards close in on both start cells; the west pack falling passes the test.
    private void FirstTest(IInstanceRun run)
    {
        var westLeft = new int[1];
        Action<IInstanceRun> westFell = r =>
        {
            if (--westLeft[0] == 0)
            {
                FirstTestPassed(r);
            }
        };

        var west = Pack(GuardianKnight, (102, 116), (103, 102), (93, 102), (75, 98), (74, 89), (85, 113), (92, 112))
            .Concat(Pack(GuardianCommander, (87, 108), (99, 119), (99, 91), (73, 120), (99, 100), (79, 109), (72, 92)))
            .Concat(Pack(GuardianMage, (103, 116), (91, 106), (80, 115), (106, 93), (103, 95), (86, 88), (93, 113)))
            .Select(m => m with { OnDeath = westFell })
            .ToArray();
        westLeft[0] = west.Length;
        run.Summon(_hall, west);
        run.Summon(_hall, Pack(GuardianKnight, (231, 80), (215, 122), (218, 93), (238, 80), (246, 106), (221, 106), (239, 76)));
        run.Summon(_hall, Pack(GuardianCommander, (216, 78), (239, 123), (252, 90), (216, 116), (253, 108), (217, 113), (234, 81)));
        run.Summon(_hall, Pack(GuardianMage, (245, 102), (230, 122), (233, 111), (253, 118), (219, 115), (231, 95), (231, 84)));
    }

    private void FirstTestPassed(IInstanceRun run)
    {
        run.SpawnTeamPortal(_hall, new Spot(91, 149), new Spot(88, 175));
        run.Message(Game18NConstString.ZenasFirstTestPassed);
        run.At(run.Elapsed + SecondTestDelay, r =>
        {
            r.Message(Game18NConstString.ArchangelLuciferZenas13thWingAppeared);
            r.Summon(_hall, new MonsterSpawn(ArchangelLucifer, new Spot(160, 269)));
        });
    }

    private void SecondTestPassed(IInstanceRun run)
    {
        foreach (var guard in new[] { GuardianKnight, GuardianCommander, GuardianMage, Priest })
        {
            run.Dismiss(_hall, guard);
        }

        run.Throw(_hall, new Spot(160, 269), new ItemPile(2136, 2, 1), new ItemPile(2035, 2, 1), new ItemPile(1013, 3, 1),
            new ItemPile(2048, 1, 1), new ItemPile(2040, 1, 1), new ItemPile(2043, 1, 1), new ItemPile(2044, 2, 1),
            new ItemPile(2036, 1, 1));
        run.Message(Game18NConstString.ZenasSecondTestPassed);
        run.At(run.Elapsed + TimeSpan.FromSeconds(5), r => r.SpawnTeamPortal(_hall, new Spot(160, 247), new Spot(160, 217)));
        run.At(run.Elapsed + ThirdTestDelay, ThirdTest);
    }

    // The Second Squad holds the circle while Priestess Kelly prays, and the guards come at her in waves.
    private void ThirdTest(IInstanceRun run)
    {
        var start = run.Elapsed;
        run.SummonNpcs(_hall, new NpcSpawn(PriestessKelly, new Spot(160, 217)), new NpcSpawn(PriestessMolly, new Spot(160, 214)),
            new NpcSpawn(Initiate, new Spot(160, 204), IsAlly: true));
        run.SummonNpcs(_hall, Array.ConvertAll(CrusaderSpots, s => new NpcSpawn(Crusader, s, IsAlly: true)));
        run.StartRoomClock(_hall, PrayerLength);
        for (var i = 0; i < PrayerWaves.Length; i++)
        {
            var wave = PrayerWaves[i];
            run.At(start + TimeSpan.FromSeconds(6 + 8 * i),
                r => r.Summon(_hall, Array.ConvertAll(wave, m => new MonsterSpawn(m.VNum, new Spot(m.X, m.Y)))));
        }

        run.At(start + PrayerLength, r =>
        {
            r.StopRoomClock(_hall);
            r.Message(Game18NConstString.ZenasAllTestPassed);
        });
        run.At(start + PrayerLength + TimeSpan.FromSeconds(6),
            r => r.SpawnTeamPortal(_hall, new Spot(160, 203), new Spot(159, 170)));
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum == ArchangelLucifer)
        {
            SecondTestPassed(run);
        }
        else if (vNum == Zenas)
        {
            run.Throw(_hall, new Spot(160, 133), new ItemPile(13270, 1, 1), new ItemPile(2036, 2, 1), new ItemPile(2035, 1, 1),
                new ItemPile(1029, 2, 1), new ItemPile(2170, 1, 1), new ItemPile(2044, 1, 1), new ItemPile(1092, 2, 1),
                new ItemPile(1013, 1, 1), new ItemPile(2048, 2, 1), new ItemPile(2127, 1, 1), new ItemPile(1046, 30, 5000));
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();

    private static MonsterSpawn[] Pack(short vNum, params (short X, short Y)[] spots) =>
        Array.ConvertAll(spots, s => new MonsterSpawn(vNum, new Spot(s.X, s.Y)));
}

return new ZenasRaid();
