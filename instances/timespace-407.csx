public class TimeSpace407 : TimeSpaceScript
{
    public TimeSpace407() : base(407, "Echekaruel Mysticaron")
    {
        Levels(65, 99);
        Start(21, 14);
        Lives(2);
        Clock(TimeSpan.FromSeconds(1500));
        GoldPerKill(208);
        Reputation(1950);

        var entrance = Room(4676, 5, 11);
        var reunion = Room(4676, 5, 10);
        var firstGate = Room(4676, 5, 9);
        var followers = Room(4676, 5, 8);
        var secondGate = Room(4676, 5, 7);
        var thirdGate = Room(4676, 5, 6);
        var fourthGate = Room(4676, 5, 5);
        var jabinielRoom = Room(4676, 5, 4);
        var israelRoom = Room(4676, 5, 3);
        var reedwhanelRoom = Room(4676, 5, 2);
        var meeting = Room(4676, 5, 1);
        var echekaruelRoom = Room(4677, 6, 1);
        var exitRoom = Room(4677, 7, 1);

        entrance.Portal(new(26, 7), reunion, new(26, 34), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        // Whitney, Kupei and Justin join the party and follow it to the end.
        reunion.Portal(new(26, 7), firstGate, new(26, 34), locked: false);
        reunion.Portal(new(26, 34), entrance, new(26, 7), locked: false);
        reunion.Spawn(Npc(503).At(32, 25).Escort(), Npc(515).At(25, 20).Escort(), Npc(502).At(21, 27).Escort());
        reunion.OnEnter(r => r.Dialog(7335));

        // Every fight brings more of Echekaruel's followers on its third and fifth kills, and some on others.
        var firstGateDoors = new[]
        {
            firstGate.Portal(new(26, 7), followers, new(26, 34)),
            firstGate.Portal(new(26, 34), reunion, new(26, 7))
        };
        firstGate.Spawn(
            Monster(477).At(36, 10),
            Monster(520).At(28, 22).Bonus(),
            Monster(520).At(18, 9),
            Monster(477).At(12, 13),
            Monster(477).At(10, 23));
        firstGate.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_ENEMY))
            .OnKills(3, r => r.Spawn(Monster(520).At(16, 28), Monster(477).At(24, 11), Monster(520).At(33, 27), Monster(477).At(40, 21)))
            .OnKills(5, r => r.Spawn(Monster(520).At(19, 16), Monster(477).At(21, 24), Monster(520).At(25, 17), Monster(477).At(33, 17)))
            .OnCleared(r => r.Unlock(firstGateDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        followers.Portal(new(26, 7), secondGate, new(26, 34), locked: false);
        followers.Portal(new(26, 34), firstGate, new(26, 7), locked: false);
        followers.OnEnter(r => r.Dialog(7336));

        var secondGateDoors = new[]
        {
            secondGate.Portal(new(26, 7), thirdGate, new(26, 34)),
            secondGate.Portal(new(26, 34), followers, new(26, 7))
        };
        secondGate.Spawn(
            Monster(485).At(38, 9),
            Monster(483).At(22, 20),
            Monster(520).At(19, 11),
            Monster(485).At(12, 12),
            Monster(485).At(11, 22).Bonus());
        secondGate.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_ENEMY))
            .OnKills(3, r => r.Spawn(Monster(485).At(16, 27), Monster(520).At(26, 7), Monster(483).At(30, 17), Monster(485).At(37, 24)))
            .OnKills(5, r => r.Spawn(
                Monster(520).At(15, 18),
                Monster(520).At(23, 27),
                Monster(485).At(26, 11),
                Monster(520).At(30, 25),
                Monster(520).At(37, 18)))
            .OnCleared(r => r.Unlock(secondGateDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        var thirdGateDoors = new[]
        {
            thirdGate.Portal(new(26, 7), fourthGate, new(26, 34)),
            thirdGate.Portal(new(26, 34), secondGate, new(26, 7))
        };
        thirdGate.Spawn(Monster(520).At(41, 16), Monster(486).At(40, 23), Monster(486).At(37, 11), Monster(520).At(28, 24).Bonus());
        thirdGate.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_ENEMY))
            .OnKills(3, r => r.Spawn(
                Monster(486).At(13, 24),
                Monster(486).At(20, 8),
                Monster(520).At(27, 11),
                Monster(486).At(34, 7),
                Monster(486).At(35, 28)))
            .OnKills(4, r => r.Spawn(Monster(486).At(15, 10)))
            .OnKills(5, r => r.Spawn(
                Monster(486).At(20, 15),
                Monster(486).At(20, 27),
                Monster(520).At(21, 21),
                Monster(520).At(25, 13),
                Monster(486).At(31, 15),
                Monster(486).At(33, 20)))
            .OnKills(6, r => r.Spawn(Monster(520).At(13, 16)))
            .OnCleared(r => r.Unlock(thirdGateDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        var fourthGateDoors = new[]
        {
            fourthGate.Portal(new(26, 7), jabinielRoom, new(26, 34)),
            fourthGate.Portal(new(26, 34), thirdGate, new(26, 7))
        };
        fourthGate.Spawn(
            Monster(486).At(39, 21),
            Monster(520).At(31, 27),
            Monster(486).At(24, 21),
            Monster(520).At(14, 23),
            Monster(486).At(15, 14),
            Monster(520).At(11, 16).Bonus());
        fourthGate.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_THE_ENEMY))
            .OnKills(3, r => r.Spawn(
                Monster(486).At(17, 18),
                Monster(486).At(19, 7),
                Monster(520).At(30, 16),
                Monster(520).At(35, 29),
                Monster(520).At(36, 13)))
            .OnKills(4, r => r.Spawn(Monster(520).At(21, 12)))
            .OnKills(5, r => r.Spawn(Monster(520).At(20, 27), Monster(486).At(26, 8), Monster(486).At(37, 8), Monster(486).At(41, 14)))
            .OnCleared(r => r.Unlock(fourthGateDoors).Say(Game18NConstString.ADoorHasOpened).Say(Game18NConstString.QuickMissionComplete));

        // The three angels each hold a room; their followers leave once they fall.
        var jabinielRoomDoors = new[]
        {
            jabinielRoom.Portal(new(26, 7), israelRoom, new(26, 34)),
            jabinielRoom.Portal(new(26, 34), fourthGate, new(26, 7))
        };
        jabinielRoom.Spawn(
            Monster(520).At(36, 25),
            Monster(485).At(35, 15),
            Monster(520).At(29, 22),
            Monster(520).At(22, 22),
            Monster(485).At(15, 18),
            Monster(486).At(11, 12),
            Monster(517).At(25, 12).Bonus());
        jabinielRoom.ClearedAfter(1, 517);
        jabinielRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ANGEL_JABINIEL))
            .OnKills(3, r => r.Spawn(Monster(520).At(16, 26), Monster(520).At(30, 17), Monster(486).At(40, 11), Monster(486).At(40, 20)))
            .OnKills(4, r => r.Spawn(Monster(520).At(16, 10)))
            .OnKills(5, r => r.Spawn(
                Monster(486).At(11, 22),
                Monster(520).At(20, 18),
                Monster(485).At(21, 11),
                Monster(520).At(27, 28),
                Monster(485).At(29, 10),
                Monster(520).At(35, 9)))
            .OnKills(6, r => r.Spawn(Monster(485).At(18, 21)))
            .OnKills(7, r => r.Spawn(Monster(520).At(17, 14), Monster(520).At(32, 12), Monster(485).At(35, 20)))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(jabinielRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var israelRoomDoors = new[]
        {
            israelRoom.Portal(new(26, 7), reedwhanelRoom, new(26, 34)),
            israelRoom.Portal(new(26, 34), jabinielRoom, new(26, 7))
        };
        israelRoom.Spawn(
            Monster(520).At(38, 25).Bonus(),
            Monster(485).At(38, 13),
            Monster(520).At(37, 9),
            Monster(520).At(30, 14),
            Monster(520).At(23, 29),
            Monster(485).At(13, 13),
            Monster(518).At(25, 11));
        israelRoom.ClearedAfter(1, 518);
        israelRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ANGEL_ISRAEL))
            .OnKills(3, r => r.Spawn(
                Monster(520).At(9, 9),
                Monster(520).At(15, 18),
                Monster(520).At(15, 23),
                Monster(486).At(26, 28),
                Monster(485).At(31, 23),
                Monster(486).At(39, 21)))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(israelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var reedwhanelRoomDoors = new[]
        {
            reedwhanelRoom.Portal(new(26, 7), meeting, new(26, 34)),
            reedwhanelRoom.Portal(new(26, 34), israelRoom, new(26, 7))
        };
        reedwhanelRoom.Spawn(
            Monster(520).At(39, 17),
            Monster(520).At(34, 27),
            Monster(520).At(26, 22),
            Monster(520).At(18, 27),
            Monster(520).At(12, 20),
            Monster(519).At(27, 10).Bonus());
        reedwhanelRoom.ClearedAfter(1, 519);
        reedwhanelRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ANGEL_REEDWHANEL))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(reedwhanelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        meeting.Portal(new(43, 16), echekaruelRoom, new(6, 18), locked: false);
        meeting.Portal(new(26, 34), reedwhanelRoom, new(26, 7), locked: false);
        meeting.OnEnter(r => r.Dialog(7337));

        var echekaruelRoomDoors = new[]
        {
            echekaruelRoom.Portal(new(48, 18), exitRoom, new(6, 18)),
            echekaruelRoom.Portal(new(6, 18), meeting, new(43, 16))
        };
        echekaruelRoom.Spawn(
            Monster(520).At(40, 19),
            Monster(520).At(33, 13),
            Monster(520).At(30, 31),
            Monster(485).At(30, 7),
            Monster(520).At(20, 18).Bonus(),
            Monster(517).At(19, 9),
            Monster(485).At(18, 31),
            Monster(520).At(15, 13),
            Monster(511).At(41, 9));
        echekaruelRoom.ClearedAfter(1, 511);
        echekaruelRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ECHEKARUEL))
            .OnKills(2, r => r.Spawn(Monster(518).At(23, 28), Monster(486).At(25, 13), Monster(486).At(28, 23), Monster(485).At(36, 30)))
            .OnKills(3, r => r.Spawn(Monster(519).At(38, 11)))
            .OnCleared(r => r
                .Dismiss()
                .Unlock(echekaruelRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        exitRoom.Portal(new(6, 18), echekaruelRoom, new(48, 18), locked: false);
        exitRoom.Exit(new(48, 18), locked: false)
            .OnReached(r => r.Say(Game18NConstString.PleaseWait))
            .WinsAfterDialog(7338);
    }
}

return new TimeSpace407();
