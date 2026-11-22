public class TimeSpace19 : TimeSpaceScript
{
    public TimeSpace19() : base(19, "The Black Knight")
    {
        Levels(29, 99);
        Start(11, 11);
        Requires(1012, 2);
        Lives(3);
        Clock(TimeSpan.FromSeconds(130));
        Draw(136, 1);
        Draw(143, 1);
        Draw(150, 1);
        Special(2129, 4);
        Reward(2131, 1);
        Reward(1087, 1);

        var entrance = Room(4202, 0, 8);
        var firstStand = Room(4205, 1, 8);
        var secondStand = Room(4205, 2, 8);
        var thirdStand = Room(4205, 3, 8);
        var knight = Room(4205, 4, 8);

        entrance.Portal(new(28, 15), firstStand, new(1, 15), locked: false);
        entrance.OnEnter(r => r.Say(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var firstToSecond = firstStand.Portal(new(28, 15), secondStand, new(1, 15));
        firstStand.Portal(new(1, 15), entrance, new(28, 15));
        firstStand.OnEnter(r => r.Dialog(6101))
            .OnDialogClosed(6101, r => r
                .Spawn(Monster(12).At(12, 9), Monster(12).At(11, 14))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_TWO_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(
                    Monster(4).At(13, 6),
                    Monster(140).At(14, 9),
                    Monster(4).At(14, 22),
                    Monster(140).At(15, 18),
                    Monster(99).At(19, 18))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Dismiss(4, 12, 99, 140)
                .Unlock(firstToSecond)
                .AddTime(TimeSpan.FromSeconds(70))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED));

        var secondToThird = secondStand.Portal(new(28, 15), thirdStand, new(1, 15));
        secondStand.Portal(new(1, 15), firstStand, new(28, 15));
        secondStand.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_ONE_MINUTE)
                .Dialog(6102)
                .Spawn(Monster(140).At(17, 7), Monster(13).At(11, 9).Talk(6102))
                .StartRoomClock(TimeSpan.FromSeconds(60)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(
                    Monster(141).At(12, 18),
                    Monster(13).At(16, 12),
                    Monster(140).At(16, 17),
                    Monster(66).At(19, 14),
                    Monster(66).At(19, 20))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Dismiss(13, 66, 140, 141)
                .Unlock(secondToThird)
                .AddTime(TimeSpan.FromSeconds(120))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED));

        var thirdToKnight = thirdStand.Portal(new(28, 15), knight, new(1, 15));
        thirdStand.Portal(new(1, 15), secondStand, new(28, 15));
        thirdStand.Spawn(
            Monster(140).At(18, 17),
            Monster(100).At(15, 16),
            Monster(141).At(16, 8),
            Monster(66).At(13, 17));
        thirdStand.OnEnter(r => r
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_TWO_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(120)))
            .Every(TimeSpan.FromSeconds(30), r => r
                .Spawn(
                    Monster(66).At(12, 11),
                    Monster(141).At(13, 22),
                    Monster(100).At(15, 13),
                    Monster(140).At(16, 21),
                    Monster(140).At(17, 8),
                    Monster(140).At(19, 12))
                .Say(Game18NConstString.EnemiesReinforcements))
            .OnClockOut(r => r
                .Dismiss(66, 100, 140, 141)
                .Unlock(thirdToKnight)
                .AddTime(TimeSpan.FromSeconds(10))
                .Say(Game18NConstString.ADoorHasOpened)
                .Say(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED));

        knight.Exit(new(28, 15), locked: false);
        knight.Portal(new(1, 15), thirdStand, new(28, 15), locked: false);
        knight.Spawn(Npc(318).At(14, 13));
        knight.OnEnter(r => r.Dialog(6103));
    }
}

return new TimeSpace19();
