public class TimeSpace8 : TimeSpaceScript
{
    // A room is won once its last Small Imp falls, and whatever else is still in it leaves.
    private const short SmallImp = 72;
    private static readonly short[] ImpGuards = [55, 63, 98, 129, 130];

    public TimeSpace8() : base(8, "Nasty Imps")
    {
        Levels(17, 99);
        Start(11, 11);
        Requires(1012, 1);
        Lives(2);
        RecordCost(850);
        Clock(TimeSpan.FromSeconds(180));
        Draw(1031, 1);
        Draw(1032, 1);
        Special(1017, 2);
        Reward(2028, 2);

        var entrance = Room(4100, 0, 8);
        var passage = Room(4101, 0, 9);
        var firstNest = Room(4101, 0, 10);
        var secondNest = Room(4102, 1, 10);
        var thirdNest = Room(4103, 2, 10);
        var leverRoom = Room(4106, 3, 10);
        var guardRoom = Room(4106, 2, 11);
        var fourthNest = Room(4113, 3, 11);
        var hallway = Room(4112, 4, 11);
        var way = Room(4100, 5, 11);

        entrance.Portal(new(14, 28), passage, new(14, 1), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        passage.Portal(new(14, 1), entrance, new(14, 28), locked: false);
        passage.Portal(new(14, 28), firstNest, new(14, 1), locked: false);

        var firstToPassage = firstNest.Portal(new(14, 1), passage, new(14, 28));
        var firstToSecond = firstNest.Portal(new(28, 15), secondNest, new(1, 14));
        firstNest.ClearedAfter(1, SmallImp);
        firstNest.OnEnter(r => r.Dialog(6038))
            .OnDialogClosed(6038, r => r
                .Spawn(
                    Monster(130).At(21, 9).OnDeath(r => r.Spawn(Monster(130).At(24, 15))),
                    Monster(130).At(18, 18),
                    Monster(72).At(19, 14).Bonus())
                .Objective(LanguageKey.TIMESPACE_GOAL_IMPS_ATTACK))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(firstToSecond, firstToPassage)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6039));

        var secondToThird = secondNest.Portal(new(28, 14), thirdNest, new(1, 15));
        secondNest.Portal(new(1, 14), firstNest, new(28, 15), locked: false);
        secondNest.ClearedAfter(1, SmallImp);
        secondNest.OnEnter(r => r.Dialog(6040))
            .OnDialogClosed(6040, r => r
                .Spawn(
                    Monster(55).At(15, 21),
                    Monster(55).At(13, 9).OnDeath(r => r.Spawn(Monster(130).At(17, 19))),
                    Monster(55).At(12, 21),
                    Monster(72).At(15, 14).OnDeath(r => r.Spawn(Monster(55).At(15, 9))))
                .Objective(LanguageKey.TIMESPACE_GOAL_IMPS_ATTACK))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(secondToThird)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6041));

        var thirdToLever = thirdNest.Portal(new(28, 15), leverRoom, new(1, 15));
        var thirdToGuard = thirdNest.Portal(new(14, 28), guardRoom, new(14, 1));
        thirdNest.Portal(new(1, 15), secondNest, new(28, 14));
        thirdNest.ClearedAfter(1, SmallImp);
        thirdNest.OnEnter(r => r.Dialog(6042))
            .OnDialogClosed(6042, r => r
                .Spawn(
                    Monster(98).At(11, 18),
                    Monster(72).At(17, 14).Bonus().OnDeath(r => r.Spawn(
                        Monster(130).At(23, 17).OnDeath(r => r.Spawn(Monster(130).At(21, 12))))))
                .Objective(LanguageKey.TIMESPACE_GOAL_IMPS_ATTACK))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(thirdToLever)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6043));

        var leverToThird = leverRoom.Portal(new(1, 15), thirdNest, new(28, 15));
        leverRoom.Lever(new(15, 14)).OnUse(r => r.Unlock(thirdToGuard));
        leverRoom.OnEnter(r => r
            .Unlock(leverToThird)
            .Say(Game18NConstString.ADoorHasOpened)
            .Dialog(6044));

        guardRoom.Portal(new(14, 1), thirdNest, new(14, 28));
        var guardToFourth = guardRoom.Portal(new(28, 15), fourthNest, new(1, 15));
        guardRoom.Spawn(
            Monster(63).At(8, 16).Bonus(),
            Monster(72).At(15, 14).OnDeath(r => r.Spawn(
                Monster(130).At(17, 18).OnDeath(r => r.Spawn(Monster(63).At(20, 9))))));
        guardRoom.ClearedAfter(1, SmallImp);
        guardRoom.Spawn(Npc(320).At(15, 7));
        guardRoom.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_PROTECT_GUARD_SOLDIER))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(guardToFourth)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6045));

        var fourthToHallway = fourthNest.Portal(new(28, 15), hallway, new(1, 15));
        fourthNest.Portal(new(1, 15), guardRoom, new(28, 15));
        fourthNest.ClearedAfter(2, SmallImp);
        fourthNest.OnEnter(r => r.Dialog(6048))
            .OnDialogClosed(6048, r => r.Spawn(
                Monster(129).At(15, 21),
                Monster(72).At(19, 15).OnDeath(r => r.Spawn(
                    Monster(129).At(10, 15).OnDeath(r => r.Spawn(Monster(130).At(19, 7))),
                    Monster(129).At(16, 8))),
                Monster(72).At(19, 10).Bonus()))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(fourthToHallway)
                .AddTime(TimeSpan.FromSeconds(100))
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6049));

        hallway.Portal(new(28, 14), way, new(1, 15), locked: false);
        hallway.Portal(new(1, 15), fourthNest, new(28, 15));

        var exit = way.Exit(new(28, 15));
        way.Portal(new(1, 15), hallway, new(28, 14));
        way.Spawn(Monster(72).At(17, 12).OnDeath(r => r.Spawn(Monster(72).At(17, 14))));
        way.ClearedAfter(2, SmallImp);
        way.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_IMPS_ATTACK))
            .OnCleared(r => r
                .Dismiss(ImpGuards)
                .Unlock(exit)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(6051));
    }
}

return new TimeSpace8();
