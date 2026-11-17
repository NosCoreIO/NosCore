public class TimeSpace32 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace32()
    {
        Definition = InstanceDefinitionBuilder.Named(32, "Un lieu sinistre pour la chasse aux monstres", "Un lieu sinistre pour la chasse aux monstres")
            .ForLevels(42, 99)
            .StartingAt(13, 12)
            .Requiring(1012, 4)
            .WithLives(3)
            .WithDraw(704, 1)
            .WithDraw(1028, 2)
            .WithSpecialReward(1013, 20)
            .WithReward(2028, 5)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 2, 9)
            .WithRoom(4200, out _room2, 3, 9)
            .WithRoom(2100, out _room3, 4, 9)
            .WithRoom(2101, out _room4, 4, 8)
            .WithRoom(2101, out _room5, 3, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(850));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(9, 36), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room5, new(20, 35));
        r.SpawnExit(_room2, new(14, 28));
        r.SpawnPortal(_room3, new(9, 36), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(34, 11), _room4, new(20, 35), locked: false);
        r.SpawnPortal(_room4, new(20, 35), _room3, new(34, 11));
        r.SpawnPortal(_room4, new(5, 20), _room5, new(35, 20));
        r.PlaceButton(_room4, 1, new(20, 18), 1000, 1045);
        r.SpawnPortal(_room5, new(35, 20), _room4, new(5, 20), locked: false);
        r.SpawnPortal(_room5, new(20, 35), _room2, new(14, 1));
        r.PlaceButton(_room5, 1, new(20, 19), 1000, 1045);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Message(_room2, LanguageKey.TIMESPACE_HUNT_UNTIL_BOTTOM_EXIT_OPENS);
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(300));
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_GO_HUNTING_FIVE_MINUTES);
            r.Message(_room3, Game18NConstString.EnemiesReinforcements);
            r.Summon(_room3,
                new MonsterSpawn(82, new(25, 23)),
                new MonsterSpawn(82, new(19, 27)),
                new MonsterSpawn(103, new(54, 59)),
                new MonsterSpawn(103, new(19, 48)),
                new MonsterSpawn(138, new(46, 49)),
                new MonsterSpawn(138, new(40, 17)),
                new MonsterSpawn(138, new(58, 33)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(33, 51)),
                    new MonsterSpawn(82, new(23, 38)),
                    new MonsterSpawn(103, new(46, 56)),
                    new MonsterSpawn(103, new(42, 39)),
                    new MonsterSpawn(138, new(38, 36)),
                    new MonsterSpawn(138, new(49, 33)),
                    new MonsterSpawn(138, new(32, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(31, 22)),
                    new MonsterSpawn(82, new(35, 46)),
                    new MonsterSpawn(103, new(40, 15)),
                    new MonsterSpawn(103, new(12, 18)),
                    new MonsterSpawn(138, new(32, 37)),
                    new MonsterSpawn(138, new(12, 26)),
                    new MonsterSpawn(138, new(10, 38)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(36, 14)),
                    new MonsterSpawn(82, new(21, 18)),
                    new MonsterSpawn(103, new(58, 32)),
                    new MonsterSpawn(103, new(11, 37)),
                    new MonsterSpawn(138, new(36, 56)),
                    new MonsterSpawn(138, new(46, 23)),
                    new MonsterSpawn(138, new(23, 30)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(50, 48)),
                    new MonsterSpawn(82, new(57, 23)),
                    new MonsterSpawn(103, new(33, 45)),
                    new MonsterSpawn(103, new(45, 15)),
                    new MonsterSpawn(138, new(36, 47)),
                    new MonsterSpawn(138, new(41, 57)),
                    new MonsterSpawn(138, new(53, 23)),
                    new MonsterSpawn(377, new(31, 56)),
                    new MonsterSpawn(377, new(21, 43)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(28, 29)),
                    new MonsterSpawn(82, new(40, 55)),
                    new MonsterSpawn(103, new(40, 32)),
                    new MonsterSpawn(103, new(19, 32)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Message(_room3, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3,
                    new MonsterSpawn(82, new(33, 50)),
                    new MonsterSpawn(82, new(42, 39)),
                    new MonsterSpawn(103, new(9, 59)),
                    new MonsterSpawn(103, new(32, 38)),
                    new MonsterSpawn(377, new(27, 43)),
                    new MonsterSpawn(377, new(28, 33)),
                    new MonsterSpawn(377, new(56, 45)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(300), r =>
            {
                r.StopRoomClock(_room3);
                r.Message(_room3, LanguageKey.TIMESPACE_HUNTING_TIME_OVER);
            });
        }
        else if (room == _room4)
        {
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(200));
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_GO_HUNTING_THREE_MINUTES);
            r.Message(_room4, Game18NConstString.EnemiesReinforcements);
            r.Summon(_room4,
                new MonsterSpawn(73, new(21, 17)),
                new MonsterSpawn(73, new(15, 5)),
                new MonsterSpawn(377, new(11, 13)),
                new MonsterSpawn(377, new(34, 20)),
                new MonsterSpawn(377, new(18, 35)),
                new MonsterSpawn(377, new(18, 30)),
                new MonsterSpawn(377, new(33, 23)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(26, 8)),
                    new MonsterSpawn(73, new(12, 8)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(13, 26)),
                    new MonsterSpawn(73, new(22, 8)),
                    new MonsterSpawn(377, new(24, 17)),
                    new MonsterSpawn(377, new(13, 18)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(10, 9)),
                    new MonsterSpawn(73, new(28, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(12, 21)),
                    new MonsterSpawn(73, new(24, 14)),
                    new MonsterSpawn(138, new(21, 16)),
                    new MonsterSpawn(138, new(32, 24)),
                    new MonsterSpawn(138, new(18, 21)),
                    new MonsterSpawn(138, new(17, 17)),
                    new MonsterSpawn(138, new(31, 18)),
                    new MonsterSpawn(377, new(22, 26)),
                    new MonsterSpawn(377, new(8, 25)),
                    new MonsterSpawn(377, new(18, 9)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(28, 24)),
                    new MonsterSpawn(73, new(29, 34)),
                    new MonsterSpawn(138, new(23, 16)),
                    new MonsterSpawn(138, new(29, 13)),
                    new MonsterSpawn(138, new(29, 29)),
                    new MonsterSpawn(138, new(17, 13)),
                    new MonsterSpawn(138, new(15, 7)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Message(_room4, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4,
                    new MonsterSpawn(73, new(21, 27)),
                    new MonsterSpawn(73, new(9, 24)),
                    new MonsterSpawn(138, new(23, 10)),
                    new MonsterSpawn(138, new(21, 15)),
                    new MonsterSpawn(138, new(19, 12)),
                    new MonsterSpawn(138, new(10, 13)),
                    new MonsterSpawn(138, new(11, 9)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(200), r =>
            {
                r.StopRoomClock(_room4);
                r.Dismiss(_room4, 73);
                r.Dismiss(_room4, 138);
                r.Dismiss(_room4, 377);
                r.UnlockPortal(_room4, _room3);
                r.UnlockPortal(_room4, _room5);
                r.Message(_room4, Game18NConstString.ADoorHasOpened);
                r.Message(_room4, LanguageKey.TIMESPACE_MONSTERS_VANISH_EXIT_OPENS);
            });
        }
        else if (room == _room5)
        {
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(300));
            r.Objective(_room5, LanguageKey.TIMESPACE_GOAL_HUNT_AS_MANY_FIVE_MINUTES);
            r.Message(_room5, Game18NConstString.EnemiesReinforcements);
            r.Summon(_room5,
                new MonsterSpawn(60, new(18, 15)),
                new MonsterSpawn(60, new(29, 23)),
                new MonsterSpawn(103, new(33, 16)),
                new MonsterSpawn(103, new(23, 22)),
                new MonsterSpawn(138, new(9, 15)),
                new MonsterSpawn(138, new(16, 14)),
                new MonsterSpawn(138, new(13, 10)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(35, 23)),
                    new MonsterSpawn(60, new(7, 15)),
                    new MonsterSpawn(103, new(24, 20)),
                    new MonsterSpawn(103, new(13, 18)),
                    new MonsterSpawn(138, new(29, 9)),
                    new MonsterSpawn(138, new(9, 30)),
                    new MonsterSpawn(138, new(29, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(21, 11)),
                    new MonsterSpawn(60, new(33, 26)),
                    new MonsterSpawn(103, new(22, 13)),
                    new MonsterSpawn(103, new(13, 11)),
                    new MonsterSpawn(138, new(21, 31)),
                    new MonsterSpawn(138, new(26, 10)),
                    new MonsterSpawn(138, new(35, 22)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(16, 25)),
                    new MonsterSpawn(60, new(12, 21)),
                    new MonsterSpawn(103, new(31, 11)),
                    new MonsterSpawn(103, new(29, 31)),
                    new MonsterSpawn(138, new(28, 21)),
                    new MonsterSpawn(138, new(6, 15)),
                    new MonsterSpawn(138, new(25, 22)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(16, 22)),
                    new MonsterSpawn(60, new(15, 15)),
                    new MonsterSpawn(103, new(29, 24)),
                    new MonsterSpawn(103, new(29, 13)),
                    new MonsterSpawn(138, new(19, 14)),
                    new MonsterSpawn(138, new(31, 23)),
                    new MonsterSpawn(138, new(34, 10)),
                    new MonsterSpawn(377, new(28, 15)),
                    new MonsterSpawn(377, new(17, 27)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(18, 14)),
                    new MonsterSpawn(60, new(15, 31)),
                    new MonsterSpawn(103, new(23, 30)),
                    new MonsterSpawn(103, new(28, 19)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Message(_room5, Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5,
                    new MonsterSpawn(60, new(11, 20)),
                    new MonsterSpawn(60, new(31, 17)),
                    new MonsterSpawn(103, new(25, 33)),
                    new MonsterSpawn(103, new(21, 26)),
                    new MonsterSpawn(377, new(13, 21)),
                    new MonsterSpawn(377, new(27, 10)),
                    new MonsterSpawn(377, new(14, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(300), r =>
            {
                r.StopRoomClock(_room5);
                r.Message(_room5, LanguageKey.TIMESPACE_HUNTING_TIME_OVER);
                // One complete run only: the hub exit was open when the player came back after this clock ended.
                r.UnlockExit(_room2);
                r.Message(_room2, Game18NConstString.ADoorHasOpened);
            });
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace32();
