public class TimeSpace406 : TimeSpaceScript
{
    public TimeSpace406() : base(406, "Black Flame")
    {
        Levels(65, 99);
        Start(12, 13);
        Lives(3);
        Clock(TimeSpan.FromSeconds(1500));
        GoldPerKill(208);
        Reputation(1950);

        var entrance = Room(4653, 5, 8);
        var southHub = Room(4654, 5, 7);
        var eastLever = Room(4652, 6, 7);
        var westHall = Room(4652, 4, 7);
        var justinFight = Room(4653, 5, 6);
        var centreHub = Room(4654, 5, 5);
        var pigeons = Room(4658, 6, 5);
        var eastHoldout = Room(4656, 6, 4);
        var greylander = Room(4657, 7, 4);
        var maples = Room(4655, 7, 5);
        var farEastHoldout = Room(4652, 8, 5);
        var greenLizards = Room(4655, 4, 5);
        var mothHoldout = Room(4657, 4, 4);
        var westGreylander = Room(4656, 3, 4);
        var wollets = Room(4658, 3, 5);
        var farWestHoldout = Room(4652, 2, 5);
        var northPassage = Room(4653, 5, 4);
        var northHub = Room(4654, 5, 3);
        var northLever = Room(4652, 4, 3);
        var echekaruelMeeting = Room(4652, 6, 3);
        var angelRoom = Room(4653, 5, 2);
        var exitRoom = Room(4653, 5, 1);

        var thirtySeconds = TimeSpan.FromSeconds(30);

        entrance.Portal(new(22, 4), southHub, new(19, 39), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var southHubNorth = southHub.Portal(new(21, 5), justinFight, new(22, 38));
        southHub.Portal(new(39, 21), eastLever, new(4, 21), locked: false);
        southHub.Portal(new(19, 39), entrance, new(22, 4), locked: false);
        var southHubWest = southHub.Portal(new(4, 18), westHall, new(38, 21));

        eastLever.Portal(new(4, 21), southHub, new(39, 21), locked: false);
        eastLever.Spawn(Monster(183).At(32, 18).Bonus(), Monster(183).At(28, 24), Monster(183).At(16, 15), Monster(183).At(15, 23));
        eastLever.Lever(new(32, 23))
            .OnUse(r => r.Unlock(southHubWest).Say(Game18NConstString.ADoorHasOpened));

        var westHallEast = westHall.Portal(new(38, 21), southHub, new(4, 18));
        westHall.Spawn(
            Monster(181).At(26, 27),
            Monster(181).At(25, 17),
            Monster(181).At(18, 27),
            Monster(180).At(16, 18),
            Monster(180).At(8, 24),
            Monster(180).At(8, 17).Bonus());
        westHall.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(westHallEast, southHubNorth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        // Justin fights the party for his revenge until he falls; his spearmen go with him.
        var justinFightDoors = new[]
        {
            justinFight.Portal(new(22, 4), centreHub, new(19, 39)),
            justinFight.Portal(new(22, 38), southHub, new(21, 5))
        };
        justinFight.ClearedAfter(1, 502);
        justinFight.OnEnter(r => r.Dialog(7330))
            .OnDialogClosed(7330, r => r
                .Spawn(
                    Monster(253).At(31, 28),
                    Monster(253).At(30, 8),
                    Monster(253).At(29, 21),
                    Monster(253).At(15, 18).Bonus(),
                    Monster(253).At(13, 9),
                    Monster(253).At(12, 29),
                    Monster(502).At(20, 15))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(justinFightDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(7331));

        // The ways north and west open once the holdouts at the far east and the far west are over.
        var centreHubNorth = centreHub.Portal(new(21, 5), northPassage, new(22, 38));
        centreHub.Portal(new(39, 21), pigeons, new(4, 21), locked: false);
        centreHub.Portal(new(19, 39), justinFight, new(22, 4), locked: false);
        var centreHubWest = centreHub.Portal(new(4, 18), greenLizards, new(38, 24));

        pigeons.Portal(new(25, 4), eastHoldout, new(17, 38), locked: false);
        pigeons.Portal(new(4, 21), centreHub, new(39, 21), locked: false);
        pigeons.Spawn(
            Monster(181).At(32, 21),
            Monster(181).At(33, 13),
            Monster(177).At(26, 25),
            Monster(181).At(17, 21).Bonus(),
            Monster(177).At(15, 30),
            Monster(177).At(10, 18));

        // Each holdout holds its doors for two minutes while a wave comes every thirty seconds, then
        // takes the monsters still standing away.
        var eastHoldoutDoors = new[]
        {
            eastHoldout.Portal(new(38, 21), greylander, new(4, 18)),
            eastHoldout.Portal(new(17, 38), pigeons, new(25, 4))
        };
        eastHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_MONSTER_ATTACK)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements))
            .Every(thirtySeconds, r => r.Spawn(Monster(177).At(13, 22), Monster(181).At(15, 32), Monster(181).At(29, 26)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(180).At(16, 13), Monster(180).At(29, 14)), from: TimeSpan.FromSeconds(60))
            .OnClockOut(r => r
                .Unlock(eastHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        greylander.Portal(new(23, 38), maples, new(21, 4), locked: false);
        greylander.Portal(new(4, 18), eastHoldout, new(38, 21), locked: false);
        greylander.Spawn(Monster(181).At(27, 33), Monster(475).At(19, 21), Monster(181).At(11, 14).Bonus());

        maples.Portal(new(21, 4), greylander, new(23, 38), locked: false);
        maples.Portal(new(38, 24), farEastHoldout, new(4, 21), locked: false);
        maples.Spawn(Monster(190).At(30, 27).Bonus(), Monster(191).At(16, 20));
        maples.OnKills(1, r => r.Spawn(Monster(191).At(29, 12)));

        var farEastHoldoutWest = farEastHoldout.Portal(new(4, 21), maples, new(38, 24));
        farEastHoldout.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_MONSTER_ATTACK)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements))
            .Every(thirtySeconds, r => r.Spawn(Monster(183).At(15, 16), Monster(183).At(27, 27)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(181).At(20, 23), Monster(181).At(28, 17)), from: TimeSpan.FromSeconds(60))
            .OnClockOut(r => r
                .Unlock(farEastHoldoutWest, centreHubWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        greenLizards.Portal(new(21, 4), mothHoldout, new(23, 38), locked: false);
        greenLizards.Portal(new(38, 24), centreHub, new(4, 18), locked: false);
        greenLizards.Spawn(Monster(180).At(29, 8).Bonus(), Monster(180).At(26, 32), Monster(180).At(15, 7));
        greenLizards.OnKills(1, r => r.Spawn(Monster(180).At(22, 17)))
            .OnKills(2, r => r.Spawn(Monster(180).At(13, 24)));

        var mothHoldoutDoors = new[]
        {
            mothHoldout.Portal(new(23, 38), greenLizards, new(21, 4)),
            mothHoldout.Portal(new(4, 18), westGreylander, new(38, 21))
        };
        mothHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements))
            .Every(thirtySeconds, r => r.Spawn(Monster(478).At(12, 24), Monster(478).At(27, 16)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(193).At(15, 17)), from: TimeSpan.FromSeconds(60))
            .OnClockOut(r => r
                .Unlock(mothHoldoutDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        westGreylander.Portal(new(38, 21), mothHoldout, new(4, 18), locked: false);
        westGreylander.Portal(new(17, 38), wollets, new(25, 4), locked: false);
        westGreylander.Spawn(Monster(177).At(34, 25), Monster(475).At(17, 20).Bonus(), Monster(177).At(8, 26));

        wollets.Portal(new(25, 4), westGreylander, new(17, 38), locked: false);
        wollets.Portal(new(4, 21), farWestHoldout, new(38, 21), locked: false);
        wollets.Spawn(
            Monster(177).At(31, 9),
            Monster(183).At(32, 27),
            Monster(177).At(20, 18),
            Monster(177).At(11, 28).Bonus(),
            Monster(183).At(9, 14));

        var farWestHoldoutEast = farWestHoldout.Portal(new(38, 21), wollets, new(4, 21));
        farWestHoldout.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(thirtySeconds, r => r.Say(Game18NConstString.EnemiesReinforcements))
            .Every(thirtySeconds, r => r.Spawn(Monster(180).At(12, 16)))
            .Every(thirtySeconds, r => r.Spawn(Monster(180).At(15, 28), Monster(180).At(28, 18)), times: 3)
            .Every(thirtySeconds, r => r.Spawn(Monster(478).At(31, 27)), from: TimeSpan.FromSeconds(60))
            .OnClockOut(r => r
                .Unlock(farWestHoldoutEast, centreHubNorth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .Dismiss());

        northPassage.Portal(new(22, 4), northHub, new(19, 39), locked: false);
        northPassage.Portal(new(22, 38), centreHub, new(21, 5), locked: false);

        var northHubNorth = northHub.Portal(new(21, 5), angelRoom, new(22, 38));
        var northHubEast = northHub.Portal(new(39, 21), echekaruelMeeting, new(4, 21));
        northHub.Portal(new(19, 39), northPassage, new(22, 4), locked: false);
        northHub.Portal(new(4, 18), northLever, new(38, 21), locked: false);

        northLever.Portal(new(38, 21), northHub, new(4, 18), locked: false);
        northLever.Lever(new(35, 8))
            .OnUse(r => r.Unlock(northHubEast).Say(Game18NConstString.ADoorHasOpened));

        // Arcke finds Echekaruel; the talk opens the way north.
        var echekaruelMeetingWest = echekaruelMeeting.Portal(new(4, 21), northHub, new(39, 21));
        echekaruelMeeting.OnEnter(r => r
            .Unlock(echekaruelMeetingWest, northHubNorth)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(7332));

        // Echekaruel leaves Angel Reedwhanel to hold the way; Whitney follows the party from here, Justin stays.
        var angelRoomDoors = new[]
        {
            angelRoom.Portal(new(22, 4), exitRoom, new(22, 38)),
            angelRoom.Portal(new(22, 38), northHub, new(21, 5))
        };
        angelRoom.Spawn(Npc(503).At(24, 36).Escort(), Npc(502).At(16, 36));
        angelRoom.ClearedAfter(1, 519);
        angelRoom.OnEnter(r => r.Dialog(7333))
            .OnDialogClosed(7333, r => r
                .Spawn(
                    Monster(520).At(30, 19),
                    Monster(192).At(29, 26),
                    Monster(192).At(15, 27),
                    Monster(192).At(13, 7),
                    Monster(192).At(10, 21).Bonus(),
                    Monster(519).At(20, 16))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnKills(2, r => r.Spawn(Monster(181).At(21, 25), Monster(181).At(28, 9)))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(angelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        exitRoom.Portal(new(22, 38), angelRoom, new(22, 4), locked: false);
        exitRoom.Exit(new(22, 4), locked: false)
            .OnReached(r => r.Say(Game18NConstString.PleaseWait))
            .WinsAfterDialog(7334);
    }
}

return new TimeSpace406();
