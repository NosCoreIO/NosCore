public class TimeSpace35 : TimeSpaceScript
{
    public TimeSpace35() : base(35, "L'enquête")
    {
        Levels(45, 99);
        Start(15, 14);
        Requires(1012, 4);
        Lives(1);
        Clock(TimeSpan.FromSeconds(150));
        Draw(102, 1);
        Draw(115, 1);
        Draw(128, 1);
        Special(803, 1);
        Reward(2099, 2);
        Reward(1087, 1);

        var entrance = Room(5301, 4, 11);
        var path = Room(5302, 4, 10);
        var lookout = Room(5302, 4, 9);
        var crossroads = Room(5301, 4, 8);
        var westCamp = Room(5307, 3, 8);
        var eastCamp = Room(5307, 5, 8);
        var northPath = Room(5302, 4, 7);
        var bend = Room(5303, 4, 6);
        var outpost = Room(5304, 5, 6);
        var climb = Room(5302, 5, 5);
        var trap = Room(5303, 5, 4);
        var den = Room(5306, 6, 4);

        entrance.Portal(new(14, 1), path, new(15, 28), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        path.Portal(new(15, 2), lookout, new(15, 28), locked: false);
        path.Portal(new(15, 28), entrance, new(14, 1), locked: false);
        path.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SOMEONE_WATCHING));

        var lookoutOut = lookout.Portal(new(15, 2), crossroads, new(14, 28));
        var lookoutBack = lookout.Portal(new(15, 28), path, new(15, 2));
        lookout.OnEnter(r => r.Dialog(6145))
            .OnDialogClosed(6145, r => r
                .Spawn(
                    Monster(83).At(18, 13).Bonus().OnDeath(r => r.Spawn(
                        Monster(83).At(11, 13).Bonus().OnDeath(r => r.Spawn(
                            Monster(83).At(12, 20).Bonus(),
                            Monster(83).At(22, 14))))),
                    Monster(83).At(16, 17))
                .Objective(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES))
            .OnCleared(r => r
                .Unlock(lookoutOut, lookoutBack)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ELIMINATE_ALL_ENEMIES_QUESTION));

        var crossroadsNorth = crossroads.Portal(new(14, 1), northPath, new(15, 28));
        crossroads.Portal(new(28, 15), eastCamp, new(2, 16), locked: false);
        crossroads.Portal(new(14, 28), lookout, new(15, 2), locked: false);
        crossroads.Portal(new(1, 15), westCamp, new(28, 16), locked: false);

        var westCampOut = westCamp.Portal(new(28, 16), crossroads, new(1, 15));
        westCamp.OnEnter(r => r.Dialog(6146))
            .OnDialogClosed(6146, r => r
                .Spawn(
                    Monster(144).At(21, 13).Bonus().OnDeath(r => r.Spawn(
                        Monster(144).At(17, 19).Bonus().OnDeath(r => r.Spawn(
                            Monster(139).At(10, 10).Bonus())))),
                    Monster(144).At(14, 8).OnDeath(r => r.Spawn(
                        Monster(139).At(11, 14),
                        Monster(139).At(11, 19).Bonus())))
                .Objective(LanguageKey.TIMESPACE_GOAL_ENEMIES))
            .OnCleared(r => r
                .Unlock(westCampOut)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ALL_MONSTERS_DEFEATED));

        var eastCampOut = eastCamp.Portal(new(2, 16), crossroads, new(28, 15));
        eastCamp.OnEnter(r => r.Dialog(6145))
            .OnDialogClosed(6145, r => r
                .Spawn(
                    Monster(144).At(15, 18).Bonus().OnDeath(r => r.Spawn(
                        Monster(61).At(17, 11).Bonus(),
                        Monster(61).At(18, 16).OnDeath(r => r.Spawn(
                            Monster(61).At(12, 10).Bonus())))),
                    Monster(144).At(12, 15))
                .Objective(LanguageKey.TIMESPACE_GOAL_ENEMIES))
            .OnCleared(r => r
                .Unlock(eastCampOut)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ALL_CLEAR));

        OnAllCleared([westCamp, eastCamp], r => r
            .Unlock(crossroadsNorth)
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(Game18NConstString.QuickMissionComplete));

        var northPathOut = northPath.Portal(new(15, 2), bend, new(14, 28));
        var northPathBack = northPath.Portal(new(15, 28), crossroads, new(14, 1));
        northPath.OnEnter(r => r.Dialog(6147))
            .OnDialogClosed(6147, r => r
                .Spawn(
                    Monster(143).At(23, 19).Bonus(),
                    Monster(143).At(16, 19).OnDeath(r => r.Spawn(
                        Monster(7).At(11, 15))),
                    Monster(143).At(9, 15).Bonus().OnDeath(r => r.Spawn(
                        Monster(85).At(19, 19).OnDeath(r => r.Spawn(
                            Monster(85).At(16, 10))),
                        Monster(85).At(20, 13).OnDeath(r => r.Spawn(
                            Monster(7).At(9, 10))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES))
            .OnCleared(r => r
                .Unlock(northPathOut, northPathBack)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ALL_MONSTERS_DEFEATED));

        bend.Portal(new(28, 15), outpost, new(1, 15), locked: false);
        bend.Portal(new(14, 28), northPath, new(15, 2), locked: false);

        var outpostOut = outpost.Portal(new(14, 1), climb, new(15, 28));
        var outpostBack = outpost.Portal(new(1, 15), bend, new(28, 15));
        outpost.OnEnter(r => r.Dialog(6148))
            .OnDialogClosed(6148, r => r
                .Spawn(
                    Monster(143).At(17, 9).Bonus().OnDeath(r => r.Spawn(
                        Monster(60).At(15, 16).OnDeath(r => r.Spawn(
                            Monster(60).At(8, 15))),
                        Monster(60).At(21, 17).Bonus().OnDeath(r => r.Spawn(
                            Monster(61).At(10, 13).Bonus(),
                            Monster(61).At(20, 13).Bonus())))),
                    Monster(143).At(14, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_ENEMIES))
            .OnCleared(r => r
                .Unlock(outpostOut, outpostBack)
                .AddTime(TimeSpan.FromSeconds(140))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ALL_CLEAR));

        climb.Portal(new(15, 2), trap, new(14, 28), locked: false);
        climb.Portal(new(15, 28), outpost, new(14, 1), locked: false);

        var trapOut = trap.Portal(new(28, 15), den, new(2, 16));
        var trapBack = trap.Portal(new(14, 28), climb, new(15, 2));
        trap.Spawn(
            Monster(119).At(21, 19),
            Monster(119).At(21, 16).Bonus(),
            Monster(119).At(23, 11),
            Monster(122).At(20, 8),
            Monster(122).At(18, 23),
            Monster(116).At(18, 18),
            Monster(116).At(18, 13),
            Monster(119).At(17, 14),
            Monster(116).At(15, 17),
            Monster(116).At(14, 11),
            Monster(119).At(12, 14),
            Monster(122).At(12, 8),
            Monster(116).At(11, 17),
            Monster(122).At(9, 17),
            Monster(116).At(10, 11),
            Monster(122).At(9, 9),
            Monster(122).At(5, 17));
        trap.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_TRAP_HOLD_ON)
                .StartRoomClock(TimeSpan.FromSeconds(30)))
            .OnClockOut(r => r
                .Unlock(trapOut, trapBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6149));

        den.Exit(new(15, 28), locked: false);
        den.Portal(new(2, 16), trap, new(28, 15), locked: false);
        den.OnEnter(r => r.Dialog(6150));
    }
}

return new TimeSpace35();
