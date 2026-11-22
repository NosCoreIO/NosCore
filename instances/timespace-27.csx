public class TimeSpace27 : TimeSpaceScript
{
    public TimeSpace27() : base(27, "Chasse aux monstres : 1er volet")
    {
        Levels(37, 99);
        Start(11, 11);
        Requires(1012, 3);
        Lives(3);
        Clock(TimeSpan.FromSeconds(720));
        Draw(1047, 2);
        Draw(1072, 1);
        Special(2039, 15);
        Reward(2134, 1);
        Reward(1087, 1);

        var entrance = Room(4202, 1, 9);
        var hunt = Room(2101, 2, 9);
        var den = Room(4209, 3, 9);
        var stand = Room(2101, 3, 10);
        var lair = Room(4208, 2, 10);

        entrance.Portal(new(28, 15), hunt, new(5, 20), locked: false);
        entrance.OnEnter(r => r.SayToRoom(Game18NConstString.MissionStartWhenEnterFirstRoom));

        hunt.Portal(new(35, 20), den, new(1, 15), locked: false);
        hunt.Portal(new(5, 20), entrance, new(28, 15), locked: false);
        hunt.OnEnter(r => r
                .StartRoomClock(TimeSpan.FromSeconds(180))
                .Objective(LanguageKey.TIMESPACE_GOAL_HUNT_FOR_THREE_MINUTES)
                .SayToRoom(Game18NConstString.EnemiesReinforcements)
                .Spawn(Monster(16).Anywhere().Count(2), Monster(65).Anywhere().Count(2))
                .After(TimeSpan.FromSeconds(30), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(Monster(16).Anywhere().Count(2), Monster(65).Anywhere().Count(2)))
                .After(TimeSpan.FromSeconds(60), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(
                        Monster(16).Anywhere().Count(2),
                        Monster(58).Anywhere().Count(3),
                        Monster(65).Anywhere().Count(2),
                        Monster(198).Anywhere().Count(5)))
                .After(TimeSpan.FromSeconds(90), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(
                        Monster(16).Anywhere().Count(2),
                        Monster(58).Anywhere().Count(3),
                        Monster(65).Anywhere().Count(2)))
                .After(TimeSpan.FromSeconds(120), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(
                        Monster(16).Anywhere().Count(2),
                        Monster(58).Anywhere().Count(3),
                        Monster(65).Anywhere().Count(2),
                        Monster(142).Anywhere().Count(2),
                        Monster(198).Anywhere().Count(5)))
                .After(TimeSpan.FromSeconds(150), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(Monster(58).Anywhere().Count(3), Monster(142).Anywhere().Count(2))))
            // The one run that was in the room as its clock ran out went from about 540 to 840 seconds left;
            // the run that was elsewhere at that moment got nothing.
            .OnClockOut(r => r
                .SayToRoom(Game18NConstString.QuickMissionComplete)
                .IfAnyoneInside(r => r.AddTime(TimeSpan.FromSeconds(300))));

        var denToHunt = den.Portal(new(1, 15), hunt, new(35, 20));
        var denToStand = den.Portal(new(14, 28), stand, new(20, 5));
        den.Spawn(
            Monster(22).At(16, 17),
            Monster(22).At(16, 10).Bonus(),
            Monster(137).At(11, 18).OnDeath(r => r.Spawn(
                Monster(142).At(16, 20).OnDeath(r => r.Spawn(Monster(67).At(21, 18))))),
            Monster(137).At(11, 13).Bonus().OnDeath(r => r.Spawn(
                Monster(142).At(19, 14).OnDeath(r => r.Spawn(Monster(142).At(13, 10), Monster(67).At(21, 11))))));
        den.OnEnter(r => r.Objective(LanguageKey.TIMESPACE_GOAL_KILL_ALL_MONSTERS))
            .OnCleared(r => r
                .Unlock(denToHunt, denToStand)
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .SayToRoom(Game18NConstString.QuickMissionComplete));

        var standToDen = stand.Portal(new(20, 5), den, new(14, 28));
        var standToLair = stand.Portal(new(5, 20), lair, new(28, 15));
        stand.OnEnter(r => r
                .StartRoomClock(TimeSpan.FromSeconds(180))
                .Objective(LanguageKey.TIMESPACE_GOAL_SURVIVE_THREE_MINUTES_DOOR)
                .SayToRoom(Game18NConstString.EnemiesReinforcements)
                .Spawn(StandWave())
                .After(TimeSpan.FromSeconds(30), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(StandWave()))
                .After(TimeSpan.FromSeconds(60), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(StandWave(Monster(67).Anywhere(), Monster(198).Anywhere().Count(5))))
                .After(TimeSpan.FromSeconds(90), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(StandWave(Monster(67).Anywhere())))
                .After(TimeSpan.FromSeconds(120), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(StandWave(Monster(67).Anywhere(), Monster(198).Anywhere().Count(5))))
                .After(TimeSpan.FromSeconds(150), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(Monster(56).Anywhere().Count(4), Monster(67).Anywhere())))
            .OnClockOut(r => r
                .Dismiss(22, 56, 59, 67, 137, 198)
                .Unlock(standToDen, standToLair)
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .SayToRoom(Game18NConstString.QuickMissionComplete));

        lair.Portal(new(28, 15), stand, new(5, 20), locked: false);
        lair.Exit(new(14, 28), locked: false);
        lair.Spawn(
            Monster(142).At(18, 9).OnDeath(r => r.Spawn(
                Monster(59).At(12, 18).OnDeath(r => r.Spawn(
                    Monster(198).At(11, 13),
                    Monster(198).At(23, 18).Bonus(),
                    Monster(198).At(13, 23))))),
            Monster(59).At(15, 16).OnDeath(r => r.Spawn(
                Monster(142).At(15, 19).Bonus().OnDeath(r => r.Spawn(Monster(198).At(11, 9))),
                Monster(142).At(21, 14))),
            Monster(59).At(8, 9));
    }

    // Every wave of the held room brings these; their cells differ from run to run.
    private static IMonster[] StandWave(params IMonster[] more) =>
    [
        Monster(22).Anywhere().Count(2),
        Monster(56).Anywhere().Count(4),
        Monster(59).Anywhere().Count(3),
        Monster(137).Anywhere().Count(2),
        .. more
    ];
}

return new TimeSpace27();
