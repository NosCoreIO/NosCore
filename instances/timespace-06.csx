public class TimeSpace6 : TimeSpaceScript
{
    public TimeSpace6() : base(6, "The Labyrinth")
    {
        Levels(15, 99);
        Start(11, 11);
        Requires(1012, 1);
        Lives(2);
        Clock(TimeSpan.FromSeconds(300));
        Draw(17, 1);
        Draw(716, 1);
        Special(2038, 5);
        Reward(2046, 1);

        var entrance = Room(4100, 0, 8);
        var mazeEntrance = Room(4101, 1, 8);
        var firstCorridor = Room(4102, 2, 8);
        var northCorridor = Room(4103, 3, 8);
        var northEast = Room(4103, 4, 8);
        var holdout = Room(4109, 4, 9);
        var middle = Room(4101, 3, 9);
        var keyRoom = Room(4100, 2, 9);
        var westCorridor = Room(4111, 1, 9);
        var westEnd = Room(4111, 0, 9);
        var bonusRoom = Room(4109, 0, 10);
        var southWest = Room(4106, 1, 10);
        var leverRoom = Room(4106, 2, 10);
        var way = Room(4105, 3, 10);
        var southEast = Room(4105, 4, 10);

        entrance.Portal(new(28, 15), mazeEntrance, new(1, 14), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        mazeEntrance.Portal(new(28, 15), firstCorridor, new(1, 14), locked: false);
        mazeEntrance.Portal(new(14, 28), westCorridor, new(14, 1), locked: false);
        mazeEntrance.Portal(new(1, 14), entrance, new(28, 15), locked: false);
        mazeEntrance.OnEnter(r => r.Say(LanguageKey.TIMESPACE_LABYRINTH_ENTRANCE));

        firstCorridor.Portal(new(28, 14), northCorridor, new(1, 15), locked: false);
        firstCorridor.Portal(new(14, 28), keyRoom, new(14, 1), locked: false);
        firstCorridor.Portal(new(1, 14), mazeEntrance, new(28, 15), locked: false);
        firstCorridor.Spawn(
            Monster(33).At(17, 15),
            Monster(33).At(14, 13).OnDeath(r => r.Spawn(Monster(63).At(21, 14).Bonus())),
            Monster(33).At(5, 14).OnDeath(r => r.Spawn(Monster(63).At(14, 19))));
        firstCorridor.OnEnter(r => r.Say(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING));

        northCorridor.Portal(new(28, 15), northEast, new(1, 15), locked: false);
        northCorridor.Portal(new(14, 28), middle, new(14, 1), locked: false);
        northCorridor.Portal(new(1, 15), firstCorridor, new(28, 14), locked: false);
        northCorridor.Spawn(
            Monster(64).At(20, 14),
            Monster(28).At(20, 15).OnDeath(r => r.Spawn(Monster(64).At(13, 11))),
            Monster(64).At(17, 20).OnDeath(r => r.Spawn(Monster(64).At(10, 12), Monster(64).At(12, 16))),
            Monster(64).At(15, 11),
            Monster(28).At(11, 18));
        northCorridor.OnEnter(r => r.Say(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING));

        northEast.Portal(new(14, 28), holdout, new(14, 1), locked: false);
        northEast.Portal(new(1, 15), northCorridor, new(28, 15), locked: false);

        var holdoutToNorthEast = holdout.Portal(new(14, 1), northEast, new(14, 28));
        var holdoutToSouthEast = holdout.Portal(new(14, 28), southEast, new(14, 1));
        var holdoutToMiddle = holdout.Portal(new(1, 15), middle, new(28, 15));
        holdout.Spawn(
            Monster(18).At(20, 19),
            Monster(130).At(12, 18),
            Monster(97).At(9, 12),
            Monster(18).At(8, 19));
        holdout.OnEnter(r => r
                .StartRoomClock(TimeSpan.FromSeconds(60))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_UNTIL_DOOR_OPENS))
            .OnClockOut(r => r
                .Dismiss(18, 97, 130)
                .Unlock(holdoutToNorthEast, holdoutToMiddle, holdoutToSouthEast)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_ALL_LOCKED_DOORS_OPENED));

        middle.Portal(new(14, 1), northCorridor, new(14, 28), locked: false);
        middle.Portal(new(28, 15), holdout, new(1, 15), locked: false);
        middle.Portal(new(1, 14), keyRoom, new(28, 15), locked: false);
        middle.OnEnter(r => r.Say(LanguageKey.TIMESPACE_CAN_YOU_SEE_THE_EXIT));

        keyRoom.Portal(new(14, 1), firstCorridor, new(14, 28), locked: false);
        keyRoom.Portal(new(28, 15), middle, new(1, 14), locked: false);
        keyRoom.Portal(new(14, 28), leverRoom, new(14, 1), locked: false);
        keyRoom.Portal(new(1, 15), westCorridor, new(28, 15), locked: false);
        keyRoom.KeyAnywhere(1057)
            .OnUse(r => r
                .AddTime(TimeSpan.FromSeconds(91))
                .Say(Game18NConstString.DoorOpenedSomewhere)
                .Say(Game18NConstString.FindKey));
        keyRoom.Spawn(
            Monster(130).At(15, 20).OnDeath(r => r.Spawn(
                Monster(130).At(18, 19).OnDeath(r => r.Spawn(Monster(130).At(20, 12))),
                Monster(130).At(20, 18))),
            Monster(130).At(12, 15).Bonus());
        keyRoom.OnEnter(r => r.Say(LanguageKey.TIMESPACE_FIND_KEY_FOR_BONUS_TIME));

        westCorridor.Portal(new(14, 1), mazeEntrance, new(14, 28), locked: false);
        westCorridor.Portal(new(28, 15), keyRoom, new(1, 15), locked: false);
        westCorridor.Portal(new(14, 28), southWest, new(14, 1));
        westCorridor.Portal(new(1, 15), westEnd, new(28, 15), locked: false);
        westCorridor.Spawn(
            Monster(98).At(23, 13).OnDeath(r => r.Spawn(Monster(63).At(10, 18), Monster(63).At(21, 19))),
            Monster(98).At(13, 11),
            Monster(98).At(11, 17).Bonus());

        westEnd.Portal(new(28, 15), westCorridor, new(1, 15), locked: false);
        westEnd.Portal(new(14, 28), bonusRoom, new(14, 1), locked: false);
        westEnd.Spawn(
            Monster(19).At(25, 13),
            Monster(57).At(22, 15),
            Monster(19).At(22, 10),
            Monster(57).At(26, 12),
            Monster(19).At(19, 7));
        westEnd.OnEnter(r => r.Say(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING));

        var bonusToWestEnd = bonusRoom.Portal(new(14, 1), westEnd, new(14, 28));
        var bonusToSouthWest = bonusRoom.Portal(new(28, 15), southWest, new(1, 15));
        bonusRoom.Spawn(
            Monster(10).At(12, 19).Bonus(),
            Monster(10).At(10, 11).OnDeath(r => r.Spawn(
                Monster(98).At(9, 14).OnDeath(r => r.Spawn(Monster(98).At(15, 8), Monster(10).At(19, 19))))));
        bonusRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ENEMIES_FOR_BONUS_TIME))
            .OnCleared(r => r
                .Unlock(bonusToSouthWest, bonusToWestEnd)
                .AddTime(TimeSpan.FromSeconds(150))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_BONUS_TIME_ROOM)
                .Say(LanguageKey.TIMESPACE_BONUS_TIME_EARNED, 150 * r.ClockScale));

        southWest.Portal(new(14, 1), westCorridor, new(14, 28), locked: false);
        southWest.Portal(new(28, 15), leverRoom, new(1, 15), locked: false);
        southWest.Portal(new(1, 15), bonusRoom, new(28, 15), locked: false);
        southWest.Spawn(Monster(10).At(18, 16).OnDeath(r => r.Spawn(
            Monster(55).At(8, 15),
            Monster(55).At(10, 11),
            Monster(55).At(13, 14),
            Monster(55).At(13, 19),
            Monster(55).At(14, 8),
            Monster(55).At(18, 11),
            Monster(55).At(20, 15),
            Monster(55).At(23, 16))));
        southWest.OnEnter(r => r.Say(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING));

        leverRoom.Portal(new(14, 1), keyRoom, new(14, 28), locked: false);
        var leverToWay = leverRoom.Portal(new(28, 15), way, new(1, 14));
        leverRoom.Portal(new(1, 15), southWest, new(28, 15), locked: false);
        leverRoom.Lever(new(14, 15))
            .OnUse(r => r
                .AddTime(TimeSpan.FromSeconds(-60))
                .Unlock(leverToWay)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_LEVER_TOO_DANGEROUS));
        leverRoom.Spawn(
            Monster(27).At(10, 20),
            Monster(27).At(10, 11).OnDeath(r => r.Spawn(Monster(0).At(18, 16))));
        leverRoom.OnEnter(r => r.Say(LanguageKey.TIMESPACE_LEVER_TOO_DANGEROUS));

        way.Portal(new(28, 15), southEast, new(1, 14), locked: false);
        var exit = way.Exit(new(14, 28));
        way.Portal(new(1, 14), leverRoom, new(28, 15), locked: false);
        way.Lever(new(15, 15))
            .OnUse(r => r
                .Dialog(6028)
                .Spawn(Monster(33).At(16, 10).Talk(6028).OnDeath(r => r.Spawn(
                    Monster(27).At(11, 17).OnDeath(r => r.Spawn(
                        Monster(55).At(21, 18).OnDeath(r => r.Spawn(
                            Monster(55).At(12, 9).OnDeath(r => r.Spawn(Monster(27).At(16, 19))))))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES));
        way.OnEnter(r => r.Say(LanguageKey.TIMESPACE_ROOM_WITH_A_WAY_OUT))
            .OnCleared(r => r
                .Unlock(exit)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6029));

        southEast.Portal(new(14, 1), holdout, new(14, 28), locked: false);
        southEast.Portal(new(1, 14), way, new(28, 15), locked: false);
        southEast.Spawn(
            Monster(101).At(12, 17).OnDeath(r => r.Spawn(
                Monster(130).At(13, 16).OnDeath(r => r.Spawn(Monster(101).At(20, 16))),
                Monster(130).At(16, 14).OnDeath(r => r.Spawn(Monster(101).At(18, 10))))));
        southEast.OnEnter(r => r.Say(LanguageKey.TIMESPACE_NO_SHAME_IN_FLEEING));
    }
}

return new TimeSpace6();
