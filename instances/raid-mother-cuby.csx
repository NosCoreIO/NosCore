// The rooms, the start point, the monsters, the levers, the portals and the loot follow a packet trace of
// the raid. The eleven Cubies and the three levers open the way to Mother Cuby. What the trace cannot show,
// it being one fast run, comes from the raid's known layout: the Cubies' half-HP calls, the reach of the
// teleport levers and the jelly waves around Mother Cuby.
public class MotherCubyRaid : InstanceScript
{
    private const short MotherCuby = 282;
    private const short Cuby = 283;
    private const short WeakRedJelly = 167;
    private const short LeverOff = 1135;
    private const short LeverOn = 1136;
    private const short TeleportLeverOff = 1137;
    private const short TeleportLeverOn = 1138;
    private const short CubyCount = 11;
    private const short TeleportReach = 4;
    private static readonly TimeSpan FirstWave = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WavesEvery = TimeSpan.FromSeconds(20);
    private static readonly Spot BossAt = new(26, 28);

    private static readonly Spot[] Levers = [new(79, 107), new(41, 70), new(26, 162)];

    // Each teleport lever, by button, and where it sends whoever pulls it.
    private static readonly (int Button, Spot At, Spot To)[] TeleportLevers =
    [
        (4, new(86, 15), new(132, 152)),
        (5, new(109, 139), new(82, 22))
    ];

    private static readonly MonsterSpawn[] Waves =
    [
        .. Pack(WeakRedJelly, (11, 12), (11, 9), (4, 10), (6, 12), (6, 9), (7, 8), (8, 10), (9, 6)),
        .. Pack(168, (4, 47), (4, 47), (4, 47), (6, 49), (8, 45), (8, 45), (8, 50), (10, 47), (11, 51), (12, 49)),
        .. Pack(169, (42, 49), (43, 51), (44, 44), (45, 48), (47, 46), (49, 44), (49, 47)),
        .. Pack(170, (41, 8), (42, 4), (43, 7), (45, 5), (46, 9), (46, 12), (48, 8))
    ];

    private static readonly int[] GoldPiles =
    [
        1663, 1966, 1976, 1943, 1215, 1207, 1421, 1922, 1344, 1025, 1324, 1356, 1913, 1233, 1229,
        1305, 1469, 1219, 1162, 1400, 1542, 1166, 1631, 1394, 1488, 1381, 1524, 1760, 1635, 1964
    ];

    private static readonly ItemPile[] Loot =
    [
        new(1011, 2, 1), new(1029, 1, 1), new(13259, 2, 1), new(2035, 1, 1), new(2039, 1, 1), new(2040, 1, 1),
        new(2043, 1, 1), new(2044, 2, 1), new(2047, 2, 1), new(2135, 2, 1), new(2170, 1, 1),
        .. Array.ConvertAll(GoldPiles, gold => new ItemPile(1046, 1, gold))
    ];

    private readonly int _field;
    private readonly int _square;

