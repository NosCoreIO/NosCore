public class TimeSpace34 : TimeSpaceScript
{
    public TimeSpace34() : base(34, "La salle du trésor")
    {
        Levels(44, 99);
        Start(14, 10);
        Requires(1012, 4);
        Lives(2);
        Clock(TimeSpan.FromSeconds(300));
        Objective(ObjectiveKind.CollectItem, 1059, 1);
        Draw(524, 1);
        Draw(1014, 10);
        Special(1011, 50);
        Reward(2127, 1);
        Reward(1087, 1);

        var entrance = Room(4202, 0, 7);
        var hall = Room(4200, 1, 7);
        var northWing = Room(4208, 1, 6);
        var boxRoom = Room(4213, 2, 6);
        var northPassage = Room(4213, 3, 6);
        var thornRoom = Room(4213, 4, 6);
        var northCorner = Room(4209, 5, 6);
        var eastPassage = Room(4214, 5, 7);
        var bonusRoom = Room(4200, 4, 7);
        var vampieRoom = Room(4200, 4, 8);
        var crossing = Room(4200, 3, 8);
        var demonRoom = Room(4200, 2, 8);
        var westWing = Room(4212, 1, 8);
        var leverRoom = Room(4207, 1, 9);
        var impRoom = Room(4211, 2, 9);
        var trap = Room(4211, 3, 9);
        var mimicRoom = Room(4211, 4, 9);
        var timeRoom = Room(4210, 5, 9);
        var eastWing = Room(4214, 5, 8);
        var centre = Room(4200, 3, 7);
        var westCentre = Room(4200, 2, 7);

        entrance.Portal(new(28, 15), hall, new(1, 15), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        hall.Portal(new(14, 1), northWing, new(14, 28), locked: false);
        hall.Portal(new(28, 15), westCentre, new(1, 15), locked: false);
        hall.Portal(new(14, 28), westWing, new(14, 1), locked: false);
        hall.Portal(new(1, 15), entrance, new(28, 15), locked: false);

        northWing.Portal(new(28, 15), boxRoom, new(1, 15), locked: false);
        northWing.Portal(new(14, 28), hall, new(14, 1), locked: false);
        northWing.Spawn(
            Monster(144).At(21, 13).OnDeath(r => r.Spawn(
                Monster(82).At(15, 9).Bonus(),
                Monster(144).At(15, 18).OnDeath(r => r.Spawn(
                    Monster(82).At(13, 12))))),
            Monster(82).At(16, 23),
            Monster(144).At(17, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(82).At(9, 17))));

        var boxRoomEast = boxRoom.Portal(new(28, 15), northPassage, new(1, 15));
        var boxRoomSouth = boxRoom.Portal(new(14, 28), westCentre, new(14, 1));
        var boxRoomWest = boxRoom.Portal(new(1, 15), northWing, new(28, 15));
        boxRoom.Spawn(
            Monster(59).At(20, 15).Bonus().OnDeath(r => r.Spawn(
                Monster(67).At(5, 14).OnDeath(r => r.Spawn(
                    Monster(144).At(11, 12).OnDeath(r => r.Spawn(
                        Monster(144).At(15, 8).Bonus())),
                    Monster(144).At(14, 17).OnDeath(r => r.Spawn(
                        Monster(144).At(20, 13).Bonus())))),
                Monster(67).At(17, 14).Bonus())),
            Monster(59).At(18, 11).Bonus(),
            Monster(59).At(11, 18).Bonus(),
            Monster(59).At(11, 8).Bonus().OnDeath(r => r.Spawn(
                Monster(67).At(20, 20).Bonus())));
        boxRoom.Spawn(Item(1001).At(7, 19), Item(1001).At(6, 18));
        boxRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_PORTAL_OPENS_ONCE_ALL_DEFEATED))
            .OnCleared(r => r
                .Unlock(boxRoomEast, boxRoomSouth, boxRoomWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXIT_IS_OPEN));

        northPassage.Portal(new(28, 15), thornRoom, new(1, 15), locked: false);
        northPassage.Portal(new(14, 28), centre, new(14, 1), locked: false);
        northPassage.Portal(new(1, 15), boxRoom, new(28, 15), locked: false);

        thornRoom.Portal(new(28, 15), northCorner, new(1, 15), locked: false);
        thornRoom.Portal(new(14, 28), bonusRoom, new(14, 1), locked: false);
        thornRoom.Portal(new(1, 15), northPassage, new(28, 15), locked: false);
        thornRoom.Spawn(
            Monster(67).At(17, 16).Bonus(),
            Monster(67).At(19, 8),
            Monster(67).At(16, 19).Bonus(),
            Monster(67).At(18, 10),
            Monster(67).At(11, 9),
            Monster(67).At(9, 20),
            Monster(67).At(7, 13),
            Monster(67).At(7, 15),
            Monster(67).At(6, 11));

        var northCornerSouth = northCorner.Portal(new(14, 28), eastPassage, new(14, 1));
        var northCornerWest = northCorner.Portal(new(1, 15), thornRoom, new(28, 15));
        northCorner.Spawn(
            Monster(22).At(17, 14).Bonus(),
            Monster(22).At(16, 14),
            Monster(143).At(12, 19).OnDeath(r => r.Spawn(
                Monster(139).At(10, 18).OnDeath(r => r.Spawn(
                    Monster(139).At(14, 8),
                    Monster(139).At(22, 11).Bonus())),
                Monster(143).At(16, 12))),
            Monster(22).At(13, 14).Bonus().OnDeath(r => r.Spawn(
                Monster(139).At(19, 18))),
            Monster(22).At(12, 14),
            Monster(143).At(7, 13));
        northCorner.Spawn(Item(1042).At(20, 6));
        northCorner.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_PORTAL_OPENS_ONCE_ALL_DEFEATED))
            .OnCleared(r => r
                .Unlock(northCornerSouth, northCornerWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXIT_IS_OPEN));

        eastPassage.Portal(new(14, 1), northCorner, new(14, 28), locked: false);
        eastPassage.Portal(new(14, 28), eastWing, new(14, 1), locked: false);
        eastPassage.Portal(new(1, 15), bonusRoom, new(28, 15), locked: false);

        var bonusRoomNorth = bonusRoom.Portal(new(14, 1), thornRoom, new(14, 28));
        bonusRoom.Portal(new(28, 15), eastPassage, new(1, 15));
        var bonusRoomSouth = bonusRoom.Portal(new(14, 28), vampieRoom, new(14, 1));
        bonusRoom.Portal(new(1, 15), centre, new(28, 15));
        bonusRoom.Spawn(
            Monster(142).At(21, 20),
            Monster(142).At(19, 11).OnDeath(r => r.Spawn(
                Monster(60).At(23, 19).OnDeath(r => r.Spawn(
                    Monster(60).At(12, 8).Bonus())))),
            Monster(142).At(12, 22).OnDeath(r => r.Spawn(
                Monster(60).At(11, 17).OnDeath(r => r.Spawn(
                    Monster(83).At(8, 15),
                    Monster(83).At(16, 19).OnDeath(r => r.Spawn(
                        Monster(83).At(22, 14))))),
                Monster(60).At(13, 14))),
            Monster(142).At(9, 11).Bonus(),
            Monster(142).At(4, 17).OnDeath(r => r.Spawn(
                Monster(83).At(16, 9).Bonus(),
                Monster(73).At(16, 14))));
        bonusRoom.Spawn(Item(1001).At(21, 22));
        bonusRoom.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_IN_TWO_MINUTES_FOR_BONUS)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .OnCleared(r =>
            {
                r.Unlock(bonusRoomNorth, bonusRoomSouth).Say(Game18NConstString.ADoorHasOpened);
                if (r.IsClockRunning)
                {
                    r.AddTime(TimeSpan.FromMinutes(3)).Say(LanguageKey.TIMESPACE_BONUS_THREE_MINUTES);
                }
            });

        vampieRoom.Portal(new(14, 1), bonusRoom, new(14, 28), locked: false);
        vampieRoom.Portal(new(28, 15), eastWing, new(1, 15), locked: false);
        vampieRoom.Portal(new(14, 28), mimicRoom, new(14, 1), locked: false);
        vampieRoom.Portal(new(1, 15), crossing, new(28, 15), locked: false);
        vampieRoom.Spawn(
            Monster(59).At(24, 15).Bonus(),
            Monster(59).At(17, 16).Bonus(),
            Monster(59).At(16, 11),
            Monster(59).At(14, 6).Bonus(),
            Monster(59).At(12, 16),
            Monster(59).At(7, 13));

        crossing.Portal(new(14, 1), centre, new(14, 28), locked: false);
        crossing.Portal(new(28, 15), vampieRoom, new(1, 15), locked: false);
        crossing.Portal(new(14, 28), trap, new(14, 1), locked: false);
        crossing.Portal(new(1, 15), demonRoom, new(28, 15), locked: false);

        var demonRoomDoors = new[]
        {
            demonRoom.Portal(new(14, 1), westCentre, new(14, 28)),
            demonRoom.Portal(new(28, 15), crossing, new(1, 15)),
            demonRoom.Portal(new(14, 28), impRoom, new(14, 1)),
            demonRoom.Portal(new(1, 15), westWing, new(28, 15))
        };
        demonRoom.Spawn(
            Monster(60).At(15, 23).OnDeath(r => r.Spawn(
                Monster(60).At(23, 17).OnDeath(r => r.Spawn(
                    Monster(60).At(22, 13).Bonus().OnDeath(r => r.Spawn(
                        Monster(84).At(11, 9).Bonus(),
                        Monster(84).At(16, 5).OnDeath(r => r.Spawn(
                            Monster(103).At(19, 18).Bonus().OnDeath(r => r.Spawn(
                                Monster(103).At(10, 17).Bonus(),
                                Monster(73).At(16, 13))))))))))),
            Monster(60).At(4, 16).OnDeath(r => r.Spawn(
                Monster(84).At(17, 10))),
            Monster(60).At(4, 12).Bonus());
        demonRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_DEMONS_TO_OPEN_EXIT))
            .OnCleared(r => r
                .Unlock(demonRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXITS_ALL_AROUND_OPEN));

        westWing.Portal(new(14, 1), hall, new(14, 28), locked: false);
        westWing.Portal(new(28, 15), demonRoom, new(1, 15), locked: false);
        westWing.Portal(new(14, 28), leverRoom, new(14, 1), locked: false);
        westWing.Spawn(
            Monster(144).At(21, 10).OnDeath(r => r.Spawn(
                Monster(144).At(12, 18).OnDeath(r => r.Spawn(
                    Monster(144).At(17, 14))))),
            Monster(144).At(19, 18).Bonus().OnDeath(r => r.Spawn(
                Monster(144).At(5, 15).Bonus())),
            Monster(144).At(12, 9).Bonus());

        var leverRoomNorth = leverRoom.Portal(new(14, 1), westWing, new(14, 28));
        var leverRoomEast = leverRoom.Portal(new(28, 15), impRoom, new(1, 15));
        leverRoom.Spawn(
            Monster(140).At(22, 12),
            Monster(140).At(21, 19),
            Monster(140).At(21, 14).Bonus(),
            Monster(140).At(20, 10),
            Monster(140).At(18, 18).Bonus(),
            Monster(140).At(16, 8),
            Monster(140).At(15, 20),
            Monster(140).At(13, 10),
            Monster(140).At(12, 19),
            Monster(140).At(12, 13).Bonus(),
            Monster(140).At(10, 14));
        leverRoom.Spawn(Item(1042).At(5, 13), Item(1042).At(4, 17));
        leverRoom.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEE_LEVER_IN_CENTRE));
        leverRoom.Lever(new(15, 16))
            .OnUse(r => r.Unlock(leverRoomNorth, leverRoomEast).Say(Game18NConstString.ADoorHasOpened));

        impRoom.Portal(new(14, 1), demonRoom, new(14, 28), locked: false);
        impRoom.Portal(new(29, 15), trap, new(1, 15), locked: false);
        impRoom.Portal(new(1, 15), leverRoom, new(28, 15), locked: false);
        impRoom.Spawn(
            Monster(73).At(19, 18).Bonus(),
            Monster(73).At(15, 11),
            Monster(73).At(6, 14));

        var trapNorth = trap.Portal(new(14, 1), crossing, new(14, 28));
        var trapEast = trap.Portal(new(29, 15), mimicRoom, new(1, 15));
        var trapWest = trap.Portal(new(1, 15), impRoom, new(29, 15));
        trap.Spawn(Monster(271).At(13, 13).Bonus());
        trap.Spawn(Item(1059).At(19, 11));
        trap.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_DARK_TRAP_ONE_MINUTE)
                .StartRoomClock(TimeSpan.FromSeconds(60)))
            .OnClockOut(r => r
                .Unlock(trapNorth, trapEast, trapWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_PORTAL_HAS_OPENED))
            .OnItemCollected(1059, r => r.Succeed());

        mimicRoom.Portal(new(14, 1), vampieRoom, new(14, 28), locked: false);
        mimicRoom.Portal(new(29, 15), timeRoom, new(1, 15), locked: false);
        mimicRoom.Portal(new(1, 15), trap, new(29, 15), locked: false);
        mimicRoom.Spawn(
            Monster(142).At(18, 18).Bonus().OnDeath(r => r.Spawn(
                Monster(82).At(13, 18))),
            Monster(142).At(20, 14).Bonus().OnDeath(r => r.Spawn(
                Monster(82).At(12, 13).Bonus(),
                Monster(82).At(14, 7).Bonus().OnDeath(r => r.Spawn(
                    Monster(82).At(7, 19))))),
            Monster(142).At(15, 12));

        var timeRoomNorth = timeRoom.Portal(new(15, 1), eastWing, new(14, 28));
        timeRoom.Portal(new(1, 15), mimicRoom, new(29, 15));
        timeRoom.Spawn(
            Monster(83).At(21, 9).Bonus(),
            Monster(139).At(11, 19).OnDeath(r => r.Spawn(
                Monster(139).At(19, 17).Bonus())),
            Monster(144).At(10, 14).Bonus(),
            Monster(83).At(8, 11).OnDeath(r => r.Spawn(
                Monster(144).At(15, 11).OnDeath(r => r.Spawn(
                    Monster(83).At(14, 7).Bonus())))));
        timeRoom.Spawn(Item(1042).At(20, 21));
        timeRoom.OnEnter(r => r
            .Unlock(timeRoomNorth)
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(LanguageKey.TIMESPACE_TIME_ROOM_TWO_MINUTES));

        eastWing.Portal(new(14, 1), eastPassage, new(14, 28), locked: false);
        eastWing.Portal(new(14, 28), timeRoom, new(15, 1), locked: false);
        eastWing.Portal(new(1, 15), vampieRoom, new(28, 15), locked: false);
        eastWing.Spawn(
            Monster(144).At(21, 15).OnDeath(r => r.Spawn(
                Monster(73).At(15, 12).OnDeath(r => r.Spawn(
                    Monster(73).At(10, 15).Bonus())),
                Monster(73).At(18, 10).Bonus())),
            Monster(144).At(21, 17),
            Monster(144).At(15, 18),
            Monster(144).At(14, 7).Bonus());

        centre.Portal(new(14, 1), northPassage, new(14, 28), locked: false);
        centre.Portal(new(28, 15), bonusRoom, new(1, 15), locked: false);
        centre.Portal(new(14, 28), crossing, new(14, 1), locked: false);
        centre.Portal(new(1, 15), westCentre, new(28, 15), locked: false);
        centre.Spawn(
            Monster(144).At(19, 13),
            Monster(144).At(17, 24).Bonus().OnDeath(r => r.Spawn(
                Monster(144).At(11, 17).Bonus().OnDeath(r => r.Spawn(
                    Monster(144).At(19, 7).Bonus())))),
            Monster(144).At(10, 12));

        westCentre.Portal(new(14, 1), boxRoom, new(14, 28), locked: false);
        westCentre.Portal(new(28, 15), centre, new(1, 15), locked: false);
        westCentre.Portal(new(14, 28), demonRoom, new(14, 1), locked: false);
        westCentre.Portal(new(1, 15), hall, new(28, 15), locked: false);
        westCentre.Spawn(
            Monster(138).At(18, 13),
            Monster(144).At(18, 8).Bonus().OnDeath(r => r.Spawn(
                Monster(144).At(13, 20).Bonus())),
            Monster(138).At(14, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(144).At(10, 15).Bonus(),
                Monster(138).At(16, 18))));
    }
}

return new TimeSpace34();
