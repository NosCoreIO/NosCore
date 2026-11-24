public class TimeSpace16 : TimeSpaceScript
{
    public TimeSpace16() : base(16, "The Monster Army")
    {
        Levels(26, 99);
        Start(9, 11);
        Requires(1012, 2);
        Lives(2);
        Clock(TimeSpan.FromSeconds(150));
        Objective(ObjectiveKind.UseObject, 1048, 1);
        Draw(70, 1);
        Draw(80, 1);
        Draw(88, 1);
        Special(1011, 50);
        Reward(2132, 1);
        Reward(1087, 1);

        var entrance = Room(4204, 5, 8);
        var briefing = Room(4205, 4, 8);
        var firstLever = Room(4205, 3, 8);
        var secondLever = Room(4208, 2, 8);
        var thirdLever = Room(4211, 2, 9);
        var sideRoom = Room(4202, 1, 9);
        var fourthLever = Room(4209, 3, 9);
        var fifthLever = Room(4212, 3, 10);
        var deadEnd = Room(4201, 3, 11);
        var sealRoom = Room(4205, 4, 10);

        entrance.Portal(new(1, 15), briefing, new(28, 15), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        briefing.Portal(new(28, 15), entrance, new(1, 15), locked: false);
        briefing.Portal(new(1, 15), firstLever, new(28, 15), locked: false);
        briefing.OnEnter(r => r.Say(Game18NConstString.NewGoal).Dialog(6078));

        firstLever.Portal(new(28, 15), briefing, new(1, 15));
        var firstToSecond = firstLever.Portal(new(1, 15), secondLever, new(28, 15));
        firstLever.Switch(new(5, 15), 1051, 1052)
            .OnSwitchedOn(r => r
                .Unlock(firstToSecond)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal))
            .OnFirstSwitchedOn(r => r.AddTime(TimeSpan.FromSeconds(80)))
            .OnSwitchedOff(r => r
                .Lock(firstToSecond)
                .Say(Game18NConstString.DoorIsLocked)
                .Say(Game18NConstString.NewGoal));
        firstLever.OnEnter(r => r.Dialog(6079).Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE))
            .OnDialogClosed(6079, r => r
                .Spawn(
                    Monster(58).At(11, 17),
                    Monster(58).At(11, 12).OnDeath(r => r.Spawn(Monster(58).At(13, 22))),
                    Monster(271).At(17, 13))
                .Say(Game18NConstString.NewGoal));

        secondLever.Portal(new(28, 15), firstLever, new(1, 15));
        var secondToThird = secondLever.Portal(new(14, 28), thirdLever, new(14, 1));
        secondLever.Switch(new(5, 15), 1051, 1052)
            .OnSwitchedOn(r => r
                .Unlock(secondToThird)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal))
            .OnFirstSwitchedOn(r => r.AddTime(TimeSpan.FromSeconds(80)))
            .OnSwitchedOff(r => r
                .Lock(secondToThird)
                .Say(Game18NConstString.DoorIsLocked)
                .Say(Game18NConstString.NewGoal));
        secondLever.OnEnter(r => r.Dialog(6079).Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE))
            .OnDialogClosed(6079, r => r
                .Spawn(
                    Monster(64).At(13, 22),
                    Monster(64).At(7, 20).OnDeath(r => r.Spawn(Monster(58).At(12, 15))),
                    Monster(271).At(14, 18))
                .Say(Game18NConstString.NewGoal));

        thirdLever.Portal(new(14, 1), secondLever, new(14, 28));
        var thirdToFourth = thirdLever.Portal(new(29, 15), fourthLever, new(1, 15));
        var thirdToSide = thirdLever.Portal(new(1, 15), sideRoom, new(28, 15));
        thirdLever.Switch(new(15, 17), 1051, 1052)
            .OnSwitchedOn(r => r
                .Unlock(thirdToFourth, thirdToSide)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal))
            .OnFirstSwitchedOn(r => r.AddTime(TimeSpan.FromSeconds(80)))
            .OnSwitchedOff(r => r
                .Lock(thirdToFourth, thirdToSide)
                .Say(Game18NConstString.DoorIsLocked)
                .Say(Game18NConstString.NewGoal));
        thirdLever.OnEnter(r => r.Dialog(6079).Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE))
            .OnDialogClosed(6079, r => r
                .Spawn(
                    Monster(20).At(7, 10).OnDeath(r => r.Spawn(Monster(58).At(6, 18), Monster(58).At(10, 21))),
                    Monster(271).At(14, 18).Bonus())
                .Say(Game18NConstString.NewGoal));

        var sideToThird = sideRoom.Portal(new(28, 15), thirdLever, new(1, 15));
        sideRoom.Spawn(Item(1001).At(4, 14), Item(1001).At(4, 12));
        sideRoom.Spawn(
            Monster(58).At(16, 20),
            Monster(58).At(12, 11).OnDeath(r => r.Spawn(
                Monster(66).At(18, 14).OnDeath(r => r.Spawn(
                    Monster(66).At(6, 16).OnDeath(r => r.Spawn(
                        Monster(66).At(12, 17).OnDeath(r => r.Spawn(Monster(66).At(13, 21))))))))));
        sideRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(sideToThird)
                .AddTime(TimeSpan.FromSeconds(80))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var fourthToFifth = fourthLever.Portal(new(14, 28), fifthLever, new(14, 1));
        fourthLever.Portal(new(1, 15), thirdLever, new(29, 15));
        fourthLever.Switch(new(22, 15), 1051, 1052)
            .OnSwitchedOn(r => r
                .Unlock(fourthToFifth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal))
            .OnFirstSwitchedOn(r => r.AddTime(TimeSpan.FromSeconds(100)))
            .OnSwitchedOff(r => r
                .Lock(fourthToFifth)
                .Say(Game18NConstString.DoorIsLocked)
                .Say(Game18NConstString.NewGoal));
        fourthLever.OnEnter(r => r.Dialog(6079).Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE))
            .OnDialogClosed(6079, r => r
                .Spawn(
                    Monster(58).At(20, 20),
                    Monster(58).At(8, 20).Bonus().OnDeath(r => r.Spawn(Monster(58).At(23, 18))),
                    Monster(271).At(14, 18))
                .Say(Game18NConstString.NewGoal));

        fifthLever.Portal(new(14, 1), fourthLever, new(14, 28));
        var fifthToSeal = fifthLever.Portal(new(28, 15), sealRoom, new(1, 15));
        var fifthToDeadEnd = fifthLever.Portal(new(14, 28), deadEnd, new(14, 1));
        fifthLever.Switch(new(15, 15), 1051, 1052)
            .OnSwitchedOn(r => r
                .Unlock(fifthToDeadEnd, fifthToSeal)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE)
                .Say(Game18NConstString.NewGoal))
            .OnFirstSwitchedOn(r => r.AddTime(TimeSpan.FromSeconds(80)))
            .OnSwitchedOff(r => r
                .Lock(fifthToDeadEnd, fifthToSeal)
                .Say(Game18NConstString.DoorIsLocked)
                .Say(Game18NConstString.NewGoal));
        fifthLever.Spawn(
            Monster(58).At(9, 17).OnDeath(r => r.Spawn(Monster(58).At(11, 21), Monster(58).At(19, 22))),
            Monster(58).At(9, 14).Talk(6079),
            Monster(271).At(14, 22).Bonus());
        fifthLever.OnEnter(r => r.Dialog(6079).Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE))
            .OnDialogClosed(6079, r => r
                .Say(LanguageKey.TIMESPACE_USE_LEVER_TO_ESCAPE)
                .Say(Game18NConstString.NewGoal));

        var deadEndToFifth = deadEnd.Portal(new(14, 1), fifthLever, new(14, 28));
        deadEnd.Spawn(
            Monster(20).At(15, 17).OnDeath(r => r.Spawn(
                Monster(58).At(22, 11).OnDeath(r => r.Spawn(
                    Monster(66).At(12, 12).OnDeath(r => r.Spawn(Monster(66).At(17, 13))))))),
            Monster(58).At(9, 9).OnDeath(r => r.Spawn(
                Monster(66).At(16, 18).OnDeath(r => r.Spawn(Monster(58).At(9, 17).Bonus(), Monster(58).At(23, 18))))));
        deadEnd.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_DARK_GARGS_NOT_HERE))
            .OnCleared(r => r
                .Unlock(deadEndToFifth)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_NO_EXIT_IN_THIS_ROOM));

        var exit = sealRoom.Exit(new(28, 15));
        sealRoom.Portal(new(1, 15), fifthLever, new(28, 15));
        sealRoom.Sculpture(new(13, 16), 1048, 1049)
            .OnUse(r => r
                .StartRoomClock(TimeSpan.FromSeconds(60))
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Say(Game18NConstString.NewGoal)
                .Dialog(6082)
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_MONSTER_LEAVES)
                .Spawn(
                    Monster(18).At(25, 12).OnDeath(r => r.Spawn(
                        Monster(57).At(10, 10).OnDeath(r => r.Spawn(
                            Monster(136).At(17, 17).OnDeath(r => r.Spawn(
                                Monster(58).At(9, 7),
                                Monster(58).At(18, 8),
                                Monster(58).At(20, 21),
                                Monster(58).At(25, 15))))),
                        Monster(57).At(11, 16).OnDeath(r => r.Spawn(
                            Monster(57).At(15, 10).OnDeath(r => r.Spawn(
                                Monster(57).At(16, 21).OnDeath(r => r.Spawn(
                                    Monster(136).At(14, 12).OnDeath(r => r.Spawn(Monster(58).At(15, 17))),
                                    Monster(136).At(14, 21).OnDeath(r => r.Spawn(Monster(58).At(12, 13), Monster(58).At(12, 24))),
                                    Monster(136).At(17, 13))),
                                Monster(57).At(20, 17).OnDeath(r => r.Spawn(Monster(57).At(11, 20), Monster(57).At(6, 16))))))))),
                    Monster(18).At(23, 12),
                    Monster(131).At(21, 15),
                    Monster(131).At(19, 19).OnDeath(r => r.Spawn(
                        Monster(136).At(7, 21).OnDeath(r => r.Spawn(Monster(136).At(19, 12))),
                        Monster(136).At(8, 16).OnDeath(r => r.Spawn(Monster(136).At(15, 7))))),
                    Monster(18).At(17, 6),
                    Monster(18).At(17, 4),
                    Monster(131).At(13, 10),
                    Monster(131).At(9, 19).OnDeath(r => r.Spawn(Monster(136).At(6, 12))),
                    Monster(131).At(8, 13),
                    Monster(271).At(14, 15)));
        sealRoom.OnEnter(r => r.Dialog(6081).Say(LanguageKey.TIMESPACE_SEAL_ROOM))
            .OnClockOut(r => r
                .Dismiss(18, 57, 58, 131, 136, 271)
                .Unlock(exit)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal)
                .Say(LanguageKey.TIMESPACE_DARK_GARGS_GONE_EXIT_OPENS));
    }
}

return new TimeSpace16();
