public class TimeSpace44 : TimeSpaceScript
{
    public TimeSpace44() : base(44, "Le cœur de Fernon")
    {
        Levels(54, 99);
        Start(23, 15);
        Requires(1012, 5);
        Lives(2);
        Clock(TimeSpan.FromSeconds(240));
        Special(1033, 8);
        Reward(2003, 2);
        Reward(1087, 1);

        var entrance = Room(4302, 1, 6);
        var firstHall = Room(4305, 2, 6);
        var secondHall = Room(4305, 3, 6);
        var firstBattle = Room(4305, 4, 6);
        var eastHall = Room(4309, 5, 6);
        var firstStair = Room(4306, 5, 7);
        var secondBattle = Room(4306, 5, 8);
        var southEastHall = Room(4310, 5, 9);
        var southHall = Room(4305, 4, 9);
        var thirdBattle = Room(4305, 3, 9);
        var southWestHall = Room(4307, 2, 9);
        var firstHoldout = Room(4306, 2, 8);
        var westHall = Room(4308, 2, 7);
        var secondHoldout = Room(4305, 3, 7);
        var lastBattle = Room(4309, 4, 7);
        var heart = Room(4310, 4, 8);

        entrance.Portal(new(46, 18), firstHall, new(2, 20), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        firstHall.Portal(new(46, 18), secondHall, new(2, 20), locked: false);
        firstHall.Portal(new(2, 20), entrance, new(46, 18), locked: false);
        firstHall.Spawn(
            Monster(154).At(35, 18).Bonus(),
            Monster(154).At(30, 14),
            Monster(154).At(28, 24),
            Monster(154).At(26, 19),
            Monster(154).At(26, 15),
            Monster(154).At(24, 23).Bonus(),
            Monster(154).At(22, 13),
            Monster(154).At(19, 14),
            Monster(154).At(14, 22),
            Monster(154).At(13, 19));
        firstHall.OnEnter(r => r.Dialog(6171));

        secondHall.Portal(new(46, 18), firstBattle, new(2, 20), locked: false);
        secondHall.Portal(new(2, 20), firstHall, new(46, 18), locked: false);
        secondHall.Spawn(
            Monster(154).At(35, 19),
            Monster(154).At(32, 24).Bonus(),
            Monster(154).At(32, 14),
            Monster(154).At(26, 26).Bonus(),
            Monster(154).At(26, 15),
            Monster(154).At(20, 14),
            Monster(154).At(19, 23),
            Monster(154).At(15, 24),
            Monster(154).At(16, 15),
            Monster(154).At(7, 20));

        var firstBattleEast = firstBattle.Portal(new(46, 18), eastHall, new(2, 20));
        var firstBattleWest = firstBattle.Portal(new(2, 20), secondHall, new(46, 18));
        firstBattle.Spawn(
            Monster(154).At(37, 21).Bonus().OnDeath(r => r.Spawn(
                Monster(80).At(20, 13).Bonus().OnDeath(r => r.Spawn(
                    Monster(80).At(15, 15).Bonus().OnDeath(r => r.Spawn(
                        Monster(92).At(12, 17),
                        Monster(80).At(13, 13).Bonus())))),
                Monster(71).At(22, 15).OnDeath(r => r.Spawn(
                    Monster(71).At(16, 14),
                    Monster(92).At(19, 24).Bonus().OnDeath(r => r.Spawn(
                        Monster(71).At(24, 14).Bonus())))))),
            Monster(154).At(34, 13),
            Monster(154).At(30, 26),
            Monster(154).At(28, 13),
            Monster(154).At(25, 23).Bonus());
        firstBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstBattleEast, firstBattleWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(180)));

