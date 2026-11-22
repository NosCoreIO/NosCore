public class TimeSpace24 : TimeSpaceScript
{
    public TimeSpace24() : base(24, "La lutte pour Fernon")
    {
        Levels(34, 99);
        Start(11, 11);
        Requires(1012, 3);
        Lives(2);
        Clock(TimeSpan.FromSeconds(400));
        Draw(164, 1);
        Draw(170, 1);
        Draw(176, 1);
        Special(2047, 15);
        Reward(2133, 1);
        Reward(1087, 1);

        var entrance = Room(4202, 0, 8);
        var battlefield = Room(2100, 1, 8);
        var camp = Room(2100, 2, 8);
        var way = Room(4205, 3, 8);

        entrance.Portal(new(28, 15), battlefield, new(9, 36), locked: false);
        entrance.OnEnter(r => r.SayToRoom(Game18NConstString.MissionStartWhenEnterFirstRoom));

        // The miners that come in threes between the waves are called by the Chief Miners' own
        // Summon Miner skills, which the monsters cast themselves.
        var battlefieldToCamp = battlefield.Portal(new(58, 37), camp, new(9, 36));
        battlefield.Portal(new(9, 36), entrance, new(28, 15));
        battlefield.Spawn(Npc(310).At(22, 40), Npc(310).At(22, 37), Npc(310).At(22, 34), Npc(310).At(22, 31), Npc(310).At(22, 27), Npc(310).At(22, 24), Npc(310).At(22, 20), Npc(312).At(19, 21), Npc(312).At(18, 45), Npc(312).At(18, 41));
        battlefield.OnEnter(r => r.Dialog(6119))
            .OnDialogClosed(6119, r => r
                .Spawn(
                    Monster(112).At(39, 29),
                    Monster(20).At(34, 51),
                    Monster(20).At(34, 47),
                    Monster(20).At(34, 44),
                    Monster(20).At(34, 39),
                    Monster(20).At(34, 35),
                    Monster(20).At(34, 32),
                    Monster(20).At(34, 29),
                    Monster(20).At(34, 25),
                    Monster(20).At(34, 22),
                    Monster(20).At(34, 19))
                .Objective(LanguageKey.TIMESPACE_GOAL_FLEE_AFTER_FOUR_MINUTES)
                .StartRoomClock(TimeSpan.FromSeconds(240))
                .Spawn(
                    Monster(110).At(47, 22),
                    Monster(110).At(10, 23),
                    Monster(112).At(31, 17),
                    Monster(112).At(13, 29))
                .SayToRoom(Game18NConstString.EnemiesReinforcements)
                .After(TimeSpan.FromSeconds(30), r => r
                    .Spawn(
                        Monster(110).At(41, 40),
                        Monster(110).At(46, 20),
                        Monster(112).At(55, 21),
                        Monster(112).At(49, 12))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(60), r => r
                    .Spawn(
                        Monster(110).At(17, 57),
                        Monster(110).At(19, 51),
                        Monster(112).At(38, 30),
                        Monster(112).At(31, 59),
                        Monster(113).At(47, 33))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(90), r => r
                    .Spawn(
                        Monster(110).At(24, 29),
                        Monster(110).At(45, 54),
                        Monster(112).At(48, 42),
                        Monster(112).At(22, 59),
                        Monster(113).At(47, 33))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(120), r => r
                    .Spawn(
                        Monster(110).At(27, 35),
                        Monster(110).At(28, 17),
                        Monster(112).At(56, 13),
                        Monster(112).At(30, 13),
                        Monster(110).At(43, 19),
                        Monster(113).At(47, 33),
                        Monster(113).At(47, 46))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(150), r => r
                    .Spawn(
                        Monster(110).At(18, 20),
                        Monster(110).At(57, 13),
                        Monster(112).At(39, 32),
                        Monster(112).At(52, 16),
                        Monster(110).At(43, 19),
                        Monster(113).At(47, 33),
                        Monster(113).At(47, 46))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(180), r => r
                    .Spawn(
                        Monster(110).At(51, 20),
                        Monster(110).At(45, 43),
                        Monster(112).At(39, 43),
                        Monster(112).At(15, 26),
                        Monster(110).At(43, 19),
                        Monster(113).At(47, 33),
                        Monster(113).At(47, 46))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements))
                .After(TimeSpan.FromSeconds(210), r => r
                    .Spawn(
                        Monster(112).At(36, 52),
                        Monster(112).At(22, 12),
                        Monster(110).At(43, 19),
                        Monster(113).At(47, 33),
                        Monster(113).At(47, 46))
                    .SayToRoom(Game18NConstString.EnemiesReinforcements)))
            .OnClockOut(r => r
                .Dismiss(20, 106, 107, 109, 110, 112, 113)
                .Unlock(battlefieldToCamp)
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .Dialog(6120));

        var campToWay = camp.Portal(new(58, 37), way, new(1, 15));
        camp.Portal(new(9, 36), battlefield, new(58, 37));
        camp.Spawn(Npc(254).At(39, 39), Npc(254).At(39, 35), Npc(254).At(39, 31), Npc(254).At(39, 26), Npc(254).At(38, 43), Npc(254).At(35, 23), Npc(254).At(33, 40), Npc(254).At(33, 25), Npc(254).At(32, 37), Npc(254).At(32, 29), Npc(338).At(28, 26), Npc(338).At(28, 23), Npc(338).At(27, 35), Npc(338).At(27, 31));
        camp.OnEnter(r => r.Dialog(6121))
            .OnDialogClosed(6121, r => r
                .Spawn(
                    Monster(112).At(37, 41).OnDeath(r => r.Spawn(Monster(110).At(35, 41), Monster(110).At(36, 32))),
                    Monster(112).At(37, 38).OnDeath(r => r.Spawn(Monster(110).At(33, 31), Monster(110).At(35, 26).Bonus())),
                    Monster(112).At(37, 35).Bonus().OnDeath(r => r.Spawn(
                        Monster(110).At(33, 35).OnDeath(r => r.Spawn(Monster(110).At(36, 28))))),
                    Monster(112).At(37, 31),
                    Monster(112).At(37, 27),
                    Monster(110).At(34, 38),
                    Monster(110).At(34, 34).OnDeath(r => r.Spawn(Monster(110).At(35, 36))),
                    Monster(110).At(34, 29))
                .Objective(LanguageKey.TIMESPACE_GOAL_CUARRY_KNIGHTS_AND_DEMONS))
            .OnCleared(r => r
                .Unlock(campToWay)
                .SayToRoom(Game18NConstString.ADoorHasOpened)
                .SayToRoom(LanguageKey.TIMESPACE_DEMONS_CRUSH_KOVOLTS));

        way.Exit(new(28, 15), locked: false);
        way.Portal(new(1, 15), camp, new(58, 37), locked: false);
        way.Spawn(Npc(310).At(14, 10), Npc(310).At(14, 20));
        way.OnEnter(r => r.Dialog(6122));
    }
}

return new TimeSpace24();
