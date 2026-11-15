// The rooms, the start point, the portals, the levers and the monsters follow a packet trace of
// the raid. Each of the two lava fields opens once its levers are pulled and its Fiery Giants are down.
public class GrenigasRaid : InstanceScript
{
    private const short Grenigas = 1058;
    private const short FieryGiant = 1925;
    private const short LeverOff = 1135;
    private const short LeverOn = 1136;

    private static readonly Spot[] PathLevers = [new(41, 162), new(147, 31)];
    private static readonly Spot[] WayLevers = [new(34, 75), new(167, 30), new(165, 168)];

    private readonly int _path;
    private readonly int _way;
    private readonly int _temple;

    public GrenigasRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(15, "raid-grenigas", "raid-grenigas")
            .ForLevels(85, 99)
            .ForTeams(5, 15)
            .Requiring(5922, 1)
            .StartingAt(169, 182)
            .WithRoom(2537, out _path, 0, 2)
            .WithRoom(2538, out _way, 0, 1)
            .WithRoom(2539, out _temple, 0, 0)
            .WithReward(4698, 1)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromHours(2));
        run.SpawnPortal(_path, new Spot(17, 21), _way, new Spot(13, 187));
        run.SpawnPortal(_way, new Spot(48, 10), _temple, new Spot(50, 89));
        run.SetMonsterLocker(_path, 2, FieryGiant);
        run.SetMonsterLocker(_way, 3, FieryGiant);
        PlaceLevers(run, _path, PathLevers);
        PlaceLevers(run, _way, WayLevers);

        run.Summon(_path, Pack(FieryGiant, (79, 44), (120, 145)));
        run.Summon(_path, Pack(1909, (77, 41)));
        run.Summon(_path, Pack(1910, (83, 45), (81, 76), (79, 132), (74, 47), (65, 145), (63, 54), (47, 139), (41, 34), (37, 47), (159, 115), (155, 107), (153, 161), (150, 60), (149, 150), (148, 43), (138, 47), (138, 39), (133, 56), (120, 141), (119, 126), (116, 154), (114, 144), (104, 46)));
        run.Summon(_path, Pack(1911, (97, 146), (87, 109), (86, 39), (84, 55), (72, 143), (71, 61), (71, 138), (54, 153), (52, 81), (45, 72), (44, 51), (43, 122), (37, 105), (34, 77), (34, 157), (159, 131), (155, 93), (152, 86), (146, 124), (129, 145), (122, 150), (119, 41), (103, 134)));
        run.Summon(_path, Pack(1920, (93, 118), (84, 140), (81, 90), (46, 163), (42, 58), (41, 65), (40, 153), (39, 114), (38, 162), (160, 143), (159, 103), (149, 28), (145, 54), (145, 159), (143, 75), (143, 33), (113, 108), (103, 59)));

        run.Summon(_way, Pack(FieryGiant, (89, 33), (87, 123), (133, 72)));
        run.Summon(_way, Pack(1911, (94, 23), (79, 141), (86, 118), (77, 141), (66, 154), (60, 31), (34, 81), (36, 79), (156, 165), (169, 27), (163, 170), (162, 89), (161, 166), (160, 153), (167, 36), (133, 68), (110, 48), (101, 137), (102, 68)));
        run.Summon(_way, Pack(1913, (91, 36)));
        run.Summon(_way, Pack(1918, (93, 45), (79, 36), (84, 122), (68, 158), (45, 89), (31, 147), (32, 93), (38, 80), (165, 33), (163, 116), (161, 44), (144, 73), (149, 90), (145, 80), (134, 76), (131, 164), (125, 153), (110, 53), (100, 39)));
        run.Summon(_way, Pack(1919, (87, 125), (84, 36), (79, 114), (73, 152), (72, 108), (61, 142), (37, 82), (38, 75), (31, 77), (30, 146), (172, 32), (165, 123), (165, 164), (164, 27), (162, 171), (165, 42), (150, 94), (158, 37), (121, 142), (133, 72), (122, 69), (119, 161)));
        run.Summon(_way, Pack(1920, (91, 127), (78, 28), (91, 29), (64, 128), (32, 118), (167, 36), (159, 170), (146, 103)));

        run.Summon(_temple, new MonsterSpawn(Grenigas, new Spot(42, 56)));
        run.Summon(_temple, Pack(1054, (51, 49)));
        run.Summon(_temple, Pack(2018, (47, 84), (31, 85), (64, 61), (64, 39), (63, 75)));
    }

    public override void OnRoomCleared(IInstanceRun run, int room) => OpenWhenReady(run, room);

    public override void OnButtonUsed(IInstanceRun run, int room, int button) => OpenWhenReady(run, room);

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum == Grenigas)
        {
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();

    private void OpenWhenReady(IInstanceRun run, int room)
    {
        var (levers, next) = room == _path ? (PathLevers.Length, _way)
            : room == _way ? (WayLevers.Length, _temple)
            : (0, 0);
        if (levers == 0 || run.LockerLeft(room) > 0)
        {
            return;
        }

        for (var button = 1; button <= levers; button++)
        {
            if (!run.IsButtonOn(room, button))
            {
                return;
            }
        }

        run.UnlockPortal(room, next);
    }

    private static void PlaceLevers(IInstanceRun run, int room, Spot[] levers)
    {
        for (var i = 0; i < levers.Length; i++)
        {
            run.PlaceButton(room, i + 1, levers[i], LeverOff, LeverOn);
        }
    }

    private static MonsterSpawn[] Pack(short vNum, params (short X, short Y)[] spots) =>
        Array.ConvertAll(spots, s => new MonsterSpawn(vNum, new Spot(s.X, s.Y)));
}

return new GrenigasRaid();
