// The rooms, the start point, the boss door and the first room's monsters follow a packet trace
// of the raid. The boss door opens half an hour after the raid does, and from then on more
// guards join the boss every minute and a half.
// No trace shows a player arriving in the boss room or where its guards stand, so the arrival
// cell and the guard spots are a guess.
public class BeriosRaid : InstanceScript
{
    private const short Boss = 624;
    private static readonly TimeSpan Length = TimeSpan.FromHours(1);
    private static readonly TimeSpan BossDoorOpens = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GuardsEvery = TimeSpan.FromSeconds(90);
    private static readonly Spot BossAt = new(30, 28);

    private static readonly MonsterSpawn[] Guards =
    [
        new MonsterSpawn(780, new Spot(30, 38)),
        new MonsterSpawn(781, new Spot(30, 18)),
        new MonsterSpawn(782, new Spot(20, 30)),
        new MonsterSpawn(782, new Spot(40, 30)),
        new MonsterSpawn(783, new Spot(22, 38)),
        new MonsterSpawn(783, new Spot(38, 38))
    ];

    private static readonly ItemPile[] Loot =
    [
        new(1046, 20, 20000),
        new(2027, 10, 5),
        new(1019, 5, 1),
        new(1009, 5, 3),
        new(1011, 5, 3),
    ];

    private readonly int _entrance;
    private readonly int _lair;

    public BeriosRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(4, "act4-berios", "act4-berios")
            .ForLevels(60, 99)
            .StartingAt(17, 17)
            .WithRoom(141, out _entrance)
            .WithRoom(142, out _lair)
            .WithReward(999, 1, 50, randomRare: true)
            .Build();
    }

    // Every family's run follows the hour the raid stays open, however late the family comes in.
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(Length - run.Elapsed);
        run.Summon(_entrance, Pack(617, (10, 146), (13, 60), (22, 103), (26, 82), (31, 111), (35, 141), (38, 100), (40, 33), (49, 83), (52, 135), (60, 141), (62, 35), (63, 143), (64, 143), (65, 78), (65, 78), (70, 69), (76, 155), (80, 78), (82, 117), (82, 177), (82, 177), (84, 9), (84, 18), (84, 18), (85, 158), (87, 58), (87, 167), (93, 51), (98, 141), (100, 95), (111, 109), (113, 87), (123, 159), (128, 33), (131, 85), (132, 56), (146, 151), (149, 27), (158, 148), (164, 157), (167, 63), (168, 43)));
        run.Summon(_entrance, Pack(618, (15, 176), (29, 175), (40, 58), (42, 116), (51, 66), (57, 125), (71, 164), (75, 38), (89, 72), (98, 115), (107, 122), (108, 112), (118, 23), (126, 150), (144, 20), (151, 77), (153, 30), (162, 41), (164, 167), (175, 132)));
        run.Summon(_entrance, Pack(619, (25, 106), (32, 176), (44, 142), (48, 78), (49, 159), (60, 61), (79, 47), (88, 141), (89, 13), (89, 91), (89, 105), (93, 174), (97, 11), (102, 132), (103, 93), (127, 94), (145, 64), (163, 64), (168, 122), (173, 147)));
        run.Summon(_entrance, Pack(620, (17, 110), (30, 150), (35, 145), (62, 46), (73, 24), (74, 86), (75, 10), (101, 130)));
        run.Summon(_lair, new MonsterSpawn(Boss, BossAt));
        run.At(BossDoorOpens, r => r.SpawnPortal(_entrance, new Spot(188, 97), _lair, new Spot(30, 48), locked: false));
        for (var at = BossDoorOpens; at < Length; at += GuardsEvery)
        {
            if (at < run.Elapsed)
            {
                continue;
            }

            run.At(at, r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_lair, Guards);
            });
        }
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum != Boss)
        {
            return;
        }

        run.Message(Game18NConstString.CongratulationsKilledTheBoss);
        run.Throw(_lair, BossAt, Loot);
        run.Succeed();
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();

    private static MonsterSpawn[] Pack(short vNum, params (short X, short Y)[] spots) =>
        Array.ConvertAll(spots, s => new MonsterSpawn(vNum, new Spot(s.X, s.Y)));
}

return new BeriosRaid();
