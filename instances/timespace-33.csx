public class TimeSpace33 : TimeSpaceScript
{
    public TimeSpace33() : base(33, "La rencontre")
    {
        Levels(43, 99);
        Start(10, 7);
        Requires(1012, 4);
        Lives(3);
        Clock(TimeSpan.FromSeconds(1200));
        Draw(138, 1);
        Draw(145, 1);
        Draw(152, 1);
        Special(220, 1);
        Reward(1027, 2);
        Reward(1087, 1);

        var entrance = Room(4418, 0, 6);
        var passage = Room(4411, 1, 6);
        var firstAmbush = Room(4411, 2, 6);
        var secondAmbush = Room(4411, 3, 6);
        var firstCrossing = Room(4400, 4, 6);
        var firstNorth = Room(4419, 4, 5);
        var firstSouth = Room(4413, 4, 7);
        var secondCrossing = Room(4400, 5, 6);
        var secondNorth = Room(4419, 5, 5);
        var secondSouth = Room(4413, 5, 7);
        var leverHall = Room(4411, 6, 6);
        var hideout = Room(4405, 7, 6);
        var thirdAmbush = Room(4410, 7, 7);
        var camp = Room(4410, 7, 8);
        var fourthAmbush = Room(4406, 7, 9);
        var meeting = Room(4411, 6, 9);
        var exitHall = Room(4411, 5, 9);

        entrance.Portal(new(23, 12), passage, new(1, 11), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        passage.Portal(new(23, 11), firstAmbush, new(1, 11), locked: false);
        passage.Portal(new(1, 11), entrance, new(23, 12), locked: false);
        passage.OnEnter(r => r.Dialog(6208));

        var firstAmbushOut = firstAmbush.Portal(new(23, 11), secondAmbush, new(1, 11));
        firstAmbush.Portal(new(1, 11), passage, new(23, 11));
        firstAmbush.Spawn(
            Monster(148).At(15, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(151).At(12, 14).OnDeath(r => r.Spawn(
                    Monster(151).At(11, 7).Bonus(),
                    Monster(151).At(15, 9).OnDeath(r => r.Spawn(
                        Monster(152).At(16, 15))))),
                Monster(151).At(18, 14).Bonus())),
            Monster(148).At(7, 12).Bonus(),
            Monster(148).At(8, 9));
        firstAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(firstAmbushOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var secondAmbushOut = secondAmbush.Portal(new(23, 11), firstCrossing, new(1, 12));
        secondAmbush.Portal(new(1, 11), firstAmbush, new(23, 11));
        secondAmbush.Spawn(
            Monster(148).At(13, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(151).At(12, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(11, 7),
                    Monster(151).At(15, 9).OnDeath(r => r.Spawn(
                        Monster(152).At(16, 15))))),
                Monster(151).At(18, 14).Bonus())),
            Monster(148).At(9, 12),
            Monster(148).At(6, 8));
        secondAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(secondAmbushOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var firstCrossingNorth = firstCrossing.Portal(new(12, 1), firstNorth, new(12, 23));
        var firstCrossingEast = firstCrossing.Portal(new(23, 12), secondCrossing, new(1, 12));
        var firstCrossingSouth = firstCrossing.Portal(new(12, 23), firstSouth, new(12, 1));
        firstCrossing.Portal(new(1, 12), secondAmbush, new(23, 11));
        firstCrossing.Lever(new(12, 12))
            .OnUse(r => r.Unlock(firstCrossingNorth, firstCrossingSouth).Say(Game18NConstString.ADoorHasOpened));

        var firstNorthOut = firstNorth.Portal(new(12, 23), firstCrossing, new(12, 1));
        firstNorth.Spawn(
            Monster(148).At(7, 10).Bonus().OnDeath(r => r.Spawn(
                Monster(148).At(14, 11).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(15, 9).Bonus())))),
            Monster(148).At(7, 7).Bonus().OnDeath(r => r.Spawn(
                Monster(149).At(18, 8).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(16, 15).Bonus().OnDeath(r => r.Spawn(
                        Monster(152).At(16, 8).Bonus().OnDeath(r => r.Spawn(
                            Monster(147).At(13, 7).Bonus())))))),
                Monster(151).At(18, 14))));
        firstNorth.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(firstNorthOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var firstSouthOut = firstSouth.Portal(new(12, 1), firstCrossing, new(12, 23));
        firstSouth.Spawn(
            Monster(148).At(9, 12).Bonus().OnDeath(r => r.Spawn(
                Monster(151).At(12, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(11, 7).Bonus(),
                    Monster(151).At(15, 9).Bonus().OnDeath(r => r.Spawn(
                        Monster(152).At(16, 15).Bonus().OnDeath(r => r.Spawn(
                            Monster(152).At(16, 8).Bonus().OnDeath(r => r.Spawn(
                                Monster(149).At(18, 8))))),
                        Monster(151).At(18, 14).Bonus())))))),
            Monster(148).At(8, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(148).At(14, 11).Bonus())));
        firstSouth.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(firstSouthOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        OnAllCleared([firstNorth, firstSouth], r => r
            .Unlock(firstCrossingEast)
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(Game18NConstString.QuickMissionComplete));

        var secondCrossingNorth = secondCrossing.Portal(new(12, 1), secondNorth, new(12, 23));
        var secondCrossingEast = secondCrossing.Portal(new(23, 12), leverHall, new(1, 11));
        var secondCrossingSouth = secondCrossing.Portal(new(12, 23), secondSouth, new(12, 1));
        secondCrossing.Portal(new(1, 12), firstCrossing, new(23, 12));
        secondCrossing.Lever(new(12, 12))
            .OnUse(r => r.Unlock(secondCrossingNorth, secondCrossingSouth).Say(Game18NConstString.ADoorHasOpened));

        var secondNorthOut = secondNorth.Portal(new(12, 23), secondCrossing, new(12, 1));
        secondNorth.Spawn(
            Monster(148).At(8, 12).Bonus().OnDeath(r => r.Spawn(
                Monster(151).At(11, 7),
                Monster(151).At(15, 9).Bonus().OnDeath(r => r.Spawn(
                    Monster(149).At(18, 8).Bonus())))),
            Monster(148).At(10, 11).OnDeath(r => r.Spawn(
                Monster(148).At(14, 11).OnDeath(r => r.Spawn(
                    Monster(151).At(12, 14).OnDeath(r => r.Spawn(
                        Monster(151).At(18, 14))))))));
        secondNorth.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(secondNorthOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var secondSouthOut = secondSouth.Portal(new(12, 1), secondCrossing, new(12, 23));
        secondSouth.Spawn(
            Monster(148).At(14, 11),
            Monster(148).At(10, 8).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(16, 8).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(18, 14).Bonus().OnDeath(r => r.Spawn(
                        Monster(149).At(18, 8).Bonus().OnDeath(r => r.Spawn(
                            Monster(151).At(15, 9),
                            Monster(152).At(16, 15).Bonus())))))))));
        secondSouth.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(secondSouthOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        OnAllCleared([secondNorth, secondSouth], r => r
            .Unlock(secondCrossingEast)
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(Game18NConstString.QuickMissionComplete));

        var leverHallOut = leverHall.Portal(new(23, 11), hideout, new(1, 12));
        leverHall.Portal(new(1, 11), secondCrossing, new(23, 12));
        leverHall.Lever(new(12, 5))
            .OnUse(r => r
                .Spawn(
                    Monster(148).At(8, 9).Bonus().OnDeath(r => r.Spawn(
                        Monster(148).At(9, 12).Bonus().OnDeath(r => r.Spawn(
                            Monster(151).At(11, 7).Bonus().OnDeath(r => r.Spawn(
                                Monster(151).At(12, 14).Bonus().OnDeath(r => r.Spawn(
                                    Monster(147).At(13, 7).Bonus().OnDeath(r => r.Spawn(
                                        Monster(148).At(14, 11).Bonus(),
                                        Monster(151).At(15, 9).Bonus().OnDeath(r => r.Spawn(
                                            Monster(149).At(18, 8).Bonus())))))))))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK));
        leverHall.OnCleared(r => r
            .Unlock(leverHallOut)
            .AddTime(TimeSpan.FromSeconds(30))
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(Game18NConstString.QuickMissionComplete));

        hideout.Portal(new(12, 23), thirdAmbush, new(12, 1), locked: false);
        hideout.Portal(new(1, 12), leverHall, new(23, 11), locked: false);
        hideout.Spawn(Npc(316).At(11, 13), Npc(253).At(9, 15));
        hideout.OnEnter(r => r.Say(LanguageKey.TIMESPACE_VIOLENT_KENKO_HIDE).Dialog(6209));

        var thirdAmbushBack = thirdAmbush.Portal(new(12, 1), hideout, new(12, 23));
        var thirdAmbushOut = thirdAmbush.Portal(new(12, 23), camp, new(12, 1));
        thirdAmbush.Spawn(
            Monster(148).At(9, 12),
            Monster(148).At(8, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(148).At(14, 11).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(12, 14).OnDeath(r => r.Spawn(
                        Monster(151).At(11, 7).Bonus().OnDeath(r => r.Spawn(
                            Monster(152).At(16, 8).Bonus().OnDeath(r => r.Spawn(
                                Monster(149).At(18, 8).Bonus())))),
                        Monster(151).At(15, 9).Bonus().OnDeath(r => r.Spawn(
                            Monster(152).At(16, 15).Bonus().OnDeath(r => r.Spawn(
                                Monster(153).At(16, 11))))))),
                    Monster(151).At(18, 14).Bonus())))));
        thirdAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(thirdAmbushBack, thirdAmbushOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        camp.Portal(new(12, 1), thirdAmbush, new(12, 23), locked: false);
        camp.Portal(new(12, 23), fourthAmbush, new(12, 1), locked: false);
        camp.Spawn(Npc(329).At(12, 6), Npc(307).At(10, 6));
        camp.OnEnter(r => r.Dialog(6210));

        var fourthAmbushBack = fourthAmbush.Portal(new(12, 1), camp, new(12, 23));
        var fourthAmbushOut = fourthAmbush.Portal(new(1, 12), meeting, new(23, 11));
        fourthAmbush.Spawn(
            Monster(148).At(14, 11).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(16, 15).Bonus().OnDeath(r => r.Spawn(
                    Monster(151).At(12, 14).Bonus().OnDeath(r => r.Spawn(
                        Monster(151).At(15, 9).Bonus(),
                        Monster(149).At(18, 8).Bonus().OnDeath(r => r.Spawn(
                            Monster(147).At(13, 7).Bonus().OnDeath(r => r.Spawn(
                                Monster(153).At(16, 11).Bonus())))))),
                    Monster(151).At(18, 14))))),
            Monster(148).At(9, 10).Bonus());
        fourthAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK))
            .OnCleared(r => r
                .Unlock(fourthAmbushBack, fourthAmbushOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var meetingBack = meeting.Portal(new(23, 11), fourthAmbush, new(1, 12));
        var meetingOut = meeting.Portal(new(1, 11), exitHall, new(23, 11));
        meeting.Spawn(Npc(315).At(20, 14), Npc(307).At(20, 12));
        meeting.OnEnter(r => r.Dialog(6211).Say(LanguageKey.TIMESPACE_KORRKOAREN_MEETS_KENKO))
            .OnDialogClosed(6211, r => r
                .Spawn(Monster(316).At(20, 10).Bonus(), Monster(253).At(14, 5))
                .Objective(LanguageKey.TIMESPACE_GOAL_FIGHT_DEMON_THREE_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(180)))
            .Every(TimeSpan.FromSeconds(30), r => r
                    .Say(Game18NConstString.EnemiesReinforcements)
                    .Spawn(Monster(253).Anywhere().Count(3)),
                from: TimeSpan.FromSeconds(60))
            .OnClockOut(r => r
                .Unlock(meetingOut, meetingBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6212));

        exitHall.Exit(new(1, 11), locked: false);
        exitHall.Portal(new(23, 11), meeting, new(1, 11), locked: false);
        exitHall.OnEnter(r => r.Dialog(6213));
    }
}

return new TimeSpace33();
