public class TimeSpace36 : TimeSpaceScript
{
    public TimeSpace36() : base(36, "Alliance avec les Kovolts")
    {
        Levels(46, 99);
        Start(9, 13);
        Requires(1012, 4);
        Lives(3);
        Clock(TimeSpan.FromSeconds(200));
        Draw(713, 1);
        Draw(526, 1);
        Special(1016, 2);
        Reward(2127, 1);
        Reward(1087, 1);

        var entrance = Room(4201, 4, 11);
        var firstPass = Room(4209, 4, 10);
        var firstBattle = Room(4207, 3, 10);
        var secondPass = Room(4206, 3, 9);
        var secondBattle = Room(4208, 3, 8);
        var firstCamp = Room(4205, 4, 8);
        var thirdBattle = Room(4210, 5, 8);
        var thirdPass = Room(4208, 5, 7);
        var fourthBattle = Room(4210, 6, 7);
        var fourthPass = Room(4209, 6, 6);
        var secondCamp = Room(4205, 5, 6);
        var fifthBattle = Room(4207, 4, 6);
        var fifthPass = Room(4206, 4, 5);
        var lastStand = Room(4206, 4, 4);

        entrance.Portal(new(14, 1), firstPass, new(14, 28), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        firstPass.Portal(new(14, 28), entrance, new(14, 1), locked: false);
        firstPass.Portal(new(1, 15), firstBattle, new(28, 15), locked: false);
        firstPass.Spawn(
            Monster(67).At(14, 26).OnDeath(r => r.Spawn(
                Monster(61).At(18, 14))),
            Monster(61).At(12, 16).OnDeath(r => r.Spawn(
                Monster(61).At(17, 17).OnDeath(r => r.Spawn(
                    Monster(67).At(7, 16).OnDeath(r => r.Spawn(
                        Monster(67).At(19, 9).Bonus(),
                        Monster(67).At(22, 15).Bonus())))))),
            Monster(61).At(11, 9).Bonus());

        var firstBattleNorth = firstBattle.Portal(new(14, 1), secondPass, new(14, 28));
        var firstBattleEast = firstBattle.Portal(new(28, 15), firstPass, new(1, 15));
        firstBattle.Spawn(Npc(116).At(19, 21).Ally(), Npc(122).At(14, 19).Ally(), Npc(122).At(10, 19).Ally());
        firstBattle.Spawn(
            Monster(149).At(22, 15),
            Monster(149).At(12, 10).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(8, 16).OnDeath(r => r.Spawn(
                    Monster(152).At(15, 17).Bonus().OnDeath(r => r.Spawn(
                        Monster(152).At(7, 12).Bonus())))),
                Monster(149).At(17, 10).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(21, 19).Bonus())))));
        firstBattle.OnEnter(r => r.Dialog(6151).Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(firstBattleNorth, firstBattleEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(150)));

        secondPass.Portal(new(14, 1), secondBattle, new(14, 28), locked: false);
        secondPass.Portal(new(14, 28), firstBattle, new(14, 1), locked: false);
        secondPass.Spawn(
            Monster(67).At(20, 16).OnDeath(r => r.Spawn(
                Monster(61).At(18, 14).OnDeath(r => r.Spawn(
                    Monster(61).At(17, 17).OnDeath(r => r.Spawn(
                        Monster(67).At(7, 16).Bonus())))))),
            Monster(67).At(19, 9).Bonus());

        var secondBattleEast = secondBattle.Portal(new(28, 15), firstCamp, new(1, 15));
        var secondBattleSouth = secondBattle.Portal(new(14, 28), secondPass, new(14, 1));
        secondBattle.Spawn(Npc(116).At(19, 21).Ally(), Npc(122).At(15, 19).Ally(), Npc(122).At(10, 19).Ally(),
            Npc(116).At(5, 18).Ally());
        secondBattle.Spawn(
            Monster(149).At(22, 15).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(8, 16).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(17, 15).OnDeath(r => r.Spawn(
                        Monster(152).At(21, 19).OnDeath(r => r.Spawn(
                            Monster(152).At(7, 12).Bonus().OnDeath(r => r.Spawn(
                                Monster(146).At(10, 13).Bonus(),
                                Monster(146).At(20, 13).Bonus())))))))),
                Monster(149).At(17, 10).Bonus())),
            Monster(149).At(12, 10));
        secondBattle.OnEnter(r => r.Dialog(6151).Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(secondBattleEast, secondBattleSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(120)));

        firstCamp.Portal(new(28, 15), thirdBattle, new(1, 15), locked: false);
        firstCamp.Portal(new(1, 15), secondBattle, new(28, 15), locked: false);
        firstCamp.Spawn(Npc(319).At(8, 14));
        firstCamp.OnEnter(r => r.Dialog(6152));

        var thirdBattleNorth = thirdBattle.Portal(new(15, 1), thirdPass, new(14, 28));
        var thirdBattleWest = thirdBattle.Portal(new(1, 15), firstCamp, new(28, 15));
        thirdBattle.Spawn(
            Npc(116).At(19, 21).Ally().OnDeath(r => r.Spawn(Monster(152).At(7, 12))),
            Npc(122).At(14, 19).Ally(),
            Npc(122).At(10, 19).Ally(),
            Npc(116).At(6, 19).Ally());
        thirdBattle.Spawn(
            Monster(149).At(22, 15).Bonus(),
            Monster(149).At(12, 10).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(8, 16).OnDeath(r => r.Spawn(
                    Monster(152).At(17, 15),
                    Monster(149).At(24, 13))),
                Monster(149).At(17, 10).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(21, 19).OnDeath(r => r.Spawn(
                        Monster(146).At(10, 13),
                        Monster(146).At(20, 13))))))),
            Monster(149).At(4, 14));
        thirdBattle.OnEnter(r => r.Dialog(6151).Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(thirdBattleNorth, thirdBattleWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(120)));

        thirdPass.Portal(new(28, 15), fourthBattle, new(1, 15), locked: false);
        thirdPass.Portal(new(14, 28), thirdBattle, new(15, 1), locked: false);
        thirdPass.Spawn(
            Monster(67).At(15, 27).Bonus().OnDeath(r => r.Spawn(
                Monster(61).At(18, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(67).At(7, 16))))),
            Monster(61).At(10, 16).OnDeath(r => r.Spawn(
                Monster(61).At(17, 17))),
            Monster(61).At(13, 9).Bonus().OnDeath(r => r.Spawn(
                Monster(67).At(19, 9).Bonus(),
                Monster(67).At(22, 15))));

        var fourthBattleNorth = fourthBattle.Portal(new(15, 1), fourthPass, new(14, 28));
        var fourthBattleWest = fourthBattle.Portal(new(1, 15), thirdPass, new(28, 15));
        fourthBattle.Spawn(Npc(122).At(17, 19).Ally(), Npc(122).At(14, 19).Ally(), Npc(122).At(10, 19).Ally(),
            Npc(116).At(6, 19).Ally());
        fourthBattle.Spawn(
            Monster(149).At(12, 10).Bonus().OnDeath(r => r.Spawn(
                Monster(152).At(8, 16).OnDeath(r => r.Spawn(
                    Monster(152).At(15, 17).OnDeath(r => r.Spawn(
                        Monster(152).At(21, 19).Bonus())),
                    Monster(149).At(17, 10).OnDeath(r => r.Spawn(
                        Monster(149).At(24, 13).Bonus())))),
                Monster(149).At(22, 15).Bonus())),
            Monster(149).At(4, 14));
        fourthBattle.OnEnter(r => r.Dialog(6151).Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(fourthBattleNorth, fourthBattleWest)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .AddTime(TimeSpan.FromSeconds(150)));

        fourthPass.Portal(new(14, 28), fourthBattle, new(15, 1), locked: false);
        fourthPass.Portal(new(1, 15), secondCamp, new(28, 15), locked: false);
        fourthPass.Spawn(
            Monster(67).At(15, 27).OnDeath(r => r.Spawn(
                Monster(61).At(18, 14))),
            Monster(142).At(16, 20).OnDeath(r => r.Spawn(
                Monster(67).At(7, 16).OnDeath(r => r.Spawn(
                    Monster(142).At(15, 13).OnDeath(r => r.Spawn(
                        Monster(142).At(7, 12))))))),
            Monster(61).At(12, 15).OnDeath(r => r.Spawn(
                Monster(61).At(17, 17).OnDeath(r => r.Spawn(
                    Monster(67).At(19, 9).Bonus(),
                    Monster(67).At(22, 15).Bonus())))),
            Monster(61).At(12, 11).Bonus());

        secondCamp.Portal(new(28, 15), fourthPass, new(1, 15), locked: false);
        secondCamp.Portal(new(1, 15), fifthBattle, new(28, 15), locked: false);
        secondCamp.Spawn(Npc(319).At(14, 15));
        secondCamp.OnEnter(r => r.Dialog(6152));

        var fifthBattleNorth = fifthBattle.Portal(new(14, 1), fifthPass, new(14, 28));
        var fifthBattleEast = fifthBattle.Portal(new(28, 15), secondCamp, new(1, 15));
        fifthBattle.Spawn(Npc(122).At(23, 20).Ally(), Npc(116).At(21, 17).Ally(), Npc(122).At(17, 19).Ally(),
            Npc(122).At(14, 19).Ally(), Npc(122).At(10, 19).Ally(), Npc(116).At(6, 19).Ally());
        fifthBattle.Spawn(
            Monster(149).At(22, 15),
            Monster(149).At(12, 10).OnDeath(r => r.Spawn(
                Monster(152).At(8, 16),
                Monster(149).At(17, 10).OnDeath(r => r.Spawn(
                    Monster(152).At(15, 17).OnDeath(r => r.Spawn(
                        Monster(152).At(21, 19).OnDeath(r => r.Spawn(
                            Monster(152).At(7, 12).OnDeath(r => r.Spawn(
                                Monster(146).At(10, 13).OnDeath(r => r.Spawn(
                                    Monster(149).At(11, 15).OnDeath(r => r.Spawn(
                                        Monster(149).At(18, 15))))),
                                Monster(146).At(20, 13))))))),
                    Monster(149).At(24, 13))))),
            Monster(149).At(4, 14).Bonus());
        fifthBattle.Spawn(Item(1042).At(20, 6), Item(1042).At(9, 6));
        fifthBattle.OnEnter(r => r.Dialog(6151))
            .OnCleared(r => r
                .Unlock(fifthBattleNorth, fifthBattleEast)
                .Say(Game18NConstString.ADoorHasOpened)
                .AddTime(TimeSpan.FromSeconds(150))
                .Dialog(6153));

        fifthPass.Portal(new(14, 1), lastStand, new(14, 28), locked: false);
        fifthPass.Portal(new(14, 28), fifthBattle, new(14, 1), locked: false);
        fifthPass.Spawn(
            Monster(67).At(14, 26).OnDeath(r => r.Spawn(
                Monster(61).At(17, 17).OnDeath(r => r.Spawn(
                    Monster(67).At(19, 9).Bonus(),
                    Monster(67).At(22, 15).Bonus())))),
            Monster(61).At(11, 15).OnDeath(r => r.Spawn(
                Monster(61).At(18, 14).OnDeath(r => r.Spawn(
                    Monster(67).At(7, 16).Bonus())))),
            Monster(61).At(9, 9).Bonus());

        var exit = lastStand.Exit(new(14, 1));
        var lastStandSouth = lastStand.Portal(new(14, 28), fifthPass, new(14, 1));
        lastStand.Spawn(Npc(122).At(20, 21).Ally(), Npc(122).At(14, 22).Ally(), Npc(329).At(14, 20).Ally(),
            Npc(122).At(8, 21).Ally());
        lastStand.OnEnter(r => r.Dialog(6154))
            .OnDialogClosed(6154, r => r.Spawn(
                Monster(149).At(22, 15).Bonus(),
                Monster(149).At(12, 10).OnDeath(r => r.Spawn(
                    Monster(152).At(8, 16).OnDeath(r => r.Spawn(
                        Monster(152).At(21, 19))),
                    Monster(149).At(17, 10).Bonus().OnDeath(r => r.Spawn(
                        Monster(146).At(10, 13).OnDeath(r => r.Spawn(
                            Monster(149).At(11, 15).OnDeath(r => r.Spawn(
                                Monster(149).At(18, 15))))),
                        Monster(146).At(20, 13).Bonus())))),
                Monster(149).At(4, 14).Bonus().OnDeath(r => r.Spawn(
                    Monster(152).At(16, 15).OnDeath(r => r.Spawn(
                        Monster(152).At(7, 12).Bonus())),
                    Monster(149).At(24, 13).Bonus()))))
            .OnCleared(r => r
                .Unlock(exit)
                .Unlock(lastStandSouth)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6155));
    }
}

return new TimeSpace36();
