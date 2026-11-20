public class TimeSpace46 : TimeSpaceScript
{
    public TimeSpace46() : base(46, "Le peuple des Catsys")
    {
        Levels(56, 99);
        Start(8, 12);
        Requires(1012, 5);
        Lives(2);
        Clock(TimeSpan.FromSeconds(200));
        Draw(715, 1);
        Draw(533, 1);
        Special(1033, 8);
        Reward(2099, 4);
        Reward(1087, 1);

        var entrance = Room(4813, 3, 11);
        var village = Room(4812, 3, 10);
        var eastPath = Room(4810, 4, 10);
        var square = Room(4812, 4, 9);
        var northPath = Room(4810, 5, 9);
        var clearing = Room(4808, 5, 8);
        var lair = Room(4807, 5, 7);

        entrance.Portal(new(15, 0), village, new(15, 28), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var villageOut = village.Portal(new(29, 14), eastPath, new(1, 14));
        var villageBack = village.Portal(new(15, 28), entrance, new(15, 0));
        village.OnEnter(r => r.Dialog(6233))
            .OnDialogClosed(6233, r => r
                .Spawn(
                    Monster(79).At(15, 13),
                    Monster(79).At(9, 19).Bonus().OnDeath(r => r.Spawn(
                        Monster(200).At(11, 7).Bonus().OnDeath(r => r.Spawn(
                            Monster(92).At(12, 14).OnDeath(r => r.Spawn(
                                Monster(92).At(18, 14))),
                            Monster(200).At(21, 14).Bonus())),
                        Monster(79).At(21, 19).Bonus().OnDeath(r => r.Spawn(
                            Monster(200).At(18, 9))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES))
            .OnCleared(r => r
                .Unlock(villageOut, villageBack)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var eastPathOut = eastPath.Portal(new(15, 0), square, new(15, 28));
        var eastPathBack = eastPath.Portal(new(1, 14), village, new(29, 14));
        eastPath.Spawn(
            Monster(80).At(20, 14).Bonus().OnDeath(r => r.Spawn(
                Monster(232).At(21, 10).Bonus())),
            Monster(80).At(19, 18).OnDeath(r => r.Spawn(
                Monster(232).At(13, 8).OnDeath(r => r.Spawn(
                    Monster(91).At(16, 13))))),
            Monster(80).At(13, 11).OnDeath(r => r.Spawn(
                Monster(88).At(11, 14).OnDeath(r => r.Spawn(
                    Monster(88).At(14, 16).OnDeath(r => r.Spawn(
                        Monster(232).At(21, 20))))))));
        eastPath.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES))
            .OnCleared(r => r
                .Unlock(eastPathOut, eastPathBack)
                .AddTime(TimeSpan.FromSeconds(200))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var squareOut = square.Portal(new(29, 14), northPath, new(1, 14));
        var squareBack = square.Portal(new(15, 28), eastPath, new(15, 0));
        square.Spawn(Npc(329).At(12, 20));
        square.OnEnter(r => r.Dialog(6234))
            .OnDialogClosed(6234, r => r
                .Spawn(
                    Monster(91).At(17, 14).Bonus().OnDeath(r => r.Spawn(
                        Monster(92).At(19, 9).Bonus().OnDeath(r => r.Spawn(
                            Monster(232).At(21, 19))))),
                    Monster(88).At(14, 15),
                    Monster(91).At(12, 13).OnDeath(r => r.Spawn(
                        Monster(88).At(12, 15),
                        Monster(91).At(20, 15).OnDeath(r => r.Spawn(
                            Monster(232).At(7, 15).OnDeath(r => r.Spawn(
                                Monster(232).At(20, 11).Bonus())),
                            Monster(92).At(11, 8))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_UNEXPECTED_ATTACK))
            .OnCleared(r => r
                .Unlock(squareOut, squareBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6235));

        var northPathOut = northPath.Portal(new(15, 0), clearing, new(15, 28));
        var northPathBack = northPath.Portal(new(1, 14), square, new(29, 14));
        northPath.Spawn(
            Monster(135).At(21, 15).OnDeath(r => r.Spawn(
                Monster(88).At(15, 16).Bonus().OnDeath(r => r.Spawn(
                    Monster(92).At(11, 12).Bonus(),
                    Monster(92).At(17, 15).Bonus().OnDeath(r => r.Spawn(
                        Monster(92).At(17, 19))))))),
            Monster(135).At(19, 18).OnDeath(r => r.Spawn(
                Monster(88).At(15, 18).OnDeath(r => r.Spawn(
                    Monster(200).At(17, 8).Bonus().OnDeath(r => r.Spawn(
                        Monster(200).At(20, 9).Bonus(),
                        Monster(200).At(23, 13))))))),
            Monster(135).At(19, 12).Bonus(),
            Monster(135).At(13, 11).OnDeath(r => r.Spawn(
                Monster(88).At(14, 15))));
        northPath.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_UNEXPECTED_ATTACK))
            .OnCleared(r => r
                .Unlock(northPathOut, northPathBack)
                .AddTime(TimeSpan.FromSeconds(200))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var clearingOut = clearing.Portal(new(15, 0), lair, new(15, 28));
        var clearingBack = clearing.Portal(new(15, 28), northPath, new(15, 0));
        clearing.Spawn(
            Monster(135).At(21, 15).OnDeath(r => r.Spawn(
                Monster(88).At(15, 16))),
            Monster(135).At(19, 18).OnDeath(r => r.Spawn(
                Monster(88).At(14, 18).OnDeath(r => r.Spawn(
                    Monster(88).At(15, 18))))),
            Monster(135).At(19, 12).Bonus().OnDeath(r => r.Spawn(
                Monster(200).At(17, 8).OnDeath(r => r.Spawn(
                    Monster(200).At(20, 9).Bonus(),
                    Monster(200).At(23, 13))))),
            Monster(135).At(13, 11).OnDeath(r => r.Spawn(
                Monster(92).At(11, 12).Bonus(),
                Monster(92).At(17, 15).OnDeath(r => r.Spawn(
                    Monster(92).At(17, 19).Bonus())))));
        clearing.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(clearingOut, clearingBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var lairExit = lair.Exit(new(15, 0));
        var lairBack = lair.Portal(new(15, 28), clearing, new(15, 0));
        lair.OnEnter(r => r.Dialog(6236))
            .OnDialogClosed(6236, r => r
                .Spawn(
                    Monster(134).At(23, 15).Bonus().OnDeath(r => r.Spawn(
                        Monster(80).At(9, 16).Bonus().OnDeath(r => r.Spawn(
                            Monster(94).At(12, 13).Bonus().OnDeath(r => r.Spawn(
                                Monster(244).At(9, 11).Bonus(),
                                Monster(244).At(15, 11).Bonus(),
                                Monster(244).At(21, 11).Bonus())),
                            Monster(94).At(17, 13).Bonus())),
                        Monster(80).At(15, 16),
                        Monster(80).At(21, 16).Bonus())),
                    Monster(134).At(19, 15),
                    Monster(134).At(15, 19).Bonus(),
                    Monster(134).At(11, 15).Bonus(),
                    Monster(134).At(7, 15))
                .Objective(LanguageKey.TIMESPACE_GOAL_UNEXPECTED_ATTACK))
            .OnCleared(r => r
                .Unlock(lairExit)
                .Unlock(lairBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6237));
    }
}

return new TimeSpace46();
