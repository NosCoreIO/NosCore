// The ship to Glacernon. Boarding opens 25 minutes before it sails; the passengers are warned as
// the departure nears, and once it sails the service lands them in their side's citadel.
public class Act4Ship : InstanceScript
{
    private const short Captain = 540;
    private const short FabianFrost = 613;
    private static readonly TimeSpan Sails = TimeSpan.FromMinutes(25);

    private readonly int _deck;

    public Act4Ship()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "act4-ship", "act4-ship")
            .WithMapRoom(148, out _deck, Captain, FabianFrost)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        Warn(run, TimeSpan.FromMinutes(4), r => r.Message(Game18NConstString.ShipLeaveInMinutes, 4));
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

return new Act4Ship();
