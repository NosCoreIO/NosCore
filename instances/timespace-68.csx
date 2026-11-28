public class TimeSpace68 : TimeSpaceScript
{
    public TimeSpace68() : base(68, "Des incidents suspects")
    {
        Levels(70, 99);
        Start(14, 12);
        Requires(1012, 6);
        Lives(3);
        Clock(TimeSpan.FromSeconds(2100));
        Objective(ObjectiveKind.Conversation, 7119, 1);
        Draw(1016, 1);
        Draw(1012, 5);
        Draw(1013, 3);
        Special(1313, 1);
        GoldPerKill(238);
        Reputation(1050);

        var entrance = Room(4630, 0, 6);
        var hall = Room(4630, 1, 6);
        var crossroads = Room(4640, 2, 6);
        var northHall = Room(4639, 2, 5);
        var firstHoldout = Room(4630, 1, 5);
        var southHall = Room(4636, 2, 7);
        var firstLever = Room(4630, 1, 7);
        var northWay = Room(4635, 3, 5);
        var bend = Room(4633, 3, 6);
        var junction = Room(4632, 4, 6);
        var southWay = Room(4636, 4, 7);
        var firstBattle = Room(4630, 3, 7);
        var climb = Room(4634, 4, 5);
        var gallery = Room(4630, 5, 5);
        var turn = Room(4636, 6, 5);
        var upperHall = Room(4635, 6, 4);
        var upperGallery = Room(4630, 5, 4);
        var upperBend = Room(4633, 4, 4);
        var corner = Room(4634, 4, 3);
        var secondBattle = Room(4630, 5, 3);
        var thirdBattle = Room(4630, 6, 3);
        var northCrossroads = Room(4640, 7, 3);
        var topHall = Room(4635, 7, 2);
        var sideRoom = Room(4630, 6, 2);
        var secondHoldout = Room(4630, 5, 2);
        var fourthBattle = Room(4631, 7, 4);
        var eastBend = Room(4633, 7, 5);
        var eastHall = Room(4635, 8, 5);
        var eastTurn = Room(4636, 8, 6);
        var descent = Room(4639, 7, 6);
        var landing = Room(4630, 6, 6);
        var fifthBattle = Room(4630, 5, 6);
        var secondLever = Room(4631, 7, 7);
        var lowerTurn = Room(4636, 7, 8);
        var lowerBend = Room(4633, 6, 8);
        var lowerHall = Room(4635, 6, 7);
        var lowerClimb = Room(4634, 5, 7);
        var thirdLever = Room(4631, 5, 8);
        var lastBend = Room(4633, 5, 9);
        var lastBattle = Room(4635, 6, 9);
        var office = Room(4674, 6, 10);

        entrance.Portal(new(42, 14), hall, new(4, 14), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        hall.Portal(new(4, 14), entrance, new(42, 14), locked: false);
        hall.Portal(new(42, 14), crossroads, new(4, 21), locked: false);

        crossroads.Portal(new(25, 4), northHall, new(22, 38), locked: false);
        crossroads.Portal(new(22, 41), southHall, new(24, 4), locked: false);
        crossroads.Portal(new(4, 21), hall, new(42, 14), locked: false);
        crossroads.Spawn(
            Monster(484).At(24, 23).Bonus().OnDeath(r => r.Spawn(Monster(476).At(22, 33))),
            Monster(476).At(24, 14).OnDeath(r => r.Spawn(Monster(476).At(15, 23))));

        northHall.Portal(new(41, 19), northWay, new(4, 15), locked: false);
        northHall.Portal(new(22, 38), crossroads, new(25, 4), locked: false);
        northHall.Portal(new(4, 18), firstHoldout, new(42, 14), locked: false);

        var firstHoldoutDoor = firstHoldout.Portal(new(42, 14), northHall, new(4, 18));
        firstHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(200)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(Monster(175).At(20, 17).Count(2))
                .Say(Game18NConstString.EnemiesReinforcements), times: 7)
            .Every(TimeSpan.FromSeconds(30), r => r.Spawn(Monster(177).Anywhere().Count(2)),
                from: TimeSpan.FromSeconds(60), times: 3)
            .Every(TimeSpan.FromSeconds(30), r => r.Spawn(Monster(476).At(24, 16)),
                from: TimeSpan.FromSeconds(120), times: 3)
            .OnClockOut(r => r
                .Unlock(firstHoldoutDoor)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss()
                .Say(Game18NConstString.NewGoal));

        southHall.Portal(new(24, 4), crossroads, new(22, 41), locked: false);
        southHall.Portal(new(4, 29), firstLever, new(42, 14), locked: false);
        southHall.Spawn(
            Monster(484).At(27, 24),
            Monster(476).At(23, 15),
            Monster(476).At(15, 22).Bonus());

        var firstLeverDoor = firstLever.Portal(new(42, 14), southHall, new(4, 29));
        firstLever.Spawn(
            Monster(476).At(21, 8).OnDeath(r => r.Spawn(
                Monster(476).At(23, 20).OnDeath(r => r.Spawn(
                    Monster(484).At(19, 14),
                    Monster(476).At(32, 15))))),
            Monster(476).At(9, 14).Bonus());
        firstLever.Lever(new(27, 16))
            .OnUse(r => r
                .Unlock(firstLeverDoor)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal));

        northWay.Portal(new(29, 35), bend, new(15, 4), locked: false);
        northWay.Portal(new(4, 15), northHall, new(41, 19), locked: false);
        northWay.Spawn(
            Monster(177).At(26, 27).OnDeath(r => r.Spawn(Monster(177).At(26, 20))),
            Monster(177).At(19, 18).Bonus(),
            Monster(177).At(10, 14));

        bend.Portal(new(15, 4), northWay, new(29, 35), locked: false);
        bend.Portal(new(41, 25), junction, new(4, 26), locked: false);
        bend.Spawn(
            Monster(177).At(19, 14).Bonus().OnDeath(r => r.Spawn(Monster(177).At(30, 28))),
            Monster(177).At(19, 26));

        junction.Portal(new(20, 4), climb, new(15, 41), locked: false);
        junction.Portal(new(42, 26), fifthBattle, new(4, 14), locked: false);
        junction.Portal(new(24, 42), southWay, new(24, 4), locked: false);
        junction.Portal(new(4, 26), bend, new(41, 25), locked: false);

        southWay.Portal(new(24, 4), junction, new(24, 42), locked: false);
        southWay.Portal(new(4, 29), firstBattle, new(42, 14), locked: false);
        southWay.Spawn(
            Monster(177).At(22, 25),
            Monster(470).At(21, 13).Bonus(),
            Monster(476).At(17, 28));

        var firstBattleDoor = firstBattle.Portal(new(42, 14), southWay, new(4, 29));
        firstBattle.Spawn(
            Monster(177).At(25, 15),
            Monster(177).At(18, 11).Bonus().OnDeath(r => r.Spawn(
                Monster(177).At(23, 9).OnDeath(r => r.Spawn(
                    Monster(177).At(19, 20).OnDeath(r => r.Spawn(
                        Monster(484).At(11, 11).OnDeath(r => r.Spawn(Monster(484).At(15, 17))))),
                    Monster(484).At(22, 13),
                    Monster(177).At(31, 17).OnDeath(r => r.Spawn(Monster(484).At(25, 20))))))));
        firstBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstBattleDoor)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        climb.Portal(new(15, 41), junction, new(20, 4), locked: false);
        climb.Portal(new(35, 16), gallery, new(4, 14), locked: false);
        climb.Spawn(
            Monster(484).At(30, 18),
            Monster(177).At(18, 23),
            Monster(484).At(13, 34).Bonus());

        gallery.Portal(new(4, 14), climb, new(35, 16), locked: false);
        gallery.Portal(new(42, 14), turn, new(4, 29), locked: false);
        gallery.Spawn(
            Monster(177).At(27, 11).Bonus(),
            Monster(484).At(17, 19).OnDeath(r => r.Spawn(Monster(177).At(32, 16))),
            Monster(177).At(9, 14));

        turn.Portal(new(24, 4), upperHall, new(29, 35), locked: false);
        turn.Portal(new(4, 29), gallery, new(42, 14), locked: false);

        upperHall.Portal(new(29, 35), turn, new(24, 4), locked: false);
        upperHall.Portal(new(4, 15), upperGallery, new(42, 14), locked: false);
        upperHall.Spawn(
            Monster(476).At(29, 32).Bonus(),
            Monster(177).At(21, 24),
            Monster(476).At(14, 11));

        upperGallery.Portal(new(4, 14), upperBend, new(41, 25), locked: false);
        upperGallery.Portal(new(42, 14), upperHall, new(4, 15), locked: false);
        upperGallery.Spawn(
            Monster(177).At(30, 20),
            Monster(476).At(29, 12).Bonus(),
            Monster(476).At(17, 17),
            Monster(177).At(10, 9));

        upperBend.Portal(new(15, 4), corner, new(15, 41), locked: false);
        upperBend.Portal(new(41, 25), upperGallery, new(4, 14), locked: false);

        corner.Portal(new(15, 41), upperBend, new(15, 4), locked: false);
        corner.Portal(new(35, 16), secondBattle, new(4, 14), locked: false);
        corner.Spawn(
            Monster(476).At(21, 21),
            Monster(476).At(18, 40),
            Monster(177).At(15, 33),
            Monster(476).At(16, 18).Bonus());

        var secondBattleWest = secondBattle.Portal(new(4, 14), corner, new(35, 16));
        var secondBattleEast = secondBattle.Portal(new(42, 14), thirdBattle, new(4, 14));
        secondBattle.Spawn(
            Monster(484).At(27, 16).OnDeath(r => r.Spawn(
                Monster(476).At(17, 12).Bonus(),
                Monster(476).At(18, 19).OnDeath(r => r.Spawn(
                    Monster(476).At(23, 18).OnDeath(r => r.Spawn(Monster(467).At(21, 14))))))),
            Monster(484).At(25, 12).Bonus());
        secondBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondBattleWest, secondBattleEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        // Cleared by its captain's fall, whatever else still stands.
        var thirdBattleWest = thirdBattle.Portal(new(4, 14), secondBattle, new(42, 14));
        var thirdBattleEast = thirdBattle.Portal(new(42, 14), northCrossroads, new(4, 21));
        thirdBattle.Spawn(
            Monster(484).At(27, 16),
            Monster(484).At(27, 11),
            Monster(476).At(16, 17).Bonus(),
            Monster(467).At(21, 15).OnDeath(r => r.Spawn(Monster(484).At(23, 20))));
        thirdBattle.ClearedAfter(1, 467);
        thirdBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdBattleWest, thirdBattleEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        northCrossroads.Portal(new(25, 4), topHall, new(29, 35), locked: false);
        northCrossroads.Portal(new(22, 41), fourthBattle, new(14, 4), locked: false);
        northCrossroads.Portal(new(4, 21), thirdBattle, new(42, 14), locked: false);

        topHall.Portal(new(29, 35), northCrossroads, new(25, 4), locked: false);
        topHall.Portal(new(4, 15), sideRoom, new(42, 14), locked: false);
        topHall.Spawn(
            Monster(476).At(27, 33).Bonus().OnDeath(r => r.Spawn(Monster(476).At(23, 18))),
            Monster(177).At(23, 26),
            Monster(177).At(18, 17),
            Monster(476).At(12, 15));

        sideRoom.Portal(new(42, 14), topHall, new(4, 15), locked: false);
        var sideRoomWest = sideRoom.Portal(new(4, 14), secondHoldout, new(42, 14));
        sideRoom.Spawn(
            Monster(177).At(26, 9).OnDeath(r => r.Spawn(Monster(177).At(32, 11))),
            Monster(476).At(23, 18),
            Monster(476).At(15, 14).Bonus());
        sideRoom.OnEnter(r => r.Unlock(sideRoomWest).Say(Game18NConstString.ADoorHasOpened));

        var secondHoldoutDoor = secondHoldout.Portal(new(42, 14), sideRoom, new(4, 14));
        secondHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(270)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(Monster(177).Anywhere().Count(2))
                .Say(Game18NConstString.EnemiesReinforcements), times: 9)
            .OnClockOut(r => r
                .Unlock(secondHoldoutDoor)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss()
                .Say(Game18NConstString.NewGoal));

        var fourthBattleNorth = fourthBattle.Portal(new(14, 4), northCrossroads, new(22, 41));
        var fourthBattleSouth = fourthBattle.Portal(new(14, 42), eastBend, new(15, 4));
        fourthBattle.Spawn(
            Monster(476).At(18, 36).Bonus(),
            Monster(476).At(14, 30),
            Monster(476).At(11, 25).OnDeath(r => r.Spawn(
                Monster(467).At(20, 24).OnDeath(r => r.Spawn(Monster(467).At(16, 21))))));
        fourthBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(fourthBattleNorth, fourthBattleSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        eastBend.Portal(new(15, 4), fourthBattle, new(14, 42), locked: false);
        eastBend.Portal(new(41, 25), eastHall, new(4, 15), locked: false);

        eastHall.Portal(new(29, 35), eastTurn, new(24, 4), locked: false);
        eastHall.Portal(new(4, 15), eastBend, new(41, 25), locked: false);
        eastHall.Spawn(
            Monster(484).At(27, 20).Bonus(),
            Monster(476).At(26, 30),
            Monster(476).At(11, 19));

        eastTurn.Portal(new(24, 4), eastHall, new(29, 35), locked: false);
        eastTurn.Portal(new(4, 29), descent, new(41, 19), locked: false);

        descent.Portal(new(41, 19), eastTurn, new(4, 29), locked: false);
        descent.Portal(new(4, 18), landing, new(42, 14), locked: false);
        var descentSouth = descent.Portal(new(22, 38), secondLever, new(14, 4));
        descent.Spawn(
            Monster(177).At(32, 18).Bonus(),
            Monster(177).At(23, 27),
            Monster(476).At(19, 19));
        descent.OnEnter(r => r.Unlock(descentSouth).Say(Game18NConstString.ADoorHasOpened));

        var landingEast = landing.Portal(new(42, 14), descent, new(4, 18));
        landing.Portal(new(4, 14), fifthBattle, new(42, 14), locked: false);
        landing.Spawn(
            Monster(177).At(35, 16),
            Monster(467).At(27, 8).OnDeath(r => r.Spawn(Monster(177).At(11, 14), Monster(177).At(14, 19))),
            Monster(177).At(22, 13).Bonus());
        landing.OnEnter(r => r.Unlock(landingEast).Say(Game18NConstString.ADoorHasOpened));

        var fifthBattleWest = fifthBattle.Portal(new(4, 14), junction, new(42, 26));
        var fifthBattleEast = fifthBattle.Portal(new(42, 14), landing, new(4, 14));
        fifthBattle.Spawn(
            Monster(484).At(30, 10).OnDeath(r => r.Spawn(
                Monster(177).At(19, 17).OnDeath(r => r.Spawn(Monster(467).At(23, 13))),
                Monster(177).At(30, 17))),
            Monster(476).At(12, 10).Bonus());
        fifthBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(fifthBattleWest, fifthBattleEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        // Its way back never opens.
        secondLever.Portal(new(14, 4), descent, new(22, 38));
        var secondLeverSouth = secondLever.Portal(new(14, 42), lowerTurn, new(24, 4));
        secondLever.Spawn(
            Monster(469).At(14, 35).Bonus(),
            Monster(177).At(12, 24));
        secondLever.Lever(new(14, 37))
            .OnUse(r => r
                .Unlock(secondLeverSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal));

        lowerTurn.Portal(new(24, 4), secondLever, new(14, 42), locked: false);
        lowerTurn.Portal(new(4, 29), lowerBend, new(41, 25), locked: false);

        lowerBend.Portal(new(15, 4), lowerHall, new(29, 35), locked: false);
        lowerBend.Portal(new(41, 25), lowerTurn, new(4, 29), locked: false);
        lowerBend.Spawn(
            Monster(177).At(31, 25).Bonus(),
            Monster(177).At(25, 16),
            Monster(177).At(20, 21));

        lowerHall.Portal(new(29, 35), lowerBend, new(15, 4), locked: false);
        lowerHall.Portal(new(4, 15), lowerClimb, new(35, 16), locked: false);
        lowerHall.Spawn(
            Monster(177).At(25, 30),
            Monster(177).At(25, 20).Bonus(),
            Monster(177).At(17, 13));

        lowerClimb.Portal(new(35, 16), lowerHall, new(4, 15), locked: false);
        lowerClimb.Portal(new(15, 41), thirdLever, new(14, 4), locked: false);

        // Its way back never opens.
        thirdLever.Portal(new(14, 4), lowerClimb, new(15, 41));
        var thirdLeverSouth = thirdLever.Portal(new(14, 42), lastBend, new(15, 4));
        thirdLever.Spawn(
            Monster(484).At(20, 25).OnDeath(r => r.Spawn(Monster(469).At(13, 25))),
            Monster(177).At(17, 34).Bonus(),
            Monster(177).At(14, 18));
        thirdLever.Lever(new(15, 27))
            .OnUse(r => r
                .Unlock(thirdLeverSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.NewGoal));

        lastBend.Portal(new(15, 4), thirdLever, new(14, 42), locked: false);
        lastBend.Portal(new(41, 25), lastBattle, new(4, 15), locked: false);

        lastBattle.Portal(new(4, 15), lastBend, new(41, 25), locked: false);
        var lastBattleSouth = lastBattle.Portal(new(29, 35), office, new(15, 20));
        lastBattle.Spawn(
            Monster(177).At(27, 30),
            Monster(469).At(25, 20),
            Monster(177).At(10, 16).Bonus());
        lastBattle.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(lastBattleSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        office.Portal(new(15, 20), lastBattle, new(29, 35), locked: false);
        office.Exit(new(15, 42), locked: false);
        office.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(3)))
            .OnClockOut(r => r.Dialog(7119).Say(Game18NConstString.NewGoal))
            .OnDialogClosed(7119, r => r.Say(Game18NConstString.NewGoal));
    }
}

return new TimeSpace68();
