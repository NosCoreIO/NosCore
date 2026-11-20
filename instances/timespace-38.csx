public class TimeSpace38 : TimeSpaceScript
{
    public TimeSpace38() : base(38, "Secret des Kenkos")
    {
        Levels(48, 99);
        Start(8, 13);
        Requires(1012, 4);
        Lives(2);
        Clock(TimeSpan.FromSeconds(150));
        Draw(1085, 2);
        Draw(1010, 3);
        Special(2040, 15);
        Reward(2127, 1);
        Reward(1087, 1);

        var entrance = Room(5307, 0, 8);
        var meadow = Room(5307, 1, 8);
        var firstTrail = Room(5307, 2, 8);
        var secondTrail = Room(5307, 3, 8);
        var thirdTrail = Room(5307, 4, 8);
        var forestEdge = Room(5308, 5, 8);
        var gate = Room(4405, 6, 8);
        var firstHall = Room(4406, 6, 9);
        var secondHall = Room(4408, 5, 9);
        var thirdHall = Room(4406, 5, 10);
        var storeroom = Room(4407, 4, 10);
        var lastHall = Room(4405, 4, 9);
        var sanctum = Room(4408, 3, 9);

        entrance.Portal(new(28, 16), meadow, new(2, 16), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        meadow.Portal(new(28, 16), firstTrail, new(2, 16), locked: false);
        meadow.Portal(new(2, 16), entrance, new(28, 16), locked: false);
        meadow.OnEnter(r => r.Dialog(6161));

        firstTrail.Portal(new(28, 16), secondTrail, new(2, 16), locked: false);
        firstTrail.Portal(new(2, 16), meadow, new(28, 16), locked: false);

        secondTrail.Portal(new(28, 16), thirdTrail, new(2, 16), locked: false);
        secondTrail.Portal(new(2, 16), firstTrail, new(28, 16), locked: false);

        thirdTrail.Portal(new(28, 16), forestEdge, new(2, 16), locked: false);
        thirdTrail.Portal(new(2, 16), secondTrail, new(28, 16), locked: false);

        forestEdge.Portal(new(28, 16), gate, new(1, 12), locked: false);
        forestEdge.Portal(new(2, 16), thirdTrail, new(28, 16), locked: false);

        gate.Portal(new(12, 23), firstHall, new(12, 1), locked: false);
        gate.Portal(new(1, 12), forestEdge, new(28, 16), locked: false);
        gate.OnEnter(r => r.Dialog(6162));

        firstHall.Portal(new(12, 1), gate, new(12, 23));
        var firstHallOut = firstHall.Portal(new(1, 12), secondHall, new(23, 12));
        firstHall.OnEnter(r => r.Dialog(6163))
            .OnDialogClosed(6163, r => r
                .Spawn(
                    Monster(152).At(16, 16).Bonus().OnDeath(r => r.Spawn(
                        Monster(147).At(12, 7).Bonus(),
                        Monster(147).At(12, 18).Bonus())),
                    Monster(152).At(16, 12).Bonus(),
                    Monster(152).At(15, 8).OnDeath(r => r.Spawn(
                        Monster(150).At(11, 13).OnDeath(r => r.Spawn(
                            Monster(152).At(14, 11).OnDeath(r => r.Spawn(
                                Monster(147).At(7, 11))))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstHallOut)
                .AddTime(TimeSpan.FromSeconds(80))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        secondHall.Portal(new(23, 12), firstHall, new(1, 12));
        var secondHallOut = secondHall.Portal(new(12, 23), thirdHall, new(12, 1));
        secondHall.OnEnter(r => r.Dialog(6163))
            .OnDialogClosed(6163, r => r
                .Spawn(
                    Monster(152).At(11, 17).Bonus(),
                    Monster(152).At(11, 12),
                    Monster(152).At(11, 9).Bonus().OnDeath(r => r.Spawn(
                        Monster(147).At(7, 17).Bonus().OnDeath(r => r.Spawn(
                            Monster(150).At(6, 15).Bonus())),
                        Monster(147).At(8, 12).Bonus().OnDeath(r => r.Spawn(
                            Monster(153).At(6, 13).Bonus())))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondHallOut)
                .AddTime(TimeSpan.FromSeconds(80))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        thirdHall.Portal(new(12, 1), secondHall, new(12, 23));
        var thirdHallOut = thirdHall.Portal(new(1, 12), storeroom, new(23, 12));
        thirdHall.OnEnter(r => r.Dialog(6163))
            .OnDialogClosed(6163, r => r
                .Spawn(
                    Monster(152).At(16, 16),
                    Monster(152).At(16, 12).Bonus().OnDeath(r => r.Spawn(
                        Monster(147).At(7, 11).Bonus())),
                    Monster(152).At(15, 8).OnDeath(r => r.Spawn(
                        Monster(150).At(11, 13).OnDeath(r => r.Spawn(
                            Monster(152).At(14, 11).OnDeath(r => r.Spawn(
                                Monster(147).At(12, 7).Bonus(),
                                Monster(147).At(12, 18).Bonus())))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdHallOut)
                .AddTime(TimeSpan.FromSeconds(80))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var storeroomOut = storeroom.Portal(new(12, 1), lastHall, new(12, 23));
        storeroom.Portal(new(23, 12), thirdHall, new(1, 12));
        storeroom.OnEnter(r => r.Dialog(6163))
            .OnDialogClosed(6163, r => r
                .Spawn(
                    Monster(152).At(16, 17),
                    Monster(152).At(16, 15).OnDeath(r => r.Spawn(
                        Monster(150).At(11, 13).Bonus().OnDeath(r => r.Spawn(
                            Monster(147).At(12, 7).Bonus().OnDeath(r => r.Spawn(
                                Monster(147).At(7, 11).Bonus().OnDeath(r => r.Spawn(
                                    Monster(150).At(9, 16).Bonus())))),
                            Monster(147).At(12, 18).Bonus())))),
                    Monster(152).At(15, 8).Bonus().OnDeath(r => r.Spawn(
                        Monster(152).At(14, 11))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(storeroomOut)
                .AddTime(TimeSpan.FromSeconds(80))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        lastHall.Portal(new(12, 23), storeroom, new(12, 1));
        var lastHallOut = lastHall.Portal(new(1, 12), sanctum, new(23, 12));
        lastHall.OnEnter(r => r.Dialog(6163))
            .OnDialogClosed(6163, r => r
                .Spawn(
                    Monster(152).At(11, 17).Bonus(),
                    Monster(152).At(11, 12).Bonus().OnDeath(r => r.Spawn(
                        Monster(150).At(6, 15).Bonus().OnDeath(r => r.Spawn(
                            Monster(153).At(6, 13).Bonus().OnDeath(r => r.Spawn(
                                Monster(153).At(11, 21).Bonus())))))),
                    Monster(152).At(11, 9).OnDeath(r => r.Spawn(
                        Monster(147).At(7, 17),
                        Monster(147).At(8, 12))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(lastHallOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        sanctum.Exit(new(12, 23), locked: false);
        sanctum.Portal(new(23, 12), lastHall, new(1, 12), locked: false);
        sanctum.OnEnter(r => r.Dialog(6164));
    }
}

return new TimeSpace38();
