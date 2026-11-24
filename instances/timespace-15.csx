public class TimeSpace15 : TimeSpaceScript
{
    public TimeSpace15() : base(15, "A Bold Warrior: Part 3")
    {
        Levels(25, 99);
        Start(8, 8);
        Requires(1012, 2);
        Lives(3);
        Clock(TimeSpan.FromSeconds(1300));
        Draw(20, 1);
        Draw(34, 1);
        Draw(48, 1);
        Special(800, 1);
        Reward(1013, 1);
        Reward(1087, 1);

        var entrance = Room(4014, 2, 11);
        var camp = Room(4004, 3, 11);
        var floor1 = Room(4012, 4, 11);
        var floor2 = Room(4012, 4, 10);
        var floor3 = Room(4012, 4, 9);
        var floor4 = Room(4012, 4, 8);
        var floor5 = Room(4012, 4, 7);
        var floor6 = Room(4012, 4, 6);
        var floor7 = Room(4012, 4, 5);
        var floor8 = Room(4012, 4, 4);
        var floor9 = Room(4012, 4, 3);
        var failedAttack = Room(4012, 4, 2);
        var way = Room(4004, 5, 2);

        entrance.Portal(new(17, 9), camp, new(0, 9), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        camp.Portal(new(17, 9), floor1, new(0, 9), locked: false);
        camp.Portal(new(0, 9), entrance, new(17, 9), locked: false);
        camp.Spawn(Npc(317).At(3, 8).Escort());
        camp.OnEnter(r => r.Dialog(6189));

        var floor1ToFloor2 = floor1.Portal(new(8, 0), floor2, new(8, 17));
        var floor1ToCamp = floor1.Portal(new(0, 9), camp, new(17, 9));
        floor1.Spawn(Npc(320).At(8, 13), Npc(320).At(8, 3), Npc(320).At(8, 9), Npc(320).At(7, 5), Npc(320).At(6, 6), Npc(320).At(5, 10), Npc(320).At(5, 8), Npc(337).At(3, 9));
        floor1.Spawn(
            Monster(109).At(11, 6).Bonus(),
            Monster(111).At(9, 14),
            Monster(109).At(9, 12),
            Monster(108).At(8, 7).Talk(6190));
        floor1.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Dialog(6190))
            .OnCleared(r => r
                .Unlock(floor1ToFloor2, floor1ToCamp)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor2ToFloor3 = floor2.Portal(new(8, 0), floor3, new(8, 17));
        var floor2ToFloor1 = floor2.Portal(new(8, 17), floor1, new(8, 0));
        floor2.Spawn(Npc(320).At(8, 13), Npc(320).At(8, 3), Npc(320).At(7, 11), Npc(320).At(7, 5), Npc(320).At(6, 6), Npc(320).At(5, 10), Npc(320).At(5, 8));
        floor2.OnEnter(r => r
                .Dialog(6190)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 2))
            .OnDialogClosed(6190, r => r
                .Spawn(
                    Monster(109).At(11, 9),
                    Monster(109).At(11, 6).Bonus(),
                    Monster(111).At(9, 3),
                    Monster(108).At(8, 9),
                    Monster(108).At(8, 7))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor2ToFloor3, floor2ToFloor1)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor3ToFloor4 = floor3.Portal(new(8, 0), floor4, new(8, 17));
        var floor3ToFloor2 = floor3.Portal(new(8, 17), floor2, new(8, 0));
        floor3.Spawn(Npc(320).At(8, 13), Npc(320).At(8, 3), Npc(320).At(7, 11), Npc(320).At(7, 5), Npc(320).At(6, 6), Npc(320).At(5, 10), Npc(320).At(5, 8));
        floor3.OnEnter(r => r
                .Dialog(6190)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 3))
            .OnDialogClosed(6190, r => r
                .Spawn(
                    Monster(109).At(9, 5),
                    Monster(111).At(9, 3),
                    Monster(108).At(8, 9),
                    Monster(108).At(8, 7))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor3ToFloor4, floor3ToFloor2)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor4ToFloor5 = floor4.Portal(new(8, 0), floor5, new(8, 17));
        var floor4ToFloor3 = floor4.Portal(new(8, 17), floor3, new(8, 0));
        floor4.Spawn(Npc(320).At(14, 9), Npc(320).At(12, 10), Npc(320).At(9, 12), Npc(320).At(9, 10), Npc(320).At(9, 7), Npc(320).At(7, 7), Npc(320).At(6, 10), Npc(320).At(4, 9), Npc(320).At(6, 7));
        floor4.Spawn(
            Monster(108).At(16, 9),
            Monster(105).At(13, 7).Bonus(),
            Monster(111).At(11, 11),
            Monster(105).At(11, 7),
            Monster(111).At(11, 6),
            Monster(111).At(3, 8).Bonus(),
            Monster(108).At(2, 9).Talk(6190));
        floor4.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES)
                .Dialog(6190))
            .OnCleared(r => r
                .Unlock(floor4ToFloor5, floor4ToFloor3)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor5ToFloor6 = floor5.Portal(new(8, 0), floor6, new(8, 17));
        var floor5ToFloor4 = floor5.Portal(new(8, 17), floor4, new(8, 0));
        floor5.Spawn(Npc(320).At(14, 9), Npc(320).At(12, 10), Npc(320).At(9, 12), Npc(320).At(9, 10), Npc(320).At(9, 7), Npc(320).At(7, 7), Npc(320).At(6, 10), Npc(320).At(4, 9), Npc(320).At(3, 7));
        floor5.OnEnter(r => r
                .Dialog(6190)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 5))
            .OnDialogClosed(6190, r => r
                .Spawn(
                    Monster(111).At(11, 11),
                    Monster(105).At(11, 7),
                    Monster(111).At(11, 6),
                    Monster(111).At(10, 9),
                    Monster(111).At(6, 5),
                    Monster(108).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor5ToFloor6, floor5ToFloor4)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor6ToFloor7 = floor6.Portal(new(8, 0), floor7, new(8, 17));
        var floor6ToFloor5 = floor6.Portal(new(8, 17), floor5, new(8, 0));
        floor6.Spawn(Npc(320).At(8, 13), Npc(320).At(8, 3), Npc(320).At(7, 11), Npc(320).At(7, 5), Npc(320).At(6, 6), Npc(320).At(5, 10), Npc(320).At(5, 8));
        floor6.OnEnter(r => r
                .Dialog(6190)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 6))
            .OnDialogClosed(6190, r => r
                .Spawn(
                    Monster(109).At(9, 5),
                    Monster(111).At(9, 3),
                    Monster(108).At(8, 9),
                    Monster(108).At(8, 7))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor6ToFloor7, floor6ToFloor5)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor7ToFloor8 = floor7.Portal(new(8, 0), floor8, new(8, 17));
        var floor7ToFloor6 = floor7.Portal(new(8, 17), floor6, new(8, 0));
        floor7.Spawn(Npc(320).At(14, 9), Npc(320).At(12, 10), Npc(320).At(9, 12), Npc(320).At(9, 10), Npc(320).At(9, 7), Npc(320).At(7, 7), Npc(320).At(6, 10), Npc(320).At(4, 9), Npc(320).At(3, 7));
        floor7.OnEnter(r => r
                .Dialog(6190)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 7))
            .OnDialogClosed(6190, r => r
                .Spawn(
                    Monster(108).At(16, 9).Bonus(),
                    Monster(105).At(13, 7),
                    Monster(111).At(11, 11),
                    Monster(105).At(11, 7),
                    Monster(111).At(6, 12),
                    Monster(111).At(6, 5),
                    Monster(108).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor7ToFloor8, floor7ToFloor6)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor8ToFloor9 = floor8.Portal(new(8, 0), floor9, new(8, 17));
        var floor8ToFloor7 = floor8.Portal(new(8, 17), floor7, new(8, 0));
        floor8.Spawn(Npc(320).At(14, 9), Npc(320).At(12, 10), Npc(320).At(9, 12), Npc(320).At(9, 10), Npc(320).At(9, 7), Npc(320).At(7, 7), Npc(320).At(6, 10), Npc(320).At(4, 9), Npc(320).At(3, 7));
        floor8.OnEnter(r => r
                .Dialog(6191)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 8))
            .OnDialogClosed(6191, r => r
                .Spawn(
                    Monster(108).At(16, 9),
                    Monster(105).At(13, 7),
                    Monster(111).At(11, 11).Bonus(),
                    Monster(111).At(6, 12),
                    Monster(111).At(6, 5),
                    Monster(108).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor8ToFloor9, floor8ToFloor7)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        var floor9ToFailedAttack = floor9.Portal(new(8, 0), failedAttack, new(8, 17));
        var floor9ToFloor8 = floor9.Portal(new(8, 17), floor8, new(8, 0));
        floor9.Spawn(Npc(320).At(14, 9), Npc(320).At(12, 10), Npc(320).At(9, 12), Npc(320).At(9, 10), Npc(320).At(9, 7), Npc(320).At(7, 7), Npc(320).At(6, 10), Npc(320).At(4, 9), Npc(320).At(3, 7));
        floor9.OnEnter(r => r
                .Dialog(6192)
                .Say(LanguageKey.TIMESPACE_STRIKE_CENTRE_FLOOR, 9))
            .OnDialogClosed(6192, r => r
                .Spawn(
                    Monster(108).At(16, 9),
                    // 112 came as one of the miners this one had called fell; nothing ties it to a
                    // scripted death, so it follows the caller's.
                    Monster(111).At(10, 9).OnDeath(r => r.Spawn(Monster(112).At(8, 9))),
                    Monster(105).At(10, 8).Bonus(),
                    Monster(111).At(9, 4),
                    Monster(111).At(6, 12),
                    Monster(111).At(6, 5),
                    Monster(108).At(2, 9))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES))
            .OnCleared(r => r
                .Unlock(floor9ToFailedAttack, floor9ToFloor8)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete));

        failedAttack.Portal(new(17, 9), way, new(0, 9), locked: false);
        failedAttack.Portal(new(8, 17), floor9, new(8, 0), locked: false);
        failedAttack.Spawn(Npc(320).At(12, 8), Npc(320).At(11, 10), Npc(111).At(10, 8), Npc(320).At(10, 6), Npc(320).At(9, 12), Npc(111).At(9, 8), Npc(337).At(8, 11), Npc(111).At(8, 9), Npc(111).At(8, 7), Npc(320).At(8, 5), Npc(111).At(7, 10), Npc(111).At(7, 8), Npc(320).At(6, 11), Npc(320).At(8, 6), Npc(320).At(5, 8));
        failedAttack.OnEnter(r => r.Dialog(6193).Say(LanguageKey.TIMESPACE_ATTACK_FAILED));

        way.Exit(new(17, 9), locked: false);
        way.Portal(new(0, 9), failedAttack, new(17, 9), locked: false);
        way.OnEnter(r => r.Dialog(6194));
    }
}

return new TimeSpace15();
