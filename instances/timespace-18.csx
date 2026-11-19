public class TimeSpace18 : TimeSpaceScript
{
    public TimeSpace18() : base(18, "Hidden Schemes")
    {
        Levels(28, 99);
        Start(7, 9);
        Requires(1012, 2);
        Lives(2);
        Clock(TimeSpan.FromSeconds(120));
        Draw(2034, 3);
        Draw(2043, 3);
        Special(215, 1);
        Reward(2132, 1);
        Reward(1087, 1);

        var entrance = Room(4100, 0, 6);
        var meeting = Room(4100, 0, 7);
        var passage = Room(4100, 1, 7);
        var firstAmbush = Room(4100, 2, 7);
        var eastTurn = Room(4100, 3, 7);
        var secondAmbush = Room(4100, 3, 8);
        var hall = Room(4100, 2, 8);
        var thirdAmbush = Room(4100, 1, 8);
        var westTurn = Room(4100, 0, 8);
        var fourthAmbush = Room(4100, 0, 9);
        var crossing = Room(4100, 1, 9);
        var holdout = Room(4100, 2, 9);
        var descent = Room(4102, 1, 10);
        var barracks = Room(4102, 1, 11);
        var hideout = Room(4102, 2, 11);

        entrance.Portal(new(14, 28), meeting, new(14, 1), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        meeting.Portal(new(14, 1), entrance, new(14, 28), locked: false);
        meeting.Portal(new(28, 15), passage, new(1, 15), locked: false);
        meeting.Spawn(Npc(311).At(16, 12), Npc(311).At(15, 10), Npc(311).At(12, 12));
        meeting.OnEnter(r => r.Dialog(6089));

        passage.Portal(new(28, 15), firstAmbush, new(1, 15), locked: false);
        passage.Portal(new(1, 15), meeting, new(28, 15), locked: false);

        var firstAmbushToEastTurn = firstAmbush.Portal(new(28, 15), eastTurn, new(1, 15));
        firstAmbush.Portal(new(14, 28), hall, new(14, 1));
        var firstAmbushToPassage = firstAmbush.Portal(new(1, 15), passage, new(28, 15));
        firstAmbush.OnEnter(r => r.Dialog(6090))
            .OnDialogClosed(6090, r => r.Spawn(
                Monster(338).At(21, 14).Bonus(),
                Monster(338).At(17, 20),
                Monster(338).At(16, 10)))
            .OnCleared(r => r
                .Unlock(firstAmbushToEastTurn, firstAmbushToPassage)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6091));

        eastTurn.Portal(new(14, 28), secondAmbush, new(14, 1), locked: false);
        eastTurn.Portal(new(1, 15), firstAmbush, new(28, 15), locked: false);

        var secondAmbushToEastTurn = secondAmbush.Portal(new(14, 1), eastTurn, new(14, 28));
        var secondAmbushToHall = secondAmbush.Portal(new(1, 15), hall, new(28, 15));
        secondAmbush.OnEnter(r => r.Dialog(6092))
            .OnDialogClosed(6092, r => r.Spawn(
                Monster(338).At(19, 10).Bonus().OnDeath(r => r.Spawn(
                    Monster(338).At(11, 22),
                    Monster(338).At(15, 22).Bonus(),
                    Monster(338).At(22, 22))),
                Monster(338).At(15, 10),
                Monster(338).At(11, 10)))
            .OnCleared(r => r
                .Unlock(secondAmbushToHall, secondAmbushToEastTurn)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6093));

        hall.Portal(new(14, 1), firstAmbush, new(14, 28), locked: false);
        hall.Portal(new(28, 15), secondAmbush, new(1, 15), locked: false);
        hall.Portal(new(1, 15), thirdAmbush, new(28, 15), locked: false);

        var thirdAmbushToHall = thirdAmbush.Portal(new(28, 15), hall, new(1, 15));
        var thirdAmbushToWestTurn = thirdAmbush.Portal(new(1, 15), westTurn, new(28, 15));
        thirdAmbush.OnEnter(r => r.Dialog(6094))
            .OnDialogClosed(6094, r => r.Spawn(
                Monster(338).At(17, 11),
                Monster(338).At(17, 9).OnDeath(r => r.Spawn(
                    Monster(338).At(17, 16).OnDeath(r => r.Spawn(Monster(338).At(7, 8))),
                    Monster(338).At(17, 19).OnDeath(r => r.Spawn(
                        Monster(338).At(8, 12).OnDeath(r => r.Spawn(Monster(338).At(8, 19), Monster(338).At(8, 22)))))))))
            .OnCleared(r => r
                .Unlock(thirdAmbushToWestTurn, thirdAmbushToHall)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6095));

        westTurn.Portal(new(28, 15), thirdAmbush, new(1, 15), locked: false);
        westTurn.Portal(new(14, 28), fourthAmbush, new(14, 1), locked: false);

        var fourthAmbushToWestTurn = fourthAmbush.Portal(new(14, 1), westTurn, new(14, 28));
        var fourthAmbushToCrossing = fourthAmbush.Portal(new(28, 15), crossing, new(1, 15));
        fourthAmbush.OnEnter(r => r.Dialog(6096))
            .OnDialogClosed(6096, r => r
                .Spawn(
                    Monster(72).At(25, 15),
                    Monster(72).At(20, 17),
                    Monster(72).At(10, 18).OnDeath(r => r.Spawn(
                        Monster(72).At(13, 20).OnDeath(r => r.Spawn(Monster(72).At(7, 12))),
                        Monster(72).At(17, 20).OnDeath(r => r.Spawn(Monster(72).At(23, 20))))),
                    Monster(72).At(6, 16).Bonus().OnDeath(r => r.Spawn(Monster(72).At(7, 20), Monster(72).At(21, 12))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(fourthAmbushToCrossing, fourthAmbushToWestTurn)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        crossing.Portal(new(28, 15), holdout, new(1, 15), locked: false);
        var crossingToDescent = crossing.Portal(new(14, 28), descent, new(14, 1));
        crossing.Portal(new(1, 15), fourthAmbush, new(28, 15));

        holdout.Exit(new(28, 15));
        var holdoutToCrossing = holdout.Portal(new(1, 15), crossing, new(28, 15));
        holdout.OnEnter(r => r.Dialog(6097))
            .OnDialogClosed(6097, r => r
                .Spawn(Monster(338).At(20, 15))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_PORTAL_OPENS)
                .StartRoomClock(TimeSpan.FromSeconds(60)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Say(Game18NConstString.EnemiesReinforcements)
                .Spawn(
                    Monster(338).At(11, 20),
                    Monster(338).At(11, 22),
                    Monster(338).At(12, 20),
                    Monster(338).At(13, 10),
                    Monster(338).At(13, 22),
                    Monster(338).At(14, 20),
                    Monster(338).At(15, 10),
                    Monster(338).At(16, 20)))
            .OnClockOut(r => r
                .Unlock(holdoutToCrossing, crossingToDescent)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        descent.Portal(new(14, 1), crossing, new(14, 28));
        descent.Portal(new(14, 28), barracks, new(14, 1), locked: false);
        descent.OnEnter(r => r.Dialog(6098))
            .OnDialogClosed(6098, r => r.Spawn(
                Monster(338).At(14, 12),
                Monster(338).At(9, 23).Bonus(),
                Monster(338).At(9, 21),
                Monster(338).At(9, 19),
                Monster(338).At(9, 17),
                Monster(338).At(9, 15),
                Monster(338).At(9, 13).Bonus(),
                Monster(338).At(7, 10),
                Monster(338).At(9, 7),
                Monster(338).At(10, 6),
                Monster(338).At(6, 21).Count(2),
                Monster(338).At(6, 18),
                Monster(338).At(6, 15),
                Monster(338).At(6, 14),
                Monster(338).At(6, 11),
                Monster(338).At(6, 9)));

        barracks.Portal(new(14, 1), descent, new(14, 28), locked: false);
        barracks.Portal(new(28, 14), hideout, new(1, 14), locked: false);
        barracks.OnEnter(r => r.Dialog(6099))
            .OnDialogClosed(6099, r => r.Spawn(
                Monster(338).At(18, 21),
                Monster(338).At(18, 18),
                Monster(338).At(17, 15),
                Monster(338).At(12, 17).Bonus(),
                Monster(338).At(15, 15),
                Monster(338).At(13, 20),
                Monster(338).At(12, 18),
                Monster(338).At(11, 14),
                Monster(338).At(10, 21),
                Monster(338).At(9, 18),
                Monster(338).At(9, 16),
                Monster(338).At(8, 21),
                Monster(338).At(6, 19),
                Monster(338).At(8, 15).Count(2)));

        hideout.Exit(new(28, 14), locked: false);
        hideout.Portal(new(1, 14), barracks, new(28, 14), locked: false);
        hideout.Spawn(Npc(318).At(14, 14));
        hideout.OnEnter(r => r.Dialog(6100));
    }
}

return new TimeSpace18();
