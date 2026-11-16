public class TimeSpace19 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace19()
    {
        Definition = InstanceDefinitionBuilder.Named(19, "The Black Knight", "The Black Knight")
            .ForLevels(29, 99)
            .StartingAt(9, 10)
            .Requiring(1012, 2)
            .WithLives(3)
            .WithDraw(136, 1)
            .WithDraw(143, 1)
            .WithDraw(150, 1)
            .WithSpecialReward(2129, 4)
            .WithReward(2131, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 8)
            .WithRoom(4205, out _room2, 1, 8)
            .WithRoom(4205, out _room3, 2, 8)
            .WithRoom(4205, out _room4, 3, 8)
            .WithRoom(4205, out _room5, 4, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(130));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15));
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15));
        r.Summon(_room2,
            new MonsterSpawn(12, new(12, 9)),
            new MonsterSpawn(12, new(11, 14)));
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(140, new(17, 7)),
            new MonsterSpawn(13, new(11, 9)));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15));
        r.Summon(_room4,
            new MonsterSpawn(140, new(18, 17)),
            new MonsterSpawn(100, new(15, 16)),
            new MonsterSpawn(141, new(16, 8)),
            new MonsterSpawn(66, new(13, 17)));
        r.SpawnExit(_room5, new(28, 15), locked: false);
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15), locked: false);
        r.SummonNpcs(_room5, new NpcSpawn(318, new(14, 13)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Dialog(_room2, 6101);
            r.Objective(_room2, LanguageKey.TIMESPACE_GOAL_SURVIVE_TWO_MINUTES);
            r.Summon(_room2,
                new MonsterSpawn(4, new(13, 6)),
                new MonsterSpawn(140, new(14, 9)),
                new MonsterSpawn(4, new(14, 22)),
                new MonsterSpawn(140, new(15, 18)),
                new MonsterSpawn(99, new(19, 18)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(4, new(13, 6)),
                    new MonsterSpawn(140, new(14, 9)),
                    new MonsterSpawn(4, new(14, 22)),
                    new MonsterSpawn(140, new(15, 18)),
                    new MonsterSpawn(99, new(19, 18)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(4, new(13, 6)),
                    new MonsterSpawn(140, new(14, 9)),
                    new MonsterSpawn(4, new(14, 22)),
                    new MonsterSpawn(140, new(15, 18)),
                    new MonsterSpawn(99, new(19, 18)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room2,
                    new MonsterSpawn(4, new(13, 6)),
                    new MonsterSpawn(140, new(14, 9)),
                    new MonsterSpawn(4, new(14, 22)),
                    new MonsterSpawn(140, new(15, 18)),
                    new MonsterSpawn(99, new(19, 18)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Dismiss(_room2, 4);
                r.Dismiss(_room2, 12);
                r.Dismiss(_room2, 99);
                r.Dismiss(_room2, 140);
                r.UnlockPortal(_room2, _room3);
                r.AddClockTime(TimeSpan.FromSeconds(70));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED);
                r.StopRoomClock(_room2);
            });
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Dialog(_room3, 6102);
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_SURVIVE_ONE_MINUTE);
            r.Summon(_room3,
                new MonsterSpawn(141, new(12, 18)),
                new MonsterSpawn(13, new(16, 12)),
                new MonsterSpawn(140, new(16, 17)),
                new MonsterSpawn(66, new(19, 14)),
                new MonsterSpawn(66, new(19, 20)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(141, new(12, 18)),
                    new MonsterSpawn(13, new(16, 12)),
                    new MonsterSpawn(140, new(16, 17)),
                    new MonsterSpawn(66, new(19, 14)),
                    new MonsterSpawn(66, new(19, 20)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Dismiss(_room3, 13);
                r.Dismiss(_room3, 66);
                r.Dismiss(_room3, 140);
                r.Dismiss(_room3, 141);
                r.UnlockPortal(_room3, _room4);
                r.AddClockTime(TimeSpan.FromSeconds(120));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED);
                r.StopRoomClock(_room3);
            });
        }
        else if (room == _room4)
        {
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(120));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_SURVIVE_TWO_MINUTES);
            r.Summon(_room4,
                new MonsterSpawn(66, new(12, 11)),
                new MonsterSpawn(141, new(13, 22)),
                new MonsterSpawn(100, new(15, 13)),
                new MonsterSpawn(140, new(16, 21)),
                new MonsterSpawn(140, new(17, 8)),
                new MonsterSpawn(140, new(19, 12)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(66, new(12, 11)),
                    new MonsterSpawn(141, new(13, 22)),
                    new MonsterSpawn(100, new(15, 13)),
                    new MonsterSpawn(140, new(16, 21)),
                    new MonsterSpawn(140, new(17, 8)),
                    new MonsterSpawn(140, new(19, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(66, new(12, 11)),
                    new MonsterSpawn(141, new(13, 22)),
                    new MonsterSpawn(100, new(15, 13)),
                    new MonsterSpawn(140, new(16, 21)),
                    new MonsterSpawn(140, new(17, 8)),
                    new MonsterSpawn(140, new(19, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room4,
                    new MonsterSpawn(66, new(12, 11)),
                    new MonsterSpawn(141, new(13, 22)),
                    new MonsterSpawn(100, new(15, 13)),
                    new MonsterSpawn(140, new(16, 21)),
                    new MonsterSpawn(140, new(17, 8)),
                    new MonsterSpawn(140, new(19, 12)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Dismiss(_room4, 66);
                r.Dismiss(_room4, 100);
                r.Dismiss(_room4, 140);
                r.Dismiss(_room4, 141);
                r.UnlockPortal(_room4, _room5);
                r.AddClockTime(TimeSpan.FromSeconds(10));
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Message(LanguageKey.TIMESPACE_MONSTERS_GONE_EXIT_OPENED);
                r.StopRoomClock(_room4);
            });
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6103);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace19();
