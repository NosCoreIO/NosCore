public class TimeSpace0 : TimeSpaceScript
{
    public TimeSpace0() : base(0, "Time-Space Tutorial")
    {
        Levels(1, 99);
        Start(11, 11);
        Lives(2);
        Clock(TimeSpan.FromSeconds(300));
        Draw(2023, 1);
        Draw(2024, 1);
        Draw(2025, 1);
        Draw(2026, 1);
        Draw(2027, 1);
        Special(2016, 2);
        Reward(13, 1);

        var lobby = Room(4100, 3, 11);
        var firstFight = Room(4105, 3, 10);
        var ambush = Room(4114, 3, 9);
        var leverRoom = Room(4100, 3, 8);
        var timedRoom = Room(4101, 3, 7);
        var guardRoom = Room(4102, 3, 6);
        var way = Room(4103, 3, 5);

        lobby.Portal(new(14, 1), firstFight, new(14, 28), locked: false);
        lobby.OnEnter(r => r
            .Say(Game18NConstString.MissionStartWhenEnterFirstRoom)
            .Dialog(8004)
            .Say(LanguageKey.TIMESPACE_LOBBY));

        firstFight.Portal(new(14, 1), ambush, new(14, 28), locked: false);
        firstFight.Portal(new(14, 28), lobby, new(14, 1), locked: false);
        firstFight.OnEnter(r => r.Dialog(8005))
            .OnDialogClosed(8005, r => r.Spawn(Monster(24).At(13, 14).OnDeath(r => r.Spawn(Monster(24).At(17, 12)))));

        var ambushToLever = ambush.Portal(new(14, 1), leverRoom, new(14, 28));
        ambush.Portal(new(14, 28), firstFight, new(14, 1));
        ambush.OnEnter(r => r.Dialog(8006))
            .OnDialogClosed(8006, r => r
                .Spawn(Monster(333).At(12, 13))
                .Objective(LanguageKey.TIMESPACE_GOAL_UNDER_ATTACK))
            .OnCleared(r => r
                .Unlock(ambushToLever)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(8007));

        var leverToTimed = leverRoom.Portal(new(14, 1), timedRoom, new(14, 28));
        leverRoom.Portal(new(14, 28), ambush, new(14, 1));
        leverRoom.Lever(new(14, 18))
            .OnUse(r => r.Spawn(Monster(333).At(20, 15), Monster(333).At(8, 15)));
        leverRoom.OnEnter(r => r.Dialog(8008))
            .OnCleared(r => r
                .Unlock(leverToTimed)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(8009));

        var timedToGuard = timedRoom.Portal(new(14, 1), guardRoom, new(14, 28));
        timedRoom.Portal(new(14, 28), leverRoom, new(14, 1));
        timedRoom.OnEnter(r => r
                .Dialog(8010)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .OnDialogClosed(8010, r => r
                .Spawn(Monster(333).At(11, 14).OnDeath(r => r.Spawn(Monster(24).At(16, 17))))
                .Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_IN_TIME))
            .OnCleared(r => r
                .Unlock(timedToGuard)
                .AddTime(TimeSpan.FromSeconds(30))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(Game18NConstString.QuickMissionComplete)
                .StopRoomClock());

        var guardToWay = guardRoom.Portal(new(14, 1), way, new(14, 28));
        guardRoom.Portal(new(14, 28), timedRoom, new(14, 1));
        guardRoom.Spawn(Npc(320).At(14, 14));
        guardRoom.OnEnter(r => r.Dialog(8013))
            .OnDialogClosed(8013, r => r
                .Spawn(Monster(25).At(15, 12), Monster(24).At(11, 14))
                .Objective(LanguageKey.TIMESPACE_GOAL_PROTECT_GUARD_KILL_ALL))
            .OnCleared(r => r
                .Unlock(guardToWay)
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_NOSVILLE_GUARD_SAFE));

        var exit = way.Exit(new(14, 1));
        way.Portal(new(14, 28), guardRoom, new(14, 1));
        way.OnEnter(r => r.Dialog(8011))
            .OnDialogClosed(8011, r => r.Spawn(Monster(24).At(19, 12), Monster(333).At(15, 17)))
            .OnCleared(r => r
                .Unlock(exit)
                .Say(Game18NConstString.ADoorHasOpened)
                .Dialog(8012));
    }
}

return new TimeSpace0();
