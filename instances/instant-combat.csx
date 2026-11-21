// The fight lasts 15 minutes from the first wave. Each wave's prize goes into every bag 30 s before
// the next wave lands, and the fight can be won once the last wave is out.
public class InstantCombat : InstanceScript
{
    private static readonly TimeSpan[] Waves =
    [
        TimeSpan.FromSeconds(24),
        TimeSpan.FromSeconds(184),
        TimeSpan.FromSeconds(334),
        TimeSpan.FromSeconds(484),
        TimeSpan.FromSeconds(634),
    ];

    private static readonly TimeSpan[] FirstWaveWarnings = [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3)];
    private const int NextWaveCountdownSeconds = 40;
    private static readonly TimeSpan ApproachWarning = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PrizeBeforeNextWave = TimeSpan.FromSeconds(30);
    private static readonly int[] MinutesLeftAnnounced = [12, 10, 5, 4, 3, 2, 1];
    private static readonly TimeSpan ThirtySecondsLeft = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Length = TimeSpan.FromMinutes(15);

    private readonly int _arena;

    public InstantCombat()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "instant-combat", "instant-combat")
            .WithRoom(2004, out _arena)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        for (byte wave = 0; wave < Waves.Length; wave++)
        {
            var current = wave;
            var lands = Waves[wave];
            if (wave == 0)
            {
                foreach (var warning in FirstWaveWarnings)
                {
                    run.At(lands - warning, r => r.Message(Game18NConstString.MonstersAreApproaching));
                }

                run.At(lands, r =>
                {
                    r.SummonWave(_arena, current);
                    r.Message(Game18NConstString.MonsterHaveAppeared);
                });
            }
            else
            {
                run.At(lands - TimeSpan.FromSeconds(NextWaveCountdownSeconds),
                    r => r.Message(Game18NConstString.MonsterWillAppearsInSeconds, NextWaveCountdownSeconds));
                run.At(lands - ApproachWarning, r => r.Message(Game18NConstString.MonstersAreApproaching));
                run.At(lands, r => r.SummonWave(_arena, current));
                run.At(lands - PrizeBeforeNextWave, r => r.DropWave(_arena, (byte)(current - 1)));
            }
        }

        var end = Waves[0] + Length;
        foreach (var minutes in MinutesLeftAnnounced)
        {
            run.At(end - TimeSpan.FromMinutes(minutes), r => r.Message(Game18NConstString.BattleEndInMinutes, minutes));
        }

        run.At(end - ThirtySecondsLeft, r => r.Message(Game18NConstString.InstantCombatThirtySecondsRemaining));
        run.At(end, r => r.Fail());
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum)
    {
        if (run.Elapsed < Waves[^1] || run.MonstersAlive(_arena) > 0)
        {
            return;
        }

        run.Message(Game18NConstString.InstantCombatSuccess);
        run.Message(Game18NConstString.ReturnToStartingPositionTenSeconds);
        run.Succeed();
    }
}

return new InstantCombat();
