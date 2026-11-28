public class TimeSpace404 : TimeSpaceScript
{
    public TimeSpace404() : base(404, "Unlimited Hatred")
    {
        Levels(65, 99);
        Start(8, 11);
        Lives(3);
        Clock(TimeSpan.FromSeconds(1260));
        GoldPerKill(208);
        Reputation(1950);

        var entrance = Room(4409, 4, 11);
        var sentinels = Room(4406, 5, 11);
        var sentinelPassage = Room(4410, 5, 10);
        var sentinelHall = Room(4408, 5, 9);
        var moles = Room(4409, 6, 9);
        var whitneyFight = Room(4406, 7, 9);
        var eastPassage = Room(4410, 7, 8);
        var blockedPath = Room(4405, 7, 7);
        var raccoonPassage = Room(4409, 6, 7);
        var leverHall = Room(4402, 5, 7);
        var southPit = Room(4410, 5, 8);
        var leeches = Room(4404, 5, 6);
        var westPassage = Room(4409, 4, 6);
        var westLever = Room(4409, 3, 6);
        var lizards = Room(4410, 5, 5);
        var lizardHall = Room(4404, 5, 4);
        var northPit = Room(4410, 5, 3);
        var raccoonCrossing = Room(4409, 4, 4);
        var raccoonHall = Room(4408, 3, 4);
        var sentinelCorner = Room(4406, 3, 5);
        var angelRoom = Room(4409, 2, 5);
        var exitRoom = Room(4409, 1, 5);

        entrance.Portal(new(23, 11), sentinels, new(1, 12), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var sentinelsDoors = new[]
        {
            sentinels.Portal(new(12, 1), sentinelPassage, new(12, 23)),
            sentinels.Portal(new(1, 12), entrance, new(23, 11))
        };
        sentinels.Spawn(Monster(477).At(7, 4), Monster(477).At(6, 18).Bonus());
        sentinels.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_MONSTER_ATTACK))
            .OnKills(2, r => r.Spawn(Monster(477).At(11, 11)))
            .OnCleared(r => r.Unlock(sentinelsDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        sentinelPassage.Portal(new(12, 1), sentinelHall, new(12, 23), locked: false);
        sentinelPassage.Portal(new(12, 23), sentinels, new(12, 1), locked: false);
        sentinelPassage.Spawn(Monster(477).At(8, 17).Bonus(), Monster(477).At(9, 6));
        sentinelPassage.OnKills(2, r => r.Spawn(Monster(477).At(16, 16)));

        var sentinelHallDoors = new[]
        {
            sentinelHall.Portal(new(23, 12), moles, new(1, 11)),
            sentinelHall.Portal(new(12, 23), sentinelPassage, new(12, 1))
        };
        sentinelHall.Spawn(Monster(477).At(20, 18).Bonus(), Monster(477).At(6, 12));
        sentinelHall.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_MONSTER_ATTACK))
            .OnKills(2, r => r.Spawn(Monster(477).At(8, 19)))
            .OnKills(3, r => r.Spawn(Monster(478).At(11, 7)))
            .OnCleared(r => r.Unlock(sentinelHallDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        moles.Portal(new(23, 11), whitneyFight, new(1, 12), locked: false);
        moles.Portal(new(1, 11), sentinelHall, new(23, 12), locked: false);
        moles.Spawn(Monster(480).At(20, 14), Monster(480).At(11, 7).Bonus());
        moles.OnKills(1, r => r.Spawn(Monster(480).At(6, 6)))
            .OnKills(2, r => r.Spawn(Monster(480).At(6, 15)))
            .OnKills(3, r => r.Spawn(Monster(480).At(10, 17)));

        // Whitney fights the party until she falls; her spearmen go with her.
        var whitneyFightDoors = new[]
        {
            whitneyFight.Portal(new(12, 1), eastPassage, new(12, 23)),
            whitneyFight.Portal(new(1, 12), moles, new(23, 11))
        };
        whitneyFight.ClearedAfter(1, 503);
        whitneyFight.OnEnter(r => r.Dialog(7321))
            .OnDialogClosed(7321, r => r
                .Spawn(Monster(253).At(6, 7).Bonus(), Monster(503).At(10, 12))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnKills(1, r => r.Spawn(Monster(253).At(5, 17), Monster(253).At(12, 4)))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(whitneyFightDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        eastPassage.Portal(new(12, 1), blockedPath, new(12, 23), locked: false);
        eastPassage.Portal(new(12, 23), whitneyFight, new(12, 1), locked: false);
        eastPassage.Spawn(Monster(485).At(17, 15), Monster(485).At(19, 5).Bonus(), Monster(485).At(9, 14));
        eastPassage.OnKills(2, r => r.Spawn(Monster(485).At(9, 6)))
            .OnKills(3, r => r.Spawn(Monster(485).At(11, 18)));

        // Whitney waits where the way down is blocked, then follows the party to the end.
        blockedPath.Portal(new(12, 23), eastPassage, new(12, 1), locked: false);
        var blockedPathWest = blockedPath.Portal(new(1, 12), raccoonPassage, new(23, 11));
        blockedPath.Spawn(Npc(503).At(10, 14).Escort());
        blockedPath.OnEnter(r => r.Unlock(blockedPathWest).Say(Game18NConstString.ADoorHasOpened).Dialog(7322));

        raccoonPassage.Portal(new(23, 11), blockedPath, new(1, 12), locked: false);
        raccoonPassage.Portal(new(1, 11), leverHall, new(23, 12), locked: false);
        raccoonPassage.Spawn(Monster(183).At(20, 13), Monster(183).At(16, 9), Monster(183).At(6, 16), Monster(183).At(5, 5).Bonus());
        raccoonPassage.OnKills(2, r => r.Spawn(Monster(183).At(8, 11)))
            .OnKills(3, r => r.Spawn(Monster(183).At(16, 8)));

        // The lever opens the way back and the way south and wakes three moles; the way north waits
        // for the pit to the south.
        var leverHallNorth = leverHall.Portal(new(12, 1), leeches, new(12, 23));
        var leverHallEast = leverHall.Portal(new(23, 12), raccoonPassage, new(1, 11));
        var leverHallSouth = leverHall.Portal(new(12, 23), southPit, new(12, 1));
        leverHall.Lever(new(20, 13))
            .OnUse(r => r
                .Unlock(leverHallEast, leverHallSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Spawn(Monster(480).At(18, 4), Monster(480).At(10, 17).Bonus(), Monster(480).At(6, 4)));

        var southPitNorth = southPit.Portal(new(12, 1), leverHall, new(12, 23));
        southPit.Spawn(Monster(485).At(17, 11), Monster(485).At(7, 17).Bonus());
        southPit.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnKills(2, r => r.Spawn(Monster(478).At(8, 7), Monster(478).At(14, 20)))
            .OnCleared(r => r
                .Unlock(southPitNorth, leverHallNorth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var leechesNorth = leeches.Portal(new(12, 1), lizards, new(12, 23));
        leeches.Portal(new(12, 23), leverHall, new(12, 1), locked: false);
        var leechesWest = leeches.Portal(new(1, 12), westPassage, new(23, 11));
        leeches.Spawn(Monster(467).At(14, 8), Monster(467).At(5, 17).Bonus());
        leeches.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnKills(2, r => r.Spawn(Monster(476).At(6, 7)))
            .OnKills(3, r => r.Spawn(Monster(476).At(16, 18), Monster(476).At(18, 4)))
            .OnCleared(r => r.Unlock(leechesWest).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        westPassage.Portal(new(23, 11), leeches, new(1, 12), locked: false);
        westPassage.Portal(new(1, 11), westLever, new(23, 11), locked: false);

        westLever.Portal(new(23, 11), westPassage, new(1, 11), locked: false);
        westLever.Lever(new(14, 9))
            .OnUse(r => r.Unlock(leechesNorth).Say(Game18NConstString.ADoorHasOpened));

        lizards.Portal(new(12, 1), lizardHall, new(12, 23), locked: false);
        lizards.Portal(new(12, 23), leeches, new(12, 1), locked: false);
        lizards.Spawn(Monster(179).At(19, 6), Monster(179).At(13, 18), Monster(179).At(10, 20).Bonus(), Monster(179).At(5, 4));

        var lizardHallNorth = lizardHall.Portal(new(12, 1), northPit, new(12, 23));
        lizardHall.Portal(new(12, 23), lizards, new(12, 1), locked: false);
        var lizardHallWest = lizardHall.Portal(new(1, 12), raccoonCrossing, new(23, 11));
        lizardHall.Spawn(Monster(179).At(6, 18), Monster(179).At(6, 8).Bonus());
        lizardHall.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnKills(2, r => r.Spawn(Monster(179).At(14, 12)))
            .OnKills(3, r => r.Spawn(Monster(179).At(15, 5), Monster(179).At(17, 18)))
            .OnCleared(r => r.Unlock(lizardHallNorth).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        // A dead end whose clearing opens the way west, two rooms back.
        var northPitSouth = northPit.Portal(new(12, 23), lizardHall, new(12, 1));
        northPit.Spawn(Monster(485).At(8, 17), Monster(485).At(7, 5).Bonus());
        northPit.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnKills(2, r => r.Spawn(Monster(485).At(16, 9)))
            .OnKills(3, r => r.Spawn(Monster(485).At(12, 1), Monster(485).At(14, 20)))
            .OnCleared(r => r
                .Unlock(northPitSouth, lizardHallWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        raccoonCrossing.Portal(new(23, 11), lizardHall, new(1, 12), locked: false);
        raccoonCrossing.Portal(new(1, 11), raccoonHall, new(23, 12), locked: false);
        raccoonCrossing.Spawn(Monster(183).At(19, 10), Monster(183).At(5, 18).Bonus(), Monster(183).At(6, 8));
        raccoonCrossing.OnKills(2, r => r.Spawn(Monster(183).At(12, 15)))
            .OnKills(3, r => r.Spawn(Monster(183).At(20, 17)));

        var raccoonHallDoors = new[]
        {
            raccoonHall.Portal(new(23, 12), raccoonCrossing, new(1, 11)),
            raccoonHall.Portal(new(12, 23), sentinelCorner, new(12, 1))
        };
        raccoonHall.Spawn(Monster(182).At(11, 9).Bonus(), Monster(182).At(7, 18));
        raccoonHall.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnKills(2, r => r.Spawn(Monster(182).At(18, 20)))
            .OnKills(3, r => r.Spawn(Monster(472).At(10, 19), Monster(182).At(18, 8)))
            .OnCleared(r => r.Unlock(raccoonHallDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        sentinelCorner.Portal(new(12, 1), raccoonHall, new(12, 23), locked: false);
        sentinelCorner.Portal(new(1, 12), angelRoom, new(23, 11), locked: false);
        sentinelCorner.Spawn(Monster(486).At(17, 12).Bonus(), Monster(486).At(7, 6), Monster(486).At(5, 17));

        // Angel Jabiniel and Enkidu wait behind the talk; Enkidu leaves once the angel falls.
        var angelRoomDoors = new[]
        {
            angelRoom.Portal(new(23, 11), sentinelCorner, new(1, 12)),
            angelRoom.Portal(new(1, 11), exitRoom, new(23, 11))
        };
        angelRoom.ClearedAfter(1, 517);
        angelRoom.OnEnter(r => r.Dialog(7323))
            .OnDialogClosed(7323, r => r
                .Spawn(Monster(520).At(14, 7), Monster(517).At(6, 12).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_BOSS))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(angelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        exitRoom.Portal(new(23, 11), angelRoom, new(1, 11), locked: false);
        exitRoom.Exit(new(1, 11), locked: false)
            .OnReached(r => r.Say(Game18NConstString.PleaseWait))
            .WinsAfterDialog(7324);
    }
}

return new TimeSpace404();
