public class TimeSpace17 : TimeSpaceScript
{
    public TimeSpace17() : base(17, "Becoming a Warrior")
    {
        Levels(27, 99);
        Start(7, 9);
        Requires(1012, 2);
        Lives(2);
        Clock(TimeSpan.FromSeconds(140));
        Draw(163, 1);
        Draw(169, 1);
        Draw(175, 1);
        Special(2101, 40);
        Reward(2099, 2);
        Reward(1087, 1);

        var entrance = Room(4203, 0, 8);
        var briefing = Room(4207, 0, 9);
        var crossroads = Room(4213, 1, 9);
        var training = Room(4201, 1, 10);
        var yard = Room(4211, 2, 9);
        var firstDuel = Room(4203, 2, 8);
        var gauntlet = Room(4209, 3, 9);
        var secondDuel = Room(4206, 3, 10);

        entrance.Portal(new(14, 28), briefing, new(14, 1), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        briefing.Portal(new(14, 1), entrance, new(14, 28), locked: false);
        briefing.Portal(new(28, 15), crossroads, new(1, 15), locked: false);
        briefing.OnEnter(r => r.Dialog(6083));

        var crossroadsToYard = crossroads.Portal(new(28, 15), yard, new(1, 15));
        crossroads.Portal(new(14, 28), training, new(14, 1), locked: false);
        crossroads.Portal(new(1, 15), briefing, new(28, 15), locked: false);

        var trainingToCrossroads = training.Portal(new(14, 1), crossroads, new(14, 28));
        training.Spawn(Monster(66).At(17, 13).Bonus(), Monster(66).At(9, 15));
        training.Spawn(Npc(318).At(11, 15));
        training.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_TOM_FIGHTS_MONSTERS))
            .OnCleared(r => r
                .Unlock(trainingToCrossroads)
                .AddTime(TimeSpan.FromSeconds(120))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6084)
                .Unlock(crossroadsToYard));

        yard.Portal(new(14, 1), firstDuel, new(14, 28), locked: false);
        var yardToGauntlet = yard.Portal(new(29, 15), gauntlet, new(1, 15));
        yard.Portal(new(1, 15), crossroads, new(28, 15), locked: false);
        yard.Spawn(
            Monster(58).At(25, 13).Bonus(),
            Monster(58).At(20, 11).Bonus().OnDeath(r => r.Spawn(Monster(58).At(12, 20))),
            Monster(58).At(14, 8).OnDeath(r => r.Spawn(
                Monster(58).At(6, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(66).At(20, 17).OnDeath(r => r.Spawn(Monster(66).At(11, 17), Monster(66).At(16, 13))))))));

        var firstDuelToYard = firstDuel.Portal(new(14, 28), yard, new(14, 1));
        firstDuel.OnEnter(r => r.Dialog(6085))
            .OnDialogClosed(6085, r => r
                .Spawn(Monster(318).At(14, 12))
                .Objective(LanguageKey.TIMESPACE_GOAL_FIGHT_KNIGHT_TOM))
            .OnCleared(r => r
                .Unlock(firstDuelToYard)
                .AddTime(TimeSpan.FromSeconds(130))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6086)
                .Unlock(yardToGauntlet));

        gauntlet.Portal(new(14, 28), secondDuel, new(14, 1), locked: false);
        gauntlet.Portal(new(1, 15), yard, new(29, 15), locked: false);
        gauntlet.Spawn(
            Monster(58).At(21, 13).Bonus().OnDeath(r => r.Spawn(Monster(66).At(16, 12))),
            Monster(58).At(15, 15).OnDeath(r => r.Spawn(Monster(58).At(7, 14), Monster(58).At(10, 10))),
            Monster(58).At(12, 19).OnDeath(r => r.Spawn(
                Monster(66).At(19, 18).OnDeath(r => r.Spawn(Monster(66).At(12, 12))))));

        secondDuel.Portal(new(14, 1), gauntlet, new(14, 28), locked: false);
        var way = secondDuel.Exit(new(14, 28));
        secondDuel.OnEnter(r => r.Dialog(6087))
            .OnDialogClosed(6087, r => r
                .Spawn(Monster(318).At(12, 16))
                .Objective(LanguageKey.TIMESPACE_GOAL_FIGHT_TOM_AGAIN))
            .OnCleared(r => r
                .Unlock(way)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6088));
    }
}

return new TimeSpace17();
