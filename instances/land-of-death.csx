// Each family gets its own land, and every land follows the same hour-long timeline from the
// moment the event opens. From half time the Flying Fire Devil hunts the last player to arrive
// for a minute every four minutes, and experience and drops are tripled while it is out.
public class LandOfDeath : InstanceScript
{
    private const short FlyingFireDevil = 443;
    private const int HuntRate = 3;
    private static readonly TimeSpan Length = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan FirstHunt = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HuntEvery = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan HuntStay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ClosingWarning = TimeSpan.FromMinutes(55);

    private readonly int _land;

    public LandOfDeath()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "land-of-death", "land-of-death")
            .WithMapRoom(150, out _land)
            .Build();
    }

    // A land first reached during a hunt gets its devil at once; hunts already over are skipped.
    public override void OnFirstEnter(IInstanceRun run)
    {
        run.StartClock(Length - run.Elapsed);
        for (var start = FirstHunt; start < Length; start += HuntEvery)
        {
            if (start + HuntStay <= run.Elapsed)
            {
                continue;
            }

            run.At(start, r =>
            {
                r.SetRates(_land, HuntRate, HuntRate);
                r.Hunt(_land, FlyingFireDevil);
                r.Message(Game18NConstString.FlyingFireDevilAppears);
            });
            run.At(start + HuntStay, r =>
            {
                r.SetRates(_land, 1, 1);
                r.Dismiss(_land, FlyingFireDevil);
                r.Message(Game18NConstString.FlyingFireDevilVanished);
            });
        }

        if (ClosingWarning > run.Elapsed)
        {
            run.At(ClosingWarning, r => r.Message(Game18NConstString.LandOfDeathCloseSoon));
        }
    }

    public override void OnTimeout(IInstanceRun run) => run.Fail();
}

return new LandOfDeath();