    public MotherCubyRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "raid-mother-cuby", "raid-mother-cuby")
            .ForLevels(30, 49)
            .ForTeams(5, 15)
            .Requiring(1127, 1)
            .StartingAt(77, 167)
            .WithRoom(2511, out _field, 0, 1)
            .WithRoom(2512, out _square, 0, 0)
            .WithObjective(ObjectiveKind.KillMonster, Cuby, CubyCount)
            .WithObjective(ObjectiveKind.UseObject, LeverOff, (short)Levers.Length)
            .WithReward(4684, 1)
            .WithRaidHudValue(25)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromMinutes(40));
        run.SpawnPortal(_field, new Spot(154, 11), _square, new Spot(26, 40));
        run.SpawnPortal(_square, new Spot(26, 40), _field, new Spot(154, 11));
        run.SetMonsterLocker(_field, CubyCount, Cuby);
        for (var i = 0; i < Levers.Length; i++)
        {
            run.PlaceButton(_field, i + 1, Levers[i], LeverOff, LeverOn);
        }

        foreach (var (button, at, _) in TeleportLevers)
        {
            run.PlaceButton(_field, button, at, TeleportLeverOff, TeleportLeverOn);
        }

        run.Summon(_field, Pack(82, (16, 45), (16, 35), (15, 32), (14, 38), (14, 24), (13, 47), (13, 42), (12, 32), (11, 35), (11, 27), (10, 45)));
        run.Summon(_field, Pack(48, (64, 99), (61, 91), (58, 100), (57, 91), (52, 99), (52, 91), (46, 91), (45, 98), (41, 91), (39, 98), (34, 99), (32, 91)));
        run.Summon(_field, Pack(4, (147, 82), (147, 80), (146, 88), (146, 84), (143, 88), (143, 79), (141, 90), (139, 87), (139, 78), (138, 83), (138, 80), (135, 83), (38, 166), (37, 156), (36, 160), (34, 163), (32, 166), (27, 167), (21, 166), (20, 162), (15, 166), (14, 161)));
        run.Summon(_field, Pack(340, (142, 118), (141, 118), (140, 118), (139, 118), (138, 118), (137, 118), (135, 118), (134, 118), (133, 118), (132, 118), (130, 118)));
        run.Summon(_field, Pack(Cuby, (82, 118), (52, 16), (26, 33), (158, 48), (153, 19), (142, 84), (137, 59), (136, 153)));
        run.Summon(_field,
            CubyCalling(new Spot(32, 95), Pack(40, (51, 99), (44, 99), (42, 96), (42, 96), (45, 99), (42, 96), (42, 96), (42, 96), (42, 96), (42, 96), (42, 96), (42, 96), (42, 96))),
            new MonsterSpawn(Cuby, new Spot(20, 154)),
            CubyCalling(new Spot(108, 6), [
                .. Pack(45, (117, 13), (122, 12), (118, 13), (115, 9), (119, 15), (115, 9), (122, 11), (115, 9), (119, 10), (115, 17)),
                .. Pack(36, (118, 13), (117, 12))
            ]));
        run.Summon(_field, Pack(271, (57, 37), (41, 37)));
        run.Summon(_field, Pack(208, (156, 20), (155, 18), (154, 21), (153, 17), (152, 21), (151, 17), (150, 19), (129, 28), (107, 26)));
        run.Summon(_field, Pack(17, (26, 130), (25, 117), (22, 135), (21, 123), (21, 108)));
        run.Summon(_field, Pack(14, (108, 81), (106, 87), (105, 92), (104, 96), (104, 82), (102, 92), (101, 89), (70, 15), (66, 18), (45, 15), (37, 22), (36, 18), (32, 14)));
        run.Summon(_field, Pack(11, (26, 135), (24, 124), (24, 111), (23, 130), (21, 117), (21, 104)));
        run.Summon(_field, Pack(104, (28, 71), (27, 73), (26, 72), (24, 154)));
        run.Summon(_field, Pack(103, (76, 18), (75, 14), (67, 23), (57, 11)));
        run.Summon(_field, Pack(100, (133, 159), (122, 159), (116, 156), (113, 150), (112, 146), (110, 157), (110, 153), (109, 78), (108, 149), (107, 84), (107, 75), (105, 77), (103, 73), (102, 86)));
        run.Summon(_field, Pack(13, (161, 155), (161, 150), (160, 159), (160, 148), (153, 160), (153, 156), (153, 150)));
        run.Summon(_field, Pack(7, (83, 12), (81, 16), (79, 21), (57, 19), (57, 14), (54, 55), (54, 53), (54, 51), (54, 49), (45, 19), (41, 14), (157, 59), (155, 52), (155, 47)));

        run.Summon(_square, new MonsterSpawn(MotherCuby, BossAt, IsBoss: true));
        run.Summon(_square, Pack(WeakRedJelly, (7, 8)));
    }

    public override void OnRoomEntered(IInstanceRun run, int room)
    {
        if (room != _square)
        {
            return;
        }

        for (var at = run.Elapsed + FirstWave; at < TimeSpan.FromMinutes(40); at += WavesEvery)
        {
            run.At(at, r => r.Summon(_square, Waves));
        }
    }

    public override void OnRoomCleared(IInstanceRun run, int room) => OpenWhenReady(run, room);

    public override void OnButtonUsed(IInstanceRun run, int room, int button)
    {
        if (room != _field)
        {
            return;
        }

        switch (button)
        {
            case 1:
                run.Summon(_field, Pack(37, (78, 108), (78, 108), (78, 109), (79, 108), (79, 105), (81, 105), (80, 108), (81, 106), (78, 108), (79, 108), (77, 107), (79, 109), (80, 105), (78, 108), (77, 107), (79, 106), (81, 109), (81, 107), (79, 107), (78, 109), (78, 105), (78, 109), (79, 107), (80, 106), (81, 109), (81, 108), (80, 109), (78, 105), (78, 108), (80, 106)));
                break;
            case 3:
                run.Summon(_field, Pack(100, (53, 97), (57, 93), (42, 98), (48, 98), (40, 96), (47, 95), (35, 95), (42, 93), (39, 97), (53, 98), (41, 94), (52, 96), (63, 96), (55, 94), (42, 98), (40, 94), (49, 93), (33, 95), (54, 95), (64, 93)));
                break;
        }

        foreach (var (lever, at, to) in TeleportLevers)
        {
            if (lever == button)
            {
                run.Teleport(_field, at, TeleportReach, to);
            }
        }

        OpenWhenReady(run, room);
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum != MotherCuby)
        {
            return;
        }

        run.Throw(_square, BossAt, Loot);
        run.Succeed();
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();

    private void OpenWhenReady(IInstanceRun run, int room)
    {
        if (room != _field || run.LockerLeft(_field) > 0 || run.IsPortalOpen(_field, _square))
        {
            return;
        }

        for (var button = 1; button <= Levers.Length; button++)
        {
            if (!run.IsButtonOn(_field, button))
            {
                return;
            }
        }

        run.UnlockPortal(_field, _square);
        run.Message(Game18NConstString.ADoorHasOpened);
    }

    private MonsterSpawn CubyCalling(Spot at, MonsterSpawn[] called) =>
        new(Cuby, at, OnHpBelow: [new HpThreshold(50, r => r.Summon(_field, called))]);

    private static MonsterSpawn[] Pack(short vNum, params (short X, short Y)[] spots) =>
        Array.ConvertAll(spots, s => new MonsterSpawn(vNum, new Spot(s.X, s.Y)));
}

return new MotherCubyRaid();
