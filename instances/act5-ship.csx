// The ship to the Mortaz Desert. It sails every two minutes; the passengers are warned as the
// departure nears, and once it sails the service lands them at the desert harbour.
public class Act5Ship : InstanceScript
{
    private const short Sailor = 1350;
    private static readonly TimeSpan Sails = TimeSpan.FromMinutes(2);

    private readonly int _deck;

    public Act5Ship()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "act5-ship", "act5-ship")
            .WithMapRoom(148, out _deck, Sailor)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        Warn(run, TimeSpan.FromMinutes(1), r => r.Message(Game18NConstString.ShipLeaveInOneMinute));
        Warn(run, TimeSpan.FromSeconds(30), r => r.Message(Game18NConstString.ShipLeaveInThirtySeconds));
        Warn(run, TimeSpan.FromSeconds(10), r => r.Message(Game18NConstString.ShipLeaveInTenSeconds));
        run.At(Sails, r =>
        {
            r.Message(Game18NConstString.RaiseTheAnchor);
            r.Succeed();
        });
    }

    private static void Warn(IInstanceRun run, TimeSpan before, Action<IInstanceRun> warning)
    {
        if (Sails - before > run.Elapsed)
        {
            run.At(Sails - before, warning);
        }
    }
}

return new Act5Ship();
