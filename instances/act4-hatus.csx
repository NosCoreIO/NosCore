// The rooms, the start point, the boss door and the first room's monsters follow a packet trace
// of the raid. The boss door opens half an hour after the raid does, and from then on more
// guards join the boss every minute and a half.
public class HatusRaid : InstanceScript
{
    private const short Boss = 577;
    private static readonly TimeSpan Length = TimeSpan.FromHours(1);
    private static readonly TimeSpan BossDoorOpens = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GuardsEvery = TimeSpan.FromSeconds(90);
    private static readonly Spot BossAt = new(36, 18);

    private static readonly MonsterSpawn[] Guards =
    [
        new MonsterSpawn(575, new Spot(20, 22)),
        new MonsterSpawn(575, new Spot(22, 25)),
        new MonsterSpawn(575, new Spot(31, 29)),
        new MonsterSpawn(575, new Spot(36, 30)),
        new MonsterSpawn(575, new Spot(42, 29)),
        new MonsterSpawn(575, new Spot(48, 27))
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

    public HatusRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(2, "act4-hatus", "act4-hatus")
            .ForLevels(60, 99)
            .StartingAt(14, 6)
            .WithRoom(137, out _entrance)
            .WithRoom(138, out _lair)
            .WithReward(185, 1, 50)
            .Build();
    }

    // Every family's run follows the hour the raid stays open, however late the family comes in.
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(Length - run.Elapsed);
        run.Summon(_entrance, Pack(573, (22, 99), (36, 153), (42, 94), (53, 7), (59, 29), (83, 124), (85, 17), (87, 121), (90, 13), (95, 139), (101, 19), (107, 116), (119, 156), (126, 154), (146, 73), (147, 135), (150, 83), (154, 32), (156, 14), (156, 21), (158, 87), (164, 139)));
        run.Summon(_entrance, Pack(574, (26, 129), (45, 143), (53, 23), (59, 8), (82, 112), (83, 129), (101, 122), (102, 52), (104, 38), (107, 42), (111, 53), (113, 19), (114, 96), (118, 149), (124, 151), (130, 143), (133, 156), (136, 144), (144, 16), (151, 41), (157, 74), (170, 88)));
        run.Summon(_entrance, Pack(576, (24, 82), (34, 120), (41, 146), (65, 132), (74, 125), (75, 20), (94, 112), (95, 25), (100, 130), (105, 78), (106, 99), (115, 108), (119, 103), (125, 29), (128, 158), (137, 89), (139, 10), (145, 9), (147, 144), (153, 141), (156, 134), (167, 35)));
        run.Summon(_lair, new MonsterSpawn(Boss, BossAt));
        run.At(BossDoorOpens, r => r.SpawnPortal(_entrance, new Spot(37, 157), _lair, new Spot(35, 59), locked: false));
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

return new HatusRaid();
