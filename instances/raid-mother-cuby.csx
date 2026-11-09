// The map, the start point and the spawn spots have not been checked against the client's map
// files; only the seal, the monsters, the level range, the team size, the clock and the box have.
public class MotherCubyRaid : InstanceScript
{
    private const short MotherCuby = 282;
    private const short Cuby = 283;

    private readonly int _jelly;

    public MotherCubyRaid()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "raid-mother-cuby", "raid-mother-cuby")
            .ForLevels(30, 49)
            .ForTeams(5, 15)
            .Requiring(1127, 1)
            .StartingAt(10, 10)
            .WithRoom(2510, out _jelly)
            .WithReward(4684, 1)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(TimeSpan.FromMinutes(40));
        run.Summon(_jelly,
            new MonsterSpawn(Cuby, new Spot(14, 10)),
            new MonsterSpawn(Cuby, new Spot(14, 14)),
            new MonsterSpawn(Cuby, new Spot(18, 12)),
            new MonsterSpawn(MotherCuby, new Spot(22, 12)));
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (vNum == MotherCuby)
        {
            run.Succeed();
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();
}

return new MotherCubyRaid();
