// PvP opens after three warnings; the fight is won once everyone still standing is on one side.
// Nothing in the client data gives its length, so ten minutes keeps two players who avoid each
// other from holding the map forever.
public class IceBreaker : InstanceScript
{
    private static readonly TimeSpan[] PvpWarnings =
        [TimeSpan.Zero, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(13)];
    private static readonly TimeSpan BeginAttack = TimeSpan.FromSeconds(14);
    private static readonly TimeSpan Length = TimeSpan.FromSeconds(600);
    private static readonly TimeSpan LastWarning = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(1);

    private readonly int _arena;

    public IceBreaker()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "ice-breaker", "ice-breaker")
            .WithRoom(2005, out _arena)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        if (run.PlayersIn(_arena) < 2)
        {
            run.Succeed();
            return;
        }

        foreach (var warning in PvpWarnings)
        {
            run.At(warning, r => r.Message(Game18NConstString.PvpWillBeAvailable));
        }

        run.At(BeginAttack, r =>
        {
            r.Message(Game18NConstString.BeginAttack);
            r.SetPvp(_arena, true);
            Check(r);
        });
        run.At(BeginAttack + Length - LastWarning, r => r.Message(Game18NConstString.IcebreakerWillEndInThirtySeconds));
        run.At(BeginAttack + Length, r =>
        {
            r.Message(Game18NConstString.IcebreakerTimeElapsed);
            r.Fail();
        });
    }

    private void Check(IInstanceRun run)
    {
        if (run.OneSideStanding(_arena))
        {
            run.Succeed();
            return;
        }

        run.At(run.Elapsed + CheckEvery, Check);
    }
}

return new IceBreaker();
