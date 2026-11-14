// Each wave lands 160 s after the previous one, what it leaves behind is scattered 130 s
// after it lands, and the fight can be won once the last wave is out.
public class InstantCombat : InstanceScript
{
    private const byte LastWave = 4;
    private static readonly TimeSpan Arrival = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FirstWave = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaveInterval = TimeSpan.FromSeconds(160);
    private static readonly TimeSpan DropDelay = TimeSpan.FromSeconds(130);
    private static readonly TimeSpan WarningBeforeWave = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WinnableFrom = TimeSpan.FromSeconds(720);
    private static readonly TimeSpan ThirtySecondsLeft = TimeSpan.FromSeconds(870);
    private static readonly TimeSpan Length = TimeSpan.FromSeconds(900);

    private readonly int _arena;

    public InstantCombat()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "instant-combat", "instant-combat")
            .WithRoom(2004, out _arena)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        for (byte wave = 0; wave <= LastWave; wave++)
        {
            var current = wave;
            var lands = Arrival + FirstWave + WaveInterval * wave;
            run.At(lands - WarningBeforeWave, r => r.Message(Game18NConstString.MonstersAreApproaching));
            run.At(lands, r =>
            {
                r.SummonWave(_arena, current);
                r.Message(Game18NConstString.MonsterHaveAppeared);
            });
            run.At(lands + DropDelay, r => r.DropWave(_arena, current));
        }

        run.At(Arrival + WinnableFrom, TryWin);
        run.At(Arrival + ThirtySecondsLeft, r => r.Message(Game18NConstString.InstantCombatThirtySecondsRemaining));
        run.At(Arrival + Length, r =>
        {
            r.Message(Game18NConstString.InstantCombatElapsed);
            r.Message(Game18NConstString.ReturnToStartingPositionTenSeconds);
            r.Fail();
        });
    }

    public override void OnMonsterKilled(IInstanceRun run, int room, short vNum) => TryWin(run);

    private void TryWin(IInstanceRun run)
    {
        if (run.Elapsed < Arrival + WinnableFrom || run.MonstersAlive(_arena) > 0)
        {
            return;
        }

        run.Message(Game18NConstString.InstantCombatSuccess);
        run.Message(Game18NConstString.ReturnToStartingPositionTenSeconds);
        run.Succeed();
    }
}

return new InstantCombat();
