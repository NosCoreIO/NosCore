public class TimeSpace23 : TimeSpaceScript
{
    public TimeSpace23() : base(23, "The Kovolt Hero")
    {
        Levels(33, 99);
        Start(7, 8);
        Requires(1012, 3);
        Lives(3);
        Clock(TimeSpan.FromSeconds(3000));
        Draw(515, 1);
        Draw(702, 1);
        Special(801, 1);
        Reward(1013, 1);
        Reward(1087, 1);

        var landing = Room(5203, 0, 0);
        var camp = Room(5204, 1, 0);
        var firstFight = Room(5207, 2, 0);
        var secondFight = Room(5208, 2, 1);
        var thirdFight = Room(5207, 3, 1);
        var shelter = Room(5208, 3, 2);
        var fourthFight = Room(5204, 4, 2);
        var siege = Room(5207, 5, 2);
        var retreat = Room(5208, 5, 3);
        var firstGuard = Room(5204, 6, 3);
        var secondGuard = Room(5204, 7, 3);
        var thirdGuard = Room(5205, 8, 3);
        var sculptureHall = Room(4003, 9, 3);
        var mine14 = Room(4011, 9, 2);
        var mine15 = Room(4000, 10, 2);
        var mine16 = Room(4003, 11, 2);
        var mine17 = Room(4008, 11, 1);
        var mine18 = Room(4002, 10, 1);
        var mine19 = Room(4014, 9, 1);
        var mine20 = Room(4015, 10, 0);
        var mine21 = Room(4007, 11, 3);
        var mine22 = Room(4006, 11, 4);
        var mine23 = Room(4003, 11, 5);
        var mine24 = Room(4009, 11, 6);
        var mine25 = Room(4014, 10, 6);
        var mine26 = Room(4005, 10, 5);
        var mine27 = Room(4001, 9, 5);
        var mine28 = Room(4013, 9, 6);
        var lastStand = Room(4003, 9, 4);
        var mine30 = Room(4006, 10, 3);
        var mine31 = Room(4013, 10, 4);

        landing.Portal(new(23, 12), camp, new(1, 12), locked: false);
        landing.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        camp.Portal(new(23, 12), firstFight, new(1, 12), locked: false);
        camp.Portal(new(1, 12), landing, new(23, 12), locked: false);
        camp.Spawn(Npc(311).At(15, 10), Npc(311).At(14, 8), Npc(311).At(13, 11), Npc(311).At(3, 10));
        camp.OnEnter(r => r.Dialog(6195));

        var firstFightToSecond = firstFight.Portal(new(12, 23), secondFight, new(12, 1));
        var firstFightToCamp = firstFight.Portal(new(1, 12), camp, new(23, 12));
        firstFight.Spawn(
            Monster(116).At(10, 16),
            Monster(118).At(10, 13),
            Monster(118).At(8, 12),
            Monster(115).At(7, 15),
            Monster(115).At(7, 12).Bonus(),
            Monster(116).At(6, 12),
            Monster(115).At(4, 12),
            Monster(118).At(3, 12),
            Monster(115).At(1, 12));
        firstFight.Spawn(Npc(311).At(10, 14), Npc(311).At(8, 11), Npc(311).At(9, 13), Npc(311).At(5, 10), Npc(311).At(2, 11), Npc(311).At(1, 10));
        firstFight.OnCleared(r => r
            .Unlock(firstFightToSecond, firstFightToCamp)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6196));

        var secondFightToFirst = secondFight.Portal(new(12, 1), firstFight, new(12, 23));
        var secondFightToThird = secondFight.Portal(new(23, 12), thirdFight, new(1, 12));
        secondFight.Spawn(
            Monster(116).At(19, 14),
            Monster(115).At(17, 14).Bonus(),
            Monster(121).At(17, 13),
            Monster(121).At(16, 9),
            Monster(115).At(15, 10),
            Monster(118).At(15, 8),
            Monster(118).At(12, 9),
            Monster(115).At(12, 7),
            Monster(118).At(11, 7).Bonus(),
            Monster(121).At(14, 8),
            Monster(115).At(9, 2),
            Monster(118).At(9, 4));
        secondFight.Spawn(Npc(311).At(19, 13), Npc(311).At(18, 14), Npc(311).At(18, 12), Npc(311).At(13, 9), Npc(311).At(10, 4), Npc(311).At(10, 2), Npc(311).At(9, 7));
        secondFight.OnCleared(r => r
            .Unlock(secondFightToThird, secondFightToFirst)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6197));

        var thirdFightToShelter = thirdFight.Portal(new(12, 23), shelter, new(12, 1));
        var thirdFightToSecond = thirdFight.Portal(new(1, 12), secondFight, new(23, 12));
        thirdFight.Spawn(
            Monster(116).At(10, 16),
            Monster(118).At(10, 13),
            Monster(118).At(8, 13),
            Monster(115).At(3, 13),
            Monster(115).At(10, 14),
            Monster(116).At(9, 12),
            Monster(115).At(4, 13),
            Monster(118).At(3, 10).Bonus(),
            Monster(115).At(1, 13));
        thirdFight.Spawn(Npc(311).At(9, 15), Npc(311).At(8, 11), Npc(311).At(7, 14), Npc(311).At(5, 10), Npc(311).At(2, 14), Npc(311).At(1, 10));
        thirdFight.OnCleared(r => r
            .Unlock(thirdFightToSecond, thirdFightToShelter)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6198));

        shelter.Portal(new(12, 1), thirdFight, new(12, 23), locked: false);
        shelter.Portal(new(23, 12), fourthFight, new(1, 12), locked: false);
        shelter.OnEnter(r => r.Dialog(6199));

        var fourthFightToSiege = fourthFight.Portal(new(23, 12), siege, new(1, 12));
        var fourthFightToShelter = fourthFight.Portal(new(1, 12), shelter, new(23, 12));
        fourthFight.Spawn(
            Monster(121).At(16, 11),
            Monster(115).At(15, 9),
            Monster(121).At(14, 8),
            Monster(118).At(13, 10),
            Monster(116).At(11, 9),
            Monster(115).At(7, 9).Bonus(),
            Monster(118).At(10, 8),
            Monster(116).At(13, 9),
            Monster(115).At(7, 12),
            Monster(121).At(7, 9),
            Monster(118).At(5, 12),
            Monster(115).At(3, 12),
            Monster(121).At(2, 10));
        fourthFight.Spawn(Npc(311).At(16, 10), Npc(311).At(16, 8), Npc(311).At(14, 8), Npc(311).At(12, 7), Npc(311).At(8, 8), Npc(311).At(6, 9), Npc(311).At(5, 13), Npc(311).At(3, 10), Npc(311).At(1, 13));
        fourthFight.OnCleared(r => r
            .Unlock(fourthFightToShelter, fourthFightToSiege)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6200));

        var siegeToRetreat = siege.Portal(new(12, 23), retreat, new(12, 1));
        var siegeToFourthFight = siege.Portal(new(1, 12), fourthFight, new(23, 12));
        siege.Spawn(Npc(311).At(8, 11), Npc(311).At(7, 14), Npc(311).At(5, 10), Npc(311).At(2, 13), Npc(311).At(1, 10));
        siege.OnEnter(r => r.Say(Game18NConstString.EnemiesReinforcements).Dialog(6201))
            .OnDialogClosed(6201, r => r
                .Spawn(Monster(313).At(10, 12), Monster(115).At(1, 13))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_KOARENS)
                .StartRoomClock(TimeSpan.FromSeconds(60))
                .Spawn(SiegeWave())
                .After(TimeSpan.FromSeconds(30), r => r.Spawn(SiegeWave())))
            .OnClockOut(r => r
                .Dismiss(115, 116, 118, 313)
                .Unlock(siegeToRetreat, siegeToFourthFight)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6202));

        retreat.Portal(new(12, 1), siege, new(12, 23), locked: false);
        retreat.Portal(new(23, 12), firstGuard, new(1, 12), locked: false);
        retreat.OnEnter(r => r.Dialog(6203));

        var firstGuardToSecond = firstGuard.Portal(new(23, 12), secondGuard, new(1, 12));
        var firstGuardToRetreat = firstGuard.Portal(new(1, 12), retreat, new(23, 12));
        firstGuard.Spawn(Npc(310).At(5, 14), Npc(310).At(4, 10), Npc(337).At(3, 14), Npc(312).At(3, 9));
        firstGuard.OnEnter(r => r.Dialog(6204).Say(LanguageKey.TIMESPACE_ESCAPE_WITH_THE_CAPTAIN))
            .OnDialogClosed(6204, r => r
                .Spawn(
                    Monster(121).At(15, 8),
                    Monster(118).At(14, 10),
                    Monster(118).At(12, 8),
                    Monster(121).At(11, 10).Bonus(),
                    Monster(115).At(10, 8),
                    Monster(115).At(9, 10),
                    Monster(121).At(8, 8),
                    Monster(115).At(6, 9).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstGuardToSecond, firstGuardToRetreat)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var secondGuardToThird = secondGuard.Portal(new(23, 12), thirdGuard, new(1, 12));
        var secondGuardToFirst = secondGuard.Portal(new(1, 12), firstGuard, new(23, 12));
        secondGuard.OnEnter(r => r.Dialog(6204))
            .OnDialogClosed(6204, r => r
                .Spawn(
                    Monster(121).At(15, 8).Bonus(),
                    Monster(118).At(14, 10),
                    Monster(118).At(12, 8),
                    Monster(121).At(11, 10),
                    Monster(115).At(10, 8),
                    Monster(115).At(9, 10),
                    Monster(121).At(8, 8),
                    Monster(115).At(6, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondGuardToFirst, secondGuardToThird)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var thirdGuardToHall = thirdGuard.Portal(new(23, 12), sculptureHall, new(0, 9));
        var thirdGuardToSecond = thirdGuard.Portal(new(1, 12), secondGuard, new(23, 12));
        thirdGuard.OnEnter(r => r.Dialog(6204))
            .OnDialogClosed(6204, r => r
                .Spawn(
                    Monster(121).At(15, 8),
                    Monster(118).At(14, 10),
                    Monster(118).At(12, 11),
                    Monster(121).At(11, 10),
                    Monster(115).At(10, 8),
                    Monster(115).At(9, 10),
                    Monster(121).At(8, 8),
                    Monster(115).At(6, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdGuardToSecond, thirdGuardToHall)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        sculptureHall.Portal(new(8, 0), mine14, new(8, 17), locked: false);
        var hallToLastStand = sculptureHall.Portal(new(8, 17), lastStand, new(8, 0));
        sculptureHall.Portal(new(0, 9), thirdGuard, new(23, 12), locked: false);
        sculptureHall.OnEnter(r => r.Dialog(6205))
            .OnCleared(r => r
                .Unlock(hallToLastStand)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));
        sculptureHall.Sculpture(new(10, 16), 1048, 1049)
            .OnUse(r => r
                .Say(Game18NConstString.ObjectHasBeenMoved)
                .Spawn(
                    Monster(121).At(10, 13),
                    Monster(121).At(10, 5),
                    Monster(121).At(8, 9).OnDeath(r => r.Spawn(
                        Monster(121).At(7, 6).OnDeath(r => r.Spawn(Monster(121).At(8, 16), Monster(121).At(10, 8))),
                        Monster(121).At(10, 2))),
                    Monster(121).At(8, 0),
                    Monster(121).At(7, 14),
                    Monster(121).At(7, 4),
                    Monster(121).At(6, 10),
                    Monster(121).At(5, 7),
                    Monster(121).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES));

        mine14.Portal(new(17, 9), mine15, new(0, 9), locked: false);
        mine14.Portal(new(8, 17), sculptureHall, new(8, 0));
        mine14.OnEnter(r => r.Say(LanguageKey.TIMESPACE_LONG_WAY_AHEAD));

        mine15.Portal(new(17, 9), mine16, new(0, 9), locked: false);
        mine15.Portal(new(8, 17), mine30, new(8, 0), locked: false);
        mine15.Portal(new(0, 9), mine14, new(17, 9), locked: false);

        mine16.Portal(new(8, 0), mine17, new(8, 17), locked: false);
        mine16.Portal(new(8, 17), mine21, new(8, 0), locked: false);
        mine16.Portal(new(0, 9), mine15, new(17, 9), locked: false);

        mine17.Portal(new(8, 17), mine16, new(8, 0), locked: false);
        mine17.Portal(new(0, 9), mine18, new(17, 9), locked: false);

        mine18.Portal(new(8, 0), mine20, new(8, 17), locked: false);
        mine18.Portal(new(17, 9), mine17, new(0, 9), locked: false);
        mine18.Portal(new(0, 9), mine19, new(17, 9), locked: false);

        mine19.Portal(new(17, 9), mine18, new(0, 9), locked: false);

        mine20.Portal(new(8, 17), mine18, new(8, 0), locked: false);

        mine21.Portal(new(8, 0), mine16, new(8, 17), locked: false);
        mine21.Portal(new(8, 17), mine22, new(8, 0), locked: false);

        mine22.Portal(new(8, 0), mine21, new(8, 17), locked: false);
        mine22.Portal(new(8, 17), mine23, new(8, 0), locked: false);

        mine23.Portal(new(8, 0), mine22, new(8, 17), locked: false);
        mine23.Portal(new(8, 17), mine24, new(8, 0), locked: false);
        mine23.Portal(new(0, 9), mine26, new(17, 9), locked: false);

        mine24.Portal(new(8, 0), mine23, new(8, 17), locked: false);
        mine24.Portal(new(0, 9), mine25, new(17, 9), locked: false);

        mine25.Portal(new(17, 9), mine24, new(0, 9), locked: false);

        mine26.Portal(new(17, 9), mine23, new(0, 9), locked: false);
        mine26.Portal(new(0, 9), mine27, new(17, 9), locked: false);

        mine27.Portal(new(8, 0), lastStand, new(8, 17), locked: false);
        mine27.Portal(new(17, 9), mine26, new(0, 9), locked: false);
        mine27.Portal(new(8, 17), mine28, new(8, 0), locked: false);

        mine28.Portal(new(8, 0), mine27, new(8, 17), locked: false);

        lastStand.Portal(new(8, 0), sculptureHall, new(8, 17));
        lastStand.Portal(new(8, 17), mine27, new(8, 0));
        var way = lastStand.Exit(new(0, 9));
        lastStand.OnEnter(r => r.Dialog(6206))
            .OnDialogClosed(6206, r => r
                .Spawn(
                    Monster(115).At(7, 14).OnDeath(r => r.Spawn(Monster(115).At(7, 3))),
                    Monster(115).At(6, 11),
                    Monster(115).At(3, 8).OnDeath(r => r.Spawn(
                        Monster(115).At(8, 2).OnDeath(r => r.Spawn(
                            Monster(121).At(9, 11),
                            Monster(118).At(9, 15).OnDeath(r => r.Spawn(
                                Monster(121).At(8, 5),
                                Monster(118).At(8, 8).OnDeath(r => r.Spawn(Monster(121).At(5, 9))))))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(way)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6207));

        mine30.Portal(new(8, 0), mine15, new(8, 17), locked: false);
        mine30.Portal(new(8, 17), mine31, new(8, 0), locked: false);

        mine31.Portal(new(8, 0), mine30, new(8, 17), locked: false);
    }

    private static IMonster[] SiegeWave() =>
    [
        Monster(118).At(3, 10),
        Monster(115).At(4, 13),
        Monster(116).At(6, 10),
        Monster(115).At(7, 12),
        Monster(115).At(7, 15),
        Monster(118).At(8, 10),
        Monster(118).At(10, 13),
        Monster(116).At(10, 16),
    ];
}

return new TimeSpace23();
