// The rooms, the start point, the lever, the portal and the boss's spot follow a packet trace of
// the raid. The ghosts the trace shows around Glacerus come in as NPCs, so they are left out until
// it is known whether they fight for the team or against it.
public class GlacerusRaid : InstanceScript
{
    private const short Glacerus = 2049;
    private const int Lever = 1;

    private readonly int _grotto;
    private readonly int _den;

    public GlacerusRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(17, "raid-glacerus", "raid-glacerus")
            .ForLevels(80, 99)
            .ForTeams(5, 15)
            .Requiring(5512, 1)
            .StartingAt(13, 39)
            .WithRoom(2588, out _grotto, 0, 1)
            .WithRoom(2589, out _den, 0, 0)
            .WithReward(4772, 1)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromHours(1));
        run.SpawnPortal(_grotto, new Spot(13, 7), _den, new Spot(32, 48));
        run.PlaceButton(_grotto, Lever, new Spot(13, 21), 1000, 1045);
        run.Summon(_den, new MonsterSpawn(Glacerus, new Spot(22, 13)));
    }

    public override void OnButtonUsed(IInstanceRun run, int room, int button) => run.UnlockPortal(_grotto, _den);

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum == Glacerus)
        {
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();
}

return new GlacerusRaid();
