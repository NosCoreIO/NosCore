// Each side sends its fighters into the ring one at a time; a fighter who falls, or is still
// standing when the bout's time is up, is done for the match. The match is over once a side
// has nobody left to send.
public class TalentArena : InstanceScript
{
    private static readonly TimeSpan Preparation = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan BoutCountdown = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BoutLength = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BetweenBouts = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(1);

    private readonly int _ring;

    public TalentArena()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "talent-arena", "talent-arena")
            .WithRoom(2015, out _ring)
            .WithSide(new Spot(14, 38), new Spot(19, 40), new Spot(56, 40))
            .WithSide(new Spot(125, 37), new Spot(120, 39), new Spot(87, 39))
            .WithReward(2800, 1)
            .WithConsolation(2801, 3)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.ShowCountdown(_ring, Preparation);
        run.At(Preparation, NextBout);
    }

    private void NextBout(IInstanceRun run)
    {
        if (!run.CallFighters(_ring))
        {
            run.Succeed();
            return;
        }

        var start = run.Elapsed;
        run.ShowCountdown(_ring, BoutCountdown + BoutLength);
        run.At(start + CheckEvery, r => Watch(r, start, false));
    }

    private void Watch(IInstanceRun run, TimeSpan start, bool open)
    {
        if (run.FightersStanding(_ring) < 2)
        {
            run.At(run.Elapsed + BetweenBouts, NextBout);
            return;
        }

        if (run.Elapsed >= start + BoutCountdown + BoutLength)
        {
            run.EndBout(_ring);
            run.At(run.Elapsed + BetweenBouts, NextBout);
            return;
        }

        if (!open && run.Elapsed >= start + BoutCountdown)
        {
            run.OpenBout(_ring);
            open = true;
        }

        run.At(run.Elapsed + CheckEvery, r => Watch(r, start, open));
    }
}

return new TalentArena();
