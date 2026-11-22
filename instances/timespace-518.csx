public class TimeSpace518 : TimeSpaceScript
{
    public TimeSpace518() : base(518, "What Adventure Awaits?")
    {
        Levels(27, 99);
        Start(13, 9);
        Lives(1);
        Clock(TimeSpan.FromSeconds(600));

        var entrance = Room(4202, 0, 7);
        var hall = Room(4200, 1, 7);
        var northWing = Room(4208, 1, 6);
        var mimicRoom = Room(4213, 2, 6);
        var northPassage = Room(4213, 3, 6);
        var emptyPassage = Room(4213, 4, 6);
        var northCorner = Room(4209, 5, 6);
        var eastPassage = Room(4214, 5, 7);
        var bonusRoom = Room(4200, 4, 7);
        var crossing = Room(4200, 4, 8);
        var southCentre = Room(4200, 3, 8);
        var demonRoom = Room(4200, 2, 8);
        var westWing = Room(4212, 1, 8);
        var westCentre = Room(4200, 2, 7);
        var centre = Room(4200, 3, 7);
        var eastWing = Room(4214, 5, 8);
        var huntRoom = Room(4210, 5, 9);
        var exitRoom = Room(4200, 4, 9);
        var trap = Room(4211, 3, 9);
        var impRoom = Room(4211, 2, 9);
        var leverRoom = Room(4207, 1, 9);

        entrance.Portal(new(28, 15), hall, new(1, 15), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        hall.Portal(new(14, 1), northWing, new(14, 28), locked: false);
        hall.Portal(new(28, 15), westCentre, new(1, 15), locked: false);
        hall.Portal(new(14, 28), westWing, new(14, 1), locked: false);
        hall.Portal(new(1, 15), entrance, new(28, 15), locked: false);

        northWing.Portal(new(28, 15), mimicRoom, new(1, 15), locked: false);
        northWing.Portal(new(14, 28), hall, new(14, 1), locked: false);

        var mimicRoomEast = mimicRoom.Portal(new(28, 15), northPassage, new(1, 15));
        var mimicRoomSouth = mimicRoom.Portal(new(14, 28), westCentre, new(14, 1));
        var mimicRoomWest = mimicRoom.Portal(new(1, 15), northWing, new(28, 15));
        mimicRoom.Spawn(
            Monster(140).At(21, 18),
            Monster(57).At(19, 14),
            Monster(140).At(16, 19),
            Monster(57).At(16, 11),
            Monster(140).At(14, 14),
            Monster(58).At(11, 17).Bonus(),
            Monster(58).At(7, 13));
        mimicRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_PORTAL_OPENS_ONCE_ALL_DEFEATED))
            .OnCleared(r => r
                .Unlock(mimicRoomEast, mimicRoomSouth, mimicRoomWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXIT_IS_OPEN));

        northPassage.Portal(new(28, 15), emptyPassage, new(1, 15), locked: false);
        northPassage.Portal(new(14, 28), centre, new(14, 1), locked: false);
        northPassage.Portal(new(1, 15), mimicRoom, new(28, 15), locked: false);
        northPassage.Spawn(
            Monster(99).At(20, 18).Bonus(),
            Monster(13).At(20, 14),
            Monster(99).At(16, 11).Bonus(),
            Monster(141).At(11, 18),
            Monster(13).At(14, 12));

        emptyPassage.Portal(new(28, 15), northCorner, new(1, 15), locked: false);
        emptyPassage.Portal(new(14, 28), bonusRoom, new(14, 1), locked: false);
        emptyPassage.Portal(new(1, 15), northPassage, new(28, 15), locked: false);

        var northCornerSouth = northCorner.Portal(new(14, 28), eastPassage, new(14, 1));
        var northCornerWest = northCorner.Portal(new(1, 15), emptyPassage, new(28, 15));
        northCorner.Spawn(
            Monster(58).At(18, 18),
            Monster(1).At(16, 15).Bonus(),
            Monster(58).At(15, 13).Bonus(),
            Monster(58).At(13, 16),
            Monster(136).At(12, 19).Bonus(),
            Monster(136).At(9, 13));
        northCorner.Spawn(Item(1042).At(20, 6));
        northCorner.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_PORTAL_OPENS_ONCE_ALL_DEFEATED))
            .OnCleared(r => r
                .Unlock(northCornerSouth, northCornerWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXIT_IS_OPEN));

        eastPassage.Portal(new(14, 1), northCorner, new(14, 28), locked: false);
        eastPassage.Portal(new(14, 28), eastWing, new(14, 1), locked: false);
        eastPassage.Portal(new(1, 15), bonusRoom, new(28, 15), locked: false);

        var bonusRoomNorth = bonusRoom.Portal(new(14, 1), emptyPassage, new(14, 28));
        bonusRoom.Portal(new(28, 15), eastPassage, new(1, 15));
        var bonusRoomSouth = bonusRoom.Portal(new(14, 28), crossing, new(14, 1));
        bonusRoom.Portal(new(1, 15), centre, new(28, 15));
        bonusRoom.Spawn(
            Monster(99).At(20, 16),
            Monster(66).At(17, 10),
            Monster(66).At(17, 18),
            Monster(20).At(16, 15),
            Monster(140).At(15, 10).Bonus(),
            Monster(136).At(13, 20).Bonus(),
            Monster(136).At(11, 13));
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

        crossing.Portal(new(14, 1), bonusRoom, new(14, 28), locked: false);
        crossing.Portal(new(28, 15), eastWing, new(1, 15), locked: false);
        crossing.Portal(new(14, 28), exitRoom, new(14, 1), locked: false);
        crossing.Portal(new(1, 15), southCentre, new(28, 15), locked: false);
        crossing.Spawn(
            Monster(99).At(17, 15),
            Monster(13).At(18, 16),
            Monster(99).At(17, 13),
            Monster(141).At(15, 20),
            Monster(13).At(13, 15).Bonus());

        southCentre.Portal(new(14, 1), centre, new(14, 28), locked: false);
        southCentre.Portal(new(28, 15), crossing, new(1, 15), locked: false);
        southCentre.Portal(new(14, 28), trap, new(14, 1), locked: false);
        southCentre.Portal(new(1, 15), demonRoom, new(28, 15), locked: false);
        southCentre.Spawn(
            Monster(99).At(17, 18),
            Monster(13).At(16, 15),
            Monster(99).At(16, 11).Bonus(),
            Monster(141).At(13, 19),
            Monster(13).At(13, 15).Bonus());

        var demonRoomDoors = new[]
        {
            demonRoom.Portal(new(14, 1), westCentre, new(14, 28)),
            demonRoom.Portal(new(28, 15), southCentre, new(1, 15)),
            demonRoom.Portal(new(14, 28), impRoom, new(14, 1)),
            demonRoom.Portal(new(1, 15), westWing, new(28, 15))
        };
        demonRoom.Spawn(
            Monster(66).At(20, 13),
            Monster(66).At(21, 17),
            Monster(66).At(14, 20).Bonus(),
            Monster(5).At(11, 14).Bonus());
        demonRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_DEMONS_TO_OPEN_EXIT))
            .OnCleared(r => r
                .Unlock(demonRoomDoors)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_EXITS_ALL_AROUND_OPEN));

        westWing.Portal(new(14, 1), hall, new(14, 28), locked: false);
        westWing.Portal(new(28, 15), demonRoom, new(1, 15), locked: false);
        westWing.Portal(new(14, 28), leverRoom, new(14, 1), locked: false);
        westWing.Spawn(
            Monster(99).At(16, 14),
            Monster(13).At(18, 16),
            Monster(99).At(14, 12).Bonus(),
            Monster(141).At(13, 19),
            Monster(13).At(12, 14).Bonus());

        westCentre.Portal(new(14, 1), mimicRoom, new(14, 28), locked: false);
        westCentre.Portal(new(28, 15), centre, new(1, 15), locked: false);
        westCentre.Portal(new(14, 28), demonRoom, new(14, 1), locked: false);
        westCentre.Portal(new(1, 15), hall, new(28, 15), locked: false);
        westCentre.Spawn(
            Monster(100).At(19, 16),
            Monster(58).At(21, 10),
            Monster(12).At(18, 17),
            Monster(140).At(15, 18),
            Monster(140).At(11, 14),
            Monster(12).At(11, 16),
            Monster(58).At(11, 11).Bonus());

        centre.Portal(new(14, 1), northPassage, new(14, 28), locked: false);
        centre.Portal(new(28, 15), bonusRoom, new(1, 15), locked: false);
        centre.Portal(new(14, 28), southCentre, new(14, 1), locked: false);
        centre.Portal(new(1, 15), westCentre, new(28, 15), locked: false);
        centre.Spawn(
            Monster(13).At(23, 10).Bonus(),
            Monster(58).At(18, 12).Bonus(),
            Monster(99).At(18, 20),
            Monster(140).At(16, 11),
            Monster(65).At(15, 18).Bonus(),
            Monster(99).At(10, 13));

        eastWing.Portal(new(14, 1), eastPassage, new(14, 28), locked: false);
        eastWing.Portal(new(14, 28), huntRoom, new(15, 1), locked: false);
        eastWing.Portal(new(1, 15), crossing, new(28, 15), locked: false);
        eastWing.Spawn(
            Monster(99).At(17, 16),
            Monster(13).At(19, 15),
            Monster(99).At(18, 10).Bonus(),
            Monster(141).At(12, 21),
            Monster(13).At(14, 16).Bonus());

        var huntRoomNorth = huntRoom.Portal(new(15, 1), eastWing, new(14, 28));
        huntRoom.Portal(new(1, 15), exitRoom, new(28, 15));
        huntRoom.Spawn(
            Monster(136).At(20, 17).Bonus(),
            Monster(136).At(18, 10),
            Monster(140).At(17, 19).Bonus(),
            Monster(136).At(12, 19),
            Monster(140).At(9, 12));
        huntRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(huntRoomNorth)
                .Say(Game18NConstString.ADoorHasOpened)
                .AddTime(TimeSpan.FromSeconds(120))
                .Say(Game18NConstString.QuickMissionComplete));

        exitRoom.Portal(new(14, 1), crossing, new(14, 28), locked: false);
        exitRoom.Portal(new(28, 15), huntRoom, new(1, 15), locked: false);
        exitRoom.Portal(new(1, 15), trap, new(29, 15), locked: false);
        exitRoom.Exit(new(14, 28), locked: false);

        var trapNorth = trap.Portal(new(14, 1), southCentre, new(14, 28));
        var trapEast = trap.Portal(new(29, 15), exitRoom, new(1, 15));
        var trapWest = trap.Portal(new(1, 15), impRoom, new(29, 15));
        trap.Spawn(
            Monster(57).At(18, 12),
            Monster(57).At(17, 17),
            Monster(57).At(13, 9).Bonus(),
            Monster(57).At(10, 16),
            Monster(271).At(13, 13));
        trap.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_DARK_TRAP_ONE_MINUTE)
                .StartRoomClock(TimeSpan.FromSeconds(60)))
            .OnClockOut(r => r
                .Unlock(trapNorth, trapEast, trapWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_PORTAL_HAS_OPENED));

        impRoom.Portal(new(14, 1), demonRoom, new(14, 28), locked: false);
        impRoom.Portal(new(29, 15), trap, new(1, 15), locked: false);
        impRoom.Portal(new(1, 15), leverRoom, new(28, 15), locked: false);

        var leverRoomNorth = leverRoom.Portal(new(14, 1), westWing, new(14, 28));
        var leverRoomEast = leverRoom.Portal(new(28, 15), impRoom, new(1, 15));
        leverRoom.Spawn(
            Monster(141).At(20, 16),
            Monster(136).At(19, 12),
            Monster(58).At(16, 9).Bonus(),
            Monster(136).At(11, 11),
            Monster(141).At(10, 17));
        leverRoom.OnEnter(r => r.Say(LanguageKey.TIMESPACE_SEE_LEVER_IN_CENTRE));
        leverRoom.Lever(new(15, 16))
            .OnUse(r => r.Unlock(leverRoomNorth, leverRoomEast).Say(Game18NConstString.ADoorHasOpened));
    }
}

return new TimeSpace518();
