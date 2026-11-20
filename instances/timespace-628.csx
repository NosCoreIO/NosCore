public class TimeSpace628 : TimeSpaceScript
{
    public TimeSpace628() : base(628, "L'espion koaren")
    {
        Levels(44, 99);
        Start(9, 11);
        Requires(1012, 1);
        Lives(3);
        Clock(TimeSpan.FromSeconds(500));

        var entrance = Room(4414, 0, 8);
        var passage = Room(4409, 1, 8);
        var firstFight = Room(4405, 2, 8);
        var crossing = Room(4402, 2, 9);
        var junction = Room(4403, 2, 10);
        var secondFight = Room(4409, 3, 10);
        var leverRoom = Room(4416, 4, 10);
        var siege = Room(4414, 1, 10);
        var corridor = Room(4406, 3, 9);
        var guardRoom = Room(4410, 3, 8);
        var thirdFight = Room(4408, 3, 7);
        var hideout = Room(4411, 4, 7);

        entrance.Portal(new(23, 12), passage, new(1, 11), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        passage.Portal(new(23, 11), firstFight, new(1, 12), locked: false);
        passage.Portal(new(1, 11), entrance, new(23, 12), locked: false);

        var firstFightOut = firstFight.Portal(new(12, 23), crossing, new(12, 1));
        var firstFightBack = firstFight.Portal(new(1, 12), passage, new(23, 11));
        firstFight.Spawn(
            Monster(145).At(15, 12).Bonus().OnDeath(r => r.Spawn(
                Monster(151).At(10, 13).Bonus(),
                Monster(145).At(15, 17).Bonus())),
            Monster(145).At(8, 9).Bonus());
        firstFight.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstFightBack, firstFightOut)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        crossing.Portal(new(12, 1), firstFight, new(12, 23), locked: false);
        var crossingEast = crossing.Portal(new(23, 12), corridor, new(1, 12));
        crossing.Portal(new(12, 23), junction, new(12, 1), locked: false);

        junction.Portal(new(12, 1), crossing, new(12, 23), locked: false);
        junction.Portal(new(23, 12), secondFight, new(1, 11), locked: false);
        var junctionWest = junction.Portal(new(1, 12), siege, new(23, 12));

        var secondFightOut = secondFight.Portal(new(23, 11), leverRoom, new(1, 12));
        var secondFightBack = secondFight.Portal(new(1, 11), junction, new(23, 12));
        secondFight.Spawn(
            Monster(146).At(11, 14).Bonus(),
            Monster(146).At(11, 8).Bonus().OnDeath(r => r.Spawn(
                Monster(149).At(16, 11).Bonus())));
        secondFight.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondFightOut, secondFightBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var leverRoomBack = leverRoom.Portal(new(1, 12), secondFight, new(23, 11));
        leverRoom.Lever(new(18, 12))
            .OnUse(r => r
                .Spawn(
                    Monster(146).At(15, 16).Bonus().OnDeath(r => r.Spawn(
                        Monster(153).At(12, 12).Bonus(),
                        Monster(146).At(8, 16),
                        Monster(146).At(14, 8).Bonus())),
                    Monster(146).At(8, 8).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES));
        leverRoom.OnCleared(r => r
            .Unlock(leverRoomBack, junctionWest)
            .Say(Game18NConstString.ADoorHasOpened)
            .Say(Game18NConstString.QuickMissionComplete));

        var siegeOut = siege.Portal(new(23, 12), junction, new(1, 12));
        siege.OnEnter(r => r.StartRoomClock(TimeSpan.FromSeconds(200)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Say(Game18NConstString.EnemiesReinforcements)
                .Spawn(Monster(145).Anywhere().Count(3), Monster(151).Anywhere()))
            .OnClockOut(r => r
                .Unlock(siegeOut, crossingEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        corridor.Portal(new(12, 1), guardRoom, new(12, 23), locked: false);
        corridor.Portal(new(1, 12), crossing, new(23, 12), locked: false);

        var guardRoomOut = guardRoom.Portal(new(12, 1), thirdFight, new(12, 23));
        var guardRoomBack = guardRoom.Portal(new(12, 23), corridor, new(12, 1));
        guardRoom.Spawn(
            Monster(149).At(9, 5).Bonus(),
            Monster(146).At(8, 7).OnDeath(r => r.Spawn(
                Monster(146).At(15, 7).OnDeath(r => r.Spawn(
                    Monster(149).At(15, 5).Bonus())),
                Monster(146).At(17, 9).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(6, 12).OnDeath(r => r.Spawn(
                        Monster(152).At(10, 14).Bonus(),
                        Monster(152).At(14, 14).OnDeath(r => r.Spawn(
                            Monster(152).At(18, 13).Bonus())))))))),
            Monster(146).At(7, 9));
        guardRoom.Lever(new(10, 10))
            .OnUse(r => r.Unlock(guardRoomBack, guardRoomOut).Say(Game18NConstString.ADoorHasOpened));

        var thirdFightOut = thirdFight.Portal(new(23, 12), hideout, new(1, 11));
        var thirdFightBack = thirdFight.Portal(new(12, 23), guardRoom, new(12, 1));
        thirdFight.Spawn(
            Monster(146).At(18, 17).Bonus(),
            Monster(146).At(11, 12).Bonus().OnDeath(r => r.Spawn(
                Monster(146).At(9, 11),
                Monster(146).At(15, 18).Bonus(),
                Monster(149).At(17, 11).Bonus())));
        thirdFight.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdFightOut, thirdFightBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var hideoutExit = hideout.Exit(new(23, 11));
        var hideoutBack = hideout.Portal(new(1, 11), thirdFight, new(23, 12));
        hideout.Spawn(Npc(389).At(14, 11));
        hideout.Spawn(
            Monster(146).At(19, 17).Bonus().OnDeath(r => r.Spawn(
                Monster(149).At(9, 7),
                Monster(149).At(20, 6).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(11, 6).Bonus(),
                    Monster(152).At(18, 6).Bonus(),
                    Monster(152).At(20, 16).Bonus())))),
            Monster(146).At(19, 5).Bonus(),
            Monster(146).At(9, 5).Bonus());
        hideout.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(hideoutExit)
                .Unlock(hideoutBack)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
    }
}

return new TimeSpace628();