        eastHall.Portal(new(24, 36), firstStair, new(24, 3), locked: false);
        eastHall.Portal(new(2, 20), firstBattle, new(46, 18), locked: false);
        eastHall.Spawn(
            Monster(154).At(36, 16),
            Monster(154).At(32, 23).Bonus(),
            Monster(154).At(31, 12),
            Monster(154).At(24, 21),
            Monster(154).At(24, 13),
            Monster(154).At(18, 24),
            Monster(154).At(12, 14).Bonus());

        firstStair.Portal(new(24, 3), eastHall, new(24, 36), locked: false);
        firstStair.Portal(new(24, 36), secondBattle, new(24, 3), locked: false);
        firstStair.Spawn(
            Monster(154).At(32, 23).Bonus(),
            Monster(154).At(27, 13),
            Monster(154).At(26, 24),
            Monster(154).At(26, 17).Bonus(),
            Monster(154).At(22, 24),
            Monster(154).At(21, 13));

        var secondBattleNorth = secondBattle.Portal(new(24, 3), firstStair, new(24, 36));
        var secondBattleSouth = secondBattle.Portal(new(24, 36), southEastHall, new(24, 3));
        secondBattle.Spawn(
            Monster(154).At(35, 21).Bonus(),
            Monster(154).At(33, 23).OnDeath(r => r.Spawn(
                Monster(154).At(17, 12).OnDeath(r => r.Spawn(
                    Monster(80).At(24, 13).OnDeath(r => r.Spawn(
                        Monster(92).At(12, 17).Bonus(),
                        Monster(80).At(15, 15).Bonus())),
                    Monster(71).At(24, 14))))),
            Monster(154).At(29, 24),
            Monster(89).At(24, 19).OnDeath(r => r.Spawn(
                Monster(154).At(16, 24).Bonus().OnDeath(r => r.Spawn(
                    Monster(71).At(16, 14).OnDeath(r => r.Spawn(
                        Monster(92).At(19, 24),
                        Monster(80).At(20, 13).OnDeath(r => r.Spawn(
                            Monster(89).At(21, 23).OnDeath(r => r.Spawn(
                                Monster(71).At(22, 15))))))))))));
        secondBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondBattleNorth, secondBattleSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(200)));

        southEastHall.Portal(new(24, 3), secondBattle, new(24, 36), locked: false);
        southEastHall.Portal(new(2, 20), southHall, new(46, 18), locked: false);
        southEastHall.Spawn(
            Monster(154).At(36, 19).Bonus(),
            Monster(154).At(32, 15),
            Monster(154).At(26, 25),
            Monster(154).At(25, 9),
            Monster(154).At(23, 18).Bonus(),
            Monster(154).At(17, 14),
            Monster(154).At(15, 25).Bonus(),
            Monster(154).At(13, 19).Bonus());

        southHall.Portal(new(46, 18), southEastHall, new(2, 20), locked: false);
        southHall.Portal(new(2, 20), thirdBattle, new(46, 18), locked: false);
        southHall.Spawn(
            Monster(154).At(34, 19),
            Monster(154).At(34, 13).Bonus(),
            Monster(154).At(30, 23),
            Monster(154).At(27, 23),
            Monster(154).At(25, 19),
            Monster(154).At(22, 13),
            Monster(154).At(13, 18).Bonus(),
            Monster(154).At(12, 15));

        var thirdBattleEast = thirdBattle.Portal(new(46, 18), southHall, new(2, 20));
        var thirdBattleWest = thirdBattle.Portal(new(2, 20), southWestHall, new(46, 18));
        thirdBattle.Spawn(
            Monster(71).At(22, 15).Bonus().OnDeath(r => r.Spawn(
                Monster(154).At(17, 14))),
            Monster(89).At(21, 24).Bonus().OnDeath(r => r.Spawn(
                Monster(154).At(16, 24).Bonus())));
        thirdBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdBattleEast, thirdBattleWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(150)));

        southWestHall.Portal(new(24, 3), firstHoldout, new(24, 36), locked: false);
        southWestHall.Portal(new(46, 18), thirdBattle, new(2, 20), locked: false);
        southWestHall.Spawn(
            Monster(154).At(38, 18),
            Monster(154).At(30, 12),
            Monster(154).At(28, 23),
            Monster(154).At(24, 17).Bonus(),
            Monster(154).At(22, 15),
            Monster(154).At(22, 22),
            Monster(154).At(18, 26).Bonus(),
            Monster(154).At(12, 17).Bonus());
        southWestHall.Spawn(Item(1042).At(36, 20), Item(1042).At(36, 19));

        var firstHoldoutNorth = firstHoldout.Portal(new(24, 3), westHall, new(24, 36));
        var firstHoldoutSouth = firstHoldout.Portal(new(24, 36), southWestHall, new(24, 3));
        firstHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_FIGHT_TWO_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(Monster(154).Anywhere().Count(5))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Unlock(firstHoldoutNorth, firstHoldoutSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .DismissMonsters()
                .AddTime(TimeSpan.FromSeconds(180)));

        westHall.Portal(new(46, 18), secondHoldout, new(2, 20), locked: false);
        westHall.Portal(new(24, 36), firstHoldout, new(24, 3), locked: false);
        westHall.Spawn(
            Monster(154).At(37, 21).Bonus(),
            Monster(154).At(27, 24).Bonus(),
            Monster(154).At(25, 18),
            Monster(154).At(23, 20),
            Monster(154).At(19, 25),
            Monster(154).At(18, 14),
            Monster(154).At(12, 17).Bonus());

        var secondHoldoutEast = secondHoldout.Portal(new(46, 18), lastBattle, new(2, 20));
        var secondHoldoutWest = secondHoldout.Portal(new(2, 20), westHall, new(46, 18));
        secondHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_FIGHT_TWO_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(Monster(154).Anywhere().Count(6))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Unlock(secondHoldoutEast, secondHoldoutWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .DismissMonsters()
                .AddTime(TimeSpan.FromSeconds(300)));

        var lastBattleSouth = lastBattle.Portal(new(24, 36), heart, new(24, 3));
        var lastBattleWest = lastBattle.Portal(new(2, 20), secondHoldout, new(46, 18));
        lastBattle.OnEnter(r => r.Dialog(6172))
            .OnDialogClosed(6172, r => r
                .Spawn(
                    Monster(154).At(36, 19),
                    Monster(154).At(33, 24),
                    Monster(154).At(32, 14).Bonus(),
                    Monster(154).At(27, 24),
                    Monster(155).At(24, 21),
                    Monster(155).At(24, 16).Bonus(),
                    Monster(154).At(22, 14),
                    Monster(154).At(18, 25),
                    Monster(154).At(14, 21),
                    Monster(154).At(14, 15))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(lastBattleSouth, lastBattleWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(180)));

        heart.Portal(new(24, 3), lastBattle, new(24, 36));
        var exit = heart.Exit(new(2, 20));
        heart.OnEnter(r => r.Dialog(6173))
            .OnDialogClosed(6173, r => r
                .Spawn(
                    Monster(154).At(36, 20),
                    Monster(154).At(35, 17),
                    Monster(154).At(33, 24),
                    Monster(154).At(29, 13),
                    Monster(154).At(27, 24),
                    Monster(154).At(25, 15).Bonus(),
                    Monster(154).At(24, 21),
                    Monster(154).At(24, 18),
                    Monster(154).At(23, 24),
                    Monster(154).At(21, 14),
                    Monster(154).At(18, 25),
                    Monster(154).At(15, 14),
                    Monster(154).At(13, 16),
                    Monster(154).At(12, 21))
                .Objective(LanguageKey.TIMESPACE_GOAL_PATROL_TWO_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(TimeSpan.FromSeconds(30), r => r.Spawn(Monster(154).Anywhere().Count(4)), times: 3)
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(Monster(155).Anywhere().Count(2))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Unlock(exit)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6174)
                .DismissMonsters());
    }
}

return new TimeSpace44();
