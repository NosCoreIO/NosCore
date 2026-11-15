// Red against blue on the crystal field: each side starts at its own edge of the map, and the
// side holding the most points when the clock runs out wins. Points come from the Rainbow
// Crystals a side holds, so the crystals are placed when the battle opens. Teams counts the
// players of a whole battle: fewer than the minimum start nothing, more than the maximum
// open a second battle.
public class RainbowBattle : InstanceScript
{
    private const short LargeCrystal = 922;
    private const short MediumCrystal = 923;
    private const short SmallCrystal = 924;

    private static readonly TimeSpan Length = TimeSpan.FromMinutes(7);
    private static readonly TimeSpan LastWarning = TimeSpan.FromSeconds(30);

    private readonly int _field;

    public RainbowBattle()
    {
        Definition = InstanceDefinitionBuilder.Named(0, "rainbow-battle", "rainbow-battle")
            .ForLevels(70, 99)
            .ForTeams(10, 30)
            .WithRoom(2010, out _field)
            .WithBase(new Spot(0, 25), new Spot(1, 50))
            .WithBase(new Spot(117, 27), new Spot(118, 57))
            .WithReward(2361, 3)
            .WithConsolation(2361, 1)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun run)
    {
        run.PlaceNpc(_field, LargeCrystal, new Spot(59, 40));
        run.PlaceNpc(_field, MediumCrystal, new Spot(74, 53));
        run.PlaceNpc(_field, MediumCrystal, new Spot(43, 26));
        run.PlaceNpc(_field, MediumCrystal, new Spot(32, 75));
        run.PlaceNpc(_field, MediumCrystal, new Spot(85, 4));
        run.PlaceNpc(_field, SmallCrystal, new Spot(15, 40));
        run.PlaceNpc(_field, SmallCrystal, new Spot(102, 39));

        run.ShowCountdown(_field, Length);
        run.Message(Game18NConstString.BattleStarted);
        run.At(Length - LastWarning, r => r.Message(Game18NConstString.TheBattleEndsShortly, (long)LastWarning.TotalSeconds));
        run.At(Length, r => r.Succeed());
    }
}

return new RainbowBattle();
