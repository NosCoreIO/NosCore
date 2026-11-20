public class TimeSpace22 : TimeSpaceScript
{
    public TimeSpace22() : base(22, "A New Companion")
    {
        Levels(32, 99);
        Start(8, 10);
        Requires(1012, 3);
        Lives(3);
        Clock(TimeSpan.FromSeconds(300));
        Draw(180, 1);
        Draw(181, 1);
        Special(1028, 10);
        Reward(2131, 1);
        Reward(1087, 1);

        var entrance = Room(5203, 6, 9);
        var firstFight = Room(5203, 5, 9);
        var passage = Room(5203, 4, 9);
        var secondFight = Room(5203, 4, 8);
        var turn = Room(5203, 3, 8);
        var thirdFight = Room(5203, 2, 8);
        var bend = Room(5203, 2, 7);
        var fourthFight = Room(5203, 3, 7);
        var hallway = Room(5203, 4, 7);
        var guardPost = Room(5203, 5, 7);
        var companion = Room(5203, 5, 6);
        var firstAmbush = Room(5203, 6, 6);
        var secondAmbush = Room(5203, 7, 6);
        var thirdAmbush = Room(5203, 7, 7);
        var fourthAmbush = Room(5203, 7, 8);
        var way = Room(5203, 7, 9);

        entrance.Portal(new(1, 12), firstFight, new(23, 12), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var firstFightToEntrance = firstFight.Portal(new(23, 12), entrance, new(1, 12));
        var firstFightToPassage = firstFight.Portal(new(1, 12), passage, new(23, 12));
        firstFight.Spawn(Npc(311).At(15, 22), Npc(311).At(15, 19), Npc(311).At(15, 14), Npc(311).At(15, 11), Npc(311).At(15, 8), Npc(311).At(15, 7));
        firstFight.OnEnter(r => r.Dialog(6114))
            .OnDialogClosed(6114, r => r
                .Spawn(
                    Monster(114).At(15, 21),
                    Monster(114).At(15, 12).OnDeath(r => r.Spawn(Monster(114).At(6, 12), Monster(114).At(9, 13), Monster(114).At(9, 17))),
                    Monster(120).At(15, 9),
                    Monster(120).At(13, 17).OnDeath(r => r.Spawn(Monster(120).At(6, 14), Monster(120).At(9, 15))),
                    Monster(114).At(10, 12))
                .Say(Game18NConstString.NewGoal))
            .OnCleared(r => r
                .Unlock(firstFightToPassage, firstFightToEntrance)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ENEMY_DEFEATED));

        passage.Portal(new(12, 1), secondFight, new(12, 23), locked: false);
        passage.Portal(new(23, 12), firstFight, new(1, 12));

        var secondFightToPassage = secondFight.Portal(new(12, 23), passage, new(12, 1));
        var secondFightToTurn = secondFight.Portal(new(1, 12), turn, new(23, 12));
        secondFight.Spawn(Npc(311).At(14, 23), Npc(311).At(14, 20), Npc(311).At(14, 16), Npc(311).At(14, 13), Npc(311).At(14, 9), Npc(311).At(14, 6));
        secondFight.OnEnter(r => r.Dialog(6114))
            .OnDialogClosed(6114, r => r
                .Spawn(
                    Monster(120).At(15, 20),
                    Monster(120).At(15, 13),
                    Monster(120).At(15, 9),
                    Monster(117).At(11, 17),
                    Monster(114).At(11, 8),
                    Monster(114).At(10, 20),
                    Monster(117).At(9, 23),
                    Monster(114).At(9, 10),
                    Monster(117).At(9, 6).Bonus(),
                    Monster(114).At(8, 17),
                    Monster(117).At(8, 12))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Say(Game18NConstString.NewGoal))
            .OnCleared(r => r
                .Unlock(secondFightToPassage, secondFightToTurn)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        turn.Portal(new(23, 12), secondFight, new(1, 12), locked: false);
        turn.Portal(new(1, 12), thirdFight, new(23, 12), locked: false);

        var thirdFightToBend = thirdFight.Portal(new(12, 1), bend, new(12, 23));
        var thirdFightToTurn = thirdFight.Portal(new(23, 12), turn, new(1, 12));
        thirdFight.Spawn(Npc(311).At(15, 23), Npc(311).At(15, 22), Npc(311).At(15, 21), Npc(311).At(15, 18), Npc(311).At(15, 14), Npc(311).At(15, 9), Npc(311).At(15, 7), Npc(311).At(15, 6));
        thirdFight.OnEnter(r => r.Dialog(6114))
            .OnDialogClosed(6114, r => r
                .Spawn(
                    Monster(120).At(14, 19),
                    Monster(120).At(14, 11),
                    Monster(114).At(11, 22),
                    Monster(114).At(11, 21),
                    Monster(114).At(11, 17),
                    Monster(114).At(11, 14),
                    Monster(114).At(11, 10),
                    Monster(114).At(11, 7),
                    Monster(120).At(8, 17),
                    Monster(120).At(8, 15).Bonus(),
                    Monster(120).At(8, 11),
                    Monster(117).At(6, 8),
                    Monster(117).At(5, 13),
                    Monster(117).At(5, 11),
                    Monster(117).At(4, 13))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Say(Game18NConstString.NewGoal))
            .OnCleared(r => r
                .Unlock(thirdFightToTurn, thirdFightToBend)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        bend.Portal(new(23, 12), fourthFight, new(1, 12), locked: false);
        bend.Portal(new(12, 23), thirdFight, new(12, 1), locked: false);

        var fourthFightToHallway = fourthFight.Portal(new(23, 12), hallway, new(1, 12));
        var fourthFightToBend = fourthFight.Portal(new(1, 12), bend, new(23, 12));
        fourthFight.Spawn(Npc(311).At(12, 16), Npc(311).At(11, 13), Npc(311).At(10, 23), Npc(311).At(10, 6), Npc(311).At(9, 21), Npc(311).At(9, 18), Npc(311).At(9, 15), Npc(311).At(9, 11), Npc(311).At(9, 8));
        fourthFight.OnEnter(r => r.Dialog(6115))
            .OnDialogClosed(6115, r => r
                .Spawn(
                    Monster(114).At(22, 13),
                    Monster(114).At(22, 12),
                    Monster(114).At(22, 10),
                    Monster(114).At(20, 13),
                    Monster(117).At(19, 12),
                    Monster(117).At(14, 23),
                    Monster(120).At(14, 21),
                    Monster(120).At(14, 19),
                    Monster(120).At(14, 15),
                    Monster(120).At(14, 12).Bonus(),
                    Monster(120).At(14, 8),
                    Monster(117).At(14, 8),
                    Monster(117).At(14, 6))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Say(Game18NConstString.NewGoal))
            .OnCleared(r => r
                .Unlock(fourthFightToBend, fourthFightToHallway)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        hallway.Portal(new(23, 12), guardPost, new(1, 12), locked: false);
        hallway.Portal(new(1, 12), fourthFight, new(23, 12), locked: false);

        var guardPostToCompanion = guardPost.Portal(new(12, 1), companion, new(12, 23));
        guardPost.Portal(new(1, 12), hallway, new(23, 12));
        guardPost.Spawn(
            Monster(120).At(22, 13).Bonus(),
            Monster(120).At(20, 14),
            Monster(120).At(13, 10),
            Monster(120).At(12, 21),
            Monster(120).At(9, 14));
        guardPost.Spawn(Npc(311).At(23, 14), Npc(311).At(20, 13), Npc(311).At(15, 18), Npc(311).At(14, 23), Npc(311).At(15, 19), Npc(311).At(11, 8), Npc(311).At(10, 18), Npc(311).At(7, 15), Npc(311).At(9, 11), Npc(311).At(9, 7), Npc(311).At(9, 6));
        guardPost.OnCleared(r => r
                .Unlock(guardPostToCompanion)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6116))
            .OnDialogClosed(6116, r => r.Say(Game18NConstString.NewGoal));

        var companionToFirstAmbush = companion.Portal(new(23, 12), firstAmbush, new(1, 12));
        companion.Portal(new(12, 23), guardPost, new(12, 1));
        companion.Spawn(Npc(318).At(10, 15).Escort());
        companion.OnEnter(r => r.Dialog(6117))
            .OnDialogClosed(6117, r => r
                .Spawn(
                    Monster(338).At(14, 19),
                    Monster(338).At(14, 18),
                    Monster(338).At(14, 10),
                    Monster(338).At(13, 16),
                    Monster(338).At(13, 14),
                    Monster(338).At(13, 12))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Say(Game18NConstString.NewGoal))
            .OnCleared(r => r
                .Unlock(companionToFirstAmbush)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var firstAmbushToSecond = firstAmbush.Portal(new(23, 12), secondAmbush, new(1, 12));
        firstAmbush.Portal(new(1, 12), companion, new(23, 12));
        firstAmbush.Spawn(
            Monster(338).At(12, 22),
            Monster(338).At(10, 19).Bonus(),
            Monster(338).At(13, 16),
            Monster(338).At(10, 11),
            Monster(338).At(12, 9));
        firstAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstAmbushToSecond)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var secondAmbushToThird = secondAmbush.Portal(new(12, 23), thirdAmbush, new(12, 1));
        secondAmbush.Portal(new(1, 12), firstAmbush, new(23, 12));
        secondAmbush.Spawn(
            Monster(338).At(13, 15),
            Monster(338).At(13, 9).OnDeath(r => r.Spawn(Monster(338).At(12, 13), Monster(338).At(12, 21))),
            Monster(338).At(12, 19).OnDeath(r => r.Spawn(Monster(338).At(13, 17))),
            Monster(338).At(12, 12));
        secondAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondAmbushToThird)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        thirdAmbush.Portal(new(12, 1), secondAmbush, new(12, 23));
        var thirdAmbushToFourth = thirdAmbush.Portal(new(12, 23), fourthAmbush, new(12, 1));
        thirdAmbush.Spawn(
            Monster(338).At(14, 21).OnDeath(r => r.Spawn(Monster(338).At(19, 11))),
            Monster(338).At(11, 18).OnDeath(r => r.Spawn(
                Monster(338).At(15, 18).OnDeath(r => r.Spawn(Monster(338).At(20, 13).Bonus())))),
            Monster(338).At(10, 21).OnDeath(r => r.Spawn(Monster(338).At(23, 12), Monster(338).At(23, 13))),
            Monster(338).At(7, 16));
        thirdAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdAmbushToFourth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        fourthAmbush.Portal(new(12, 1), thirdAmbush, new(12, 23));
        var fourthAmbushToWay = fourthAmbush.Portal(new(12, 23), way, new(12, 1));
        fourthAmbush.Spawn(
            Monster(338).At(13, 17).OnDeath(r => r.Spawn(
                Monster(338).At(13, 20).OnDeath(r => r.Spawn(
                    Monster(338).At(17, 12),
                    Monster(338).At(18, 13).OnDeath(r => r.Spawn(Monster(338).At(23, 12))),
                    Monster(338).At(21, 12))))),
            Monster(338).At(9, 20).Bonus(),
            Monster(338).At(9, 17));
        fourthAmbush.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(fourthAmbushToWay)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        way.Portal(new(12, 1), fourthAmbush, new(12, 23));
        var exit = way.Exit(new(12, 23));
        way.OnEnter(r => r
            .Unlock(exit)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6118)
            .Say(Game18NConstString.NewGoal));
    }
}

return new TimeSpace22();
