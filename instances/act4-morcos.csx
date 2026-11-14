// The rooms, the start point, the boss door and the first room's monsters follow a packet trace
// of the raid. The boss door opens half an hour after the raid does, and from then on more
// guards join the boss every minute and a half.
public class MorcosRaid : InstanceScript
{
    private const short Boss = 563;
    private static readonly TimeSpan Length = TimeSpan.FromHours(1);
    private static readonly TimeSpan BossDoorOpens = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GuardsEvery = TimeSpan.FromSeconds(90);
    private static readonly Spot BossAt = new(56, 11);

    private static readonly MonsterSpawn[] Guards =
    [
        new MonsterSpawn(561, new Spot(37, 25)),
        new MonsterSpawn(561, new Spot(49, 35)),
        new MonsterSpawn(561, new Spot(63, 24)),
        new MonsterSpawn(562, new Spot(41, 33)),
        new MonsterSpawn(562, new Spot(56, 34)),
        new MonsterSpawn(562, new Spot(67, 24))
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

    public MorcosRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(1, "act4-morcos", "act4-morcos")
            .ForLevels(60, 99)
            .StartingAt(40, 177)
            .WithRoom(135, out _entrance)
            .WithRoom(136, out _lair)
            .WithReward(882, 1, 50)
            .Build();
    }

    // Every family's run follows the hour the raid stays open, however late the family comes in.
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(Length - run.Elapsed);
        run.Summon(_entrance, Pack(559, (30, 127), (34, 46), (34, 131), (35, 125), (36, 116), (42, 139), (44, 98), (46, 95), (52, 78), (53, 37), (61, 24), (67, 33), (77, 87), (83, 39), (88, 59), (91, 60), (98, 141), (106, 51), (112, 45), (112, 48), (113, 44), (113, 158), (115, 97), (119, 42), (121, 140), (131, 61), (133, 41), (136, 83), (139, 94), (144, 123), (147, 125), (150, 142), (154, 155), (159, 151), (163, 127), (166, 134)));
        run.Summon(_entrance, Pack(560, (17, 105), (28, 69), (31, 116), (34, 42), (37, 151), (44, 11), (45, 108), (46, 74), (50, 151), (54, 126), (65, 56), (70, 40), (75, 94), (78, 21), (85, 60), (99, 53), (103, 73), (113, 106), (115, 40), (124, 162), (131, 159), (133, 144), (134, 94), (136, 146), (137, 97), (140, 93), (146, 90), (148, 87), (150, 158), (158, 93), (160, 159), (167, 149)));
        run.Summon(_lair, new MonsterSpawn(Boss, BossAt));
        run.At(BossDoorOpens, r => r.SpawnPortal(_entrance, new Spot(151, 45), _lair, new Spot(55, 80), locked: false));
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

return new MorcosRaid();
