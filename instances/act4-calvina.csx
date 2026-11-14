// The rooms, the start point, the boss door and the first room's monsters follow a packet trace
// of the raid. The boss door opens half an hour after the raid does, and from then on more
// guards join the boss every minute and a half.
// No trace shows a player arriving in the boss room or where its guards stand, so the arrival
// cell and the guard spots are a guess.
public class CalvinaRaid : InstanceScript
{
    private const short Boss = 629;
    private static readonly TimeSpan Length = TimeSpan.FromHours(1);
    private static readonly TimeSpan BossDoorOpens = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GuardsEvery = TimeSpan.FromSeconds(90);
    private static readonly Spot BossAt = new(26, 25);

    private static readonly MonsterSpawn[] Guards =
    [
        new MonsterSpawn(770, new Spot(18, 32)),
        new MonsterSpawn(770, new Spot(26, 35)),
        new MonsterSpawn(770, new Spot(34, 32)),
        new MonsterSpawn(771, new Spot(16, 22)),
        new MonsterSpawn(771, new Spot(36, 22)),
        new MonsterSpawn(771, new Spot(26, 14))
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

    public CalvinaRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(3, "act4-calvina", "act4-calvina")
            .ForLevels(60, 99)
            .StartingAt(26, 7)
            .WithRoom(139, out _entrance)
            .WithRoom(140, out _lair)
            .WithReward(942, 1, 50)
            .Build();
    }

    // Every family's run follows the hour the raid stays open, however late the family comes in.
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(Length - run.Elapsed);
        run.Summon(_entrance, Pack(625, (8, 72), (23, 67), (37, 83), (58, 120), (69, 11), (70, 52), (79, 19), (90, 113), (91, 120), (93, 125), (112, 90), (114, 9), (144, 67), (156, 75), (172, 130), (181, 24), (182, 60), (200, 23)));
        run.Summon(_entrance, Pack(626, (6, 77), (31, 69), (48, 38), (49, 89), (53, 30), (74, 13), (74, 104), (80, 106), (98, 25), (124, 33), (138, 126), (141, 63), (153, 43), (179, 8), (180, 55), (182, 60), (189, 43), (197, 78)));
        run.Summon(_entrance, Pack(627, (18, 123), (22, 75), (36, 104), (46, 49), (58, 86), (71, 24), (77, 57), (87, 122), (105, 28), (123, 57), (123, 100), (161, 83), (173, 16), (175, 40), (180, 123)));
        run.Summon(_entrance, Pack(628, (10, 34), (14, 66), (28, 69), (30, 107), (35, 109), (41, 57), (56, 19), (59, 18), (59, 79), (74, 23), (90, 103), (104, 15), (120, 92), (120, 106), (127, 63), (163, 45), (166, 38), (168, 117), (172, 58), (176, 11), (181, 133), (185, 37), (192, 14)));
        run.Summon(_lair, new MonsterSpawn(Boss, BossAt));
        run.At(BossDoorOpens, r => r.SpawnPortal(_entrance, new Spot(201, 93), _lair, new Spot(26, 45), locked: false));
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

return new CalvinaRaid();
