public class TimeSpace26 : TimeSpaceScript
{
    public TimeSpace26() : base(26, "La bataille des d�mons")
    {
        Levels(36, 99);
        Start(9, 10);
        Requires(1012, 3);
        Lives(2);
        Clock(TimeSpan.FromSeconds(230));
        Draw(137, 1);
        Draw(144, 1);
        Draw(151, 1);
        Special(2035, 15);
        Reward(2133, 1);
        Reward(1087, 1);

        var entrance = Room(5203, 0, 8);
        var battlefield = Room(5201, 1, 8);
        var passage = Room(5208, 1, 9);
        var hall = Room(5204, 2, 9);
        var corner = Room(5207, 3, 9);
        var approach = Room(5208, 3, 10);
        var stand = Room(5201, 4, 10);

        entrance.Portal(new(23, 12), battlefield, new(1, 27), locked: false);
        entrance.OnEnter(r => r.SayToRoom(Game18NConstString.MissionStartWhenEnterFirstRoom));

        var battlefieldToPassage = battlefield.Portal(new(25, 48), passage, new(12, 1));
        battlefield.Portal(new(1, 27), entrance, new(23, 12));
        battlefield.Spawn(Npc(311).At(25, 34), Npc(311).At(22, 45), Npc(311).At(22, 42), Npc(311).At(22, 39), Npc(311).At(22, 36), Npc(311).At(22, 33), Npc(311).At(22, 29), Npc(311).At(22, 26), Npc(311).At(20, 45), Npc(311).At(20, 42), Npc(311).At(20, 39), Npc(311).At(20, 36), Npc(311).At(20, 33), Npc(311).At(20, 29), Npc(311).At(20, 26));
        battlefield.OnEnter(r => r.Dialog(6125))
            .OnDialogClosed(6125, r => r.Spawn(
                Monster(151).At(31, 42).Bonus(),
                Monster(145).At(31, 26),
                Monster(151).At(24, 46),
                Monster(145).At(24, 19).OnDeath(r => r.Spawn(
                    Monster(148).At(30, 36).OnDeath(r => r.Spawn(
                        Monster(145).At(28, 22),
                        Monster(151).At(28, 46).OnDeath(r => r.Spawn(
                            Monster(145).At(30, 37),
                            Monster(145).At(30, 43),
                            Monster(145).At(31, 47))),
                        Monster(148).At(30, 31))))),
                Monster(145).At(30, 16).Bonus(),
                Monster(145).At(30, 25),
                Monster(145).At(30, 32)))
            .OnCleared(r => r
                .Unlock(battlefieldToPassage)
                .AddTime(TimeSpan.FromSeconds(240))
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .Dialog(6126));

        passage.Portal(new(12, 1), battlefield, new(25, 48), locked: false);
        passage.Portal(new(23, 12), hall, new(1, 12), locked: false);
        passage.Spawn(Monster(145).At(11, 5).OnDeath(r => r.Spawn(Monster(145).At(14, 6), Monster(145).At(14, 10).Bonus())));
        passage.Spawn(Npc(311).At(21, 12), Npc(311).At(15, 13), Npc(311).At(17, 8), Npc(311).At(16, 15), Npc(311).At(18, 10));

        hall.Portal(new(23, 12), corner, new(1, 12), locked: false);
        hall.Portal(new(1, 12), passage, new(23, 12), locked: false);
        hall.Spawn(
            Monster(148).At(19, 11).OnDeath(r => r.Spawn(Monster(148).At(20, 14).Bonus())),
            Monster(148).At(13, 13));

        corner.Portal(new(12, 23), approach, new(12, 1), locked: false);
        corner.Portal(new(1, 12), hall, new(23, 12), locked: false);
        corner.Spawn(
            Monster(145).At(8, 13).OnDeath(r => r.Spawn(Monster(148).At(10, 17))),
            Monster(145).At(10, 10).Bonus().OnDeath(r => r.Spawn(Monster(148).At(10, 18))));

        approach.Portal(new(12, 1), corner, new(12, 23), locked: false);
        approach.Portal(new(23, 12), stand, new(1, 27), locked: false);
        approach.Spawn(
            Monster(151).At(21, 13).Bonus().OnDeath(r => r.Spawn(Monster(145).At(17, 8))),
            Monster(148).At(11, 9),
            Monster(145).At(12, 10).OnDeath(r => r.Spawn(Monster(151).At(17, 14))));

        var way = stand.Exit(new(48, 25));
        stand.Portal(new(1, 27), approach, new(23, 12));
        stand.Spawn(Npc(312).At(32, 15), Npc(311).At(25, 6), Npc(312).At(23, 44), Npc(312).At(23, 39), Npc(312).At(23, 34), Npc(312).At(23, 26), Npc(311).At(21, 26), Npc(311).At(20, 44), Npc(311).At(20, 34), Npc(311).At(20, 29), Npc(311).At(19, 38), Npc(312).At(17, 22));
        stand.OnEnter(r => r.Dialog(6127).SayToRoom(Game18NConstString.EnemiesReinforcements))
            .OnDialogClosed(6127, r => r
                .StartRoomClock(TimeSpan.FromSeconds(180))
                .Objective(LanguageKey.TIMESPACE_GOAL_MONSTERS_VANISH_IN_THREE_MINUTES)
                .Spawn(Allies())
                .Spawn(
                    Monster(145).At(32, 33),
                    Monster(145).At(31, 31),
                    Monster(148).At(31, 45),
                    Monster(148).At(32, 37),
                    Monster(145).At(33, 20))
                .After(TimeSpan.FromSeconds(30), r => r
                    .Spawn(Allies())
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(
                        Monster(145).At(31, 31),
                        Monster(148).At(31, 45),
                        Monster(148).At(32, 37),
                        Monster(145).At(33, 20)))
                .After(TimeSpan.FromSeconds(60), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(LateWave()))
                .After(TimeSpan.FromSeconds(90), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .Spawn(LateWave()))
                .After(TimeSpan.FromSeconds(120), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .SayToRoom(Game18NConstString.ReinforcementsArrived)
                    .Spawn(
                        Monster(146).At(30, 38),
                        Monster(145).At(31, 25),
                        Monster(146).At(31, 28),
                        Monster(145).At(31, 31),
                        Monster(148).At(31, 41),
                        Monster(148).At(31, 45),
                        Monster(148).At(32, 37),
                        Monster(145).At(33, 20)))
                .After(TimeSpan.FromSeconds(150), r => r
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)
                    .SayToRoom(Game18NConstString.ReinforcementsArrived)
                    .Spawn(
                        Monster(146).At(30, 38),
                        Monster(145).At(31, 25),
                        Monster(146).At(31, 28),
                        Monster(145).At(31, 31),
                        Monster(148).At(31, 41),
                        Monster(148).At(31, 45),
                        Monster(145).At(33, 20))))
            .OnClockOut(r => r
                .Dismiss(145, 146, 148)
                .Unlock(way)
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .Dialog(6128));
    }

    private static INpc[] Allies() =>
    [
        Npc(253).At(30, 27),
        Npc(253).At(31, 27),
        Npc(253).At(31, 46),
        Npc(253).At(32, 25),
        Npc(253).At(34, 23),
        Npc(253).At(36, 23),
        Npc(253).At(36, 31),
        Npc(253).At(37, 21),
    ];

    private static IMonster[] LateWave() =>
    [
        Monster(146).At(30, 38),
        Monster(146).At(31, 28),
        Monster(145).At(31, 31),
        Monster(148).At(31, 41),
        Monster(148).At(31, 45),
        Monster(148).At(32, 37),
        Monster(145).At(33, 20),
    ];
}

return new TimeSpace26();
