// The rooms, the start point, the portal and the spawn spots follow a packet trace of the raid.
public class MotherCubyRaid : InstanceScript
{
    private const short MotherCuby = 282;
    private const short Cuby = 283;
    private const short WeakRedJelly = 167;
    private const short InvisibleJelly = 168;
    private const short WeakPinkJelly = 169;
    private const short WeakDarkJelly = 170;

    private readonly int _entrance;
    private readonly int _square;

    public MotherCubyRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "raid-mother-cuby", "raid-mother-cuby")
            .ForLevels(30, 49)
            .ForTeams(5, 15)
            .Requiring(1127, 1)
            .StartingAt(20, 50)
            .WithRoom(2510, out _entrance, 0, 1)
            .WithRoom(2512, out _square, 0, 0)
            .WithReward(4684, 1)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromMinutes(40));
        run.SpawnPortal(_entrance, new Spot(20, 5), _square, new Spot(26, 40), locked: false);
        run.Summon(_square,
            new MonsterSpawn(MotherCuby, new Spot(26, 28)),
            new MonsterSpawn(Cuby, new Spot(25, 27)),
            new MonsterSpawn(Cuby, new Spot(26, 27)),
            new MonsterSpawn(Cuby, new Spot(27, 27)),
            new MonsterSpawn(Cuby, new Spot(25, 28)),
            new MonsterSpawn(Cuby, new Spot(27, 28)),
            new MonsterSpawn(Cuby, new Spot(25, 29)),
            new MonsterSpawn(Cuby, new Spot(26, 29)),
            new MonsterSpawn(Cuby, new Spot(27, 29)));
        run.Summon(_square, Pack(WeakRedJelly, (7, 8), (11, 12), (11, 9), (4, 10), (6, 12), (6, 9), (8, 10), (9, 6)));
        run.Summon(_square, Pack(InvisibleJelly, (4, 47), (6, 49), (8, 45), (8, 50), (10, 47), (11, 51), (12, 49)));
        run.Summon(_square, Pack(WeakPinkJelly, (42, 49), (43, 51), (44, 44), (45, 48), (47, 46), (49, 44), (49, 47)));
        run.Summon(_square, Pack(WeakDarkJelly, (41, 8), (42, 4), (43, 7), (45, 5), (46, 9), (46, 12), (48, 8)));
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum == MotherCuby)
        {
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();

    private static MonsterSpawn[] Pack(short vNum, params (short X, short Y)[] spots) =>
        Array.ConvertAll(spots, s => new MonsterSpawn(vNum, new Spot(s.X, s.Y)));
}

return new MotherCubyRaid();
