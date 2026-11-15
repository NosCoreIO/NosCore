public class TimeSpace11 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace11()
    {
        Definition = InstanceDefinitionBuilder.Named(11, "Lord Cuarry's Knights", "Lord Cuarry's Knights")
            .ForLevels(21, 99)
            .StartingAt(9, 12)
            .Requiring(1012, 2)
            .WithDraw(156, 1)
            .WithDraw(158, 1)
            .WithDraw(160, 1)
            .WithSpecialReward(1018, 3)
            .WithReward(1027, 1)
            .WithReward(1087, 1)
            .WithRoom(5206, out _room1, 2, 8)
            .WithRoom(5206, out _room2, 2, 9)
            .WithRoom(5201, out _room3, 2, 10)
            .WithRoom(5204, out _room4, 3, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(500));
        r.SpawnPortal(_room1, new(12, 23), _room2, new(12, 1), locked: false);
        r.SpawnPortal(_room2, new(12, 1), _room1, new(12, 23), locked: false);
        r.SpawnPortal(_room2, new(12, 23), _room3, new(25, 1), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(322, new(16, 9)), new NpcSpawn(320, new(10, 4)), new NpcSpawn(322, new(14, 18)), new NpcSpawn(320, new(14, 16)), new NpcSpawn(337, new(14, 12)), new NpcSpawn(320, new(10, 16)), new NpcSpawn(322, new(9, 18)));
        r.SpawnPortal(_room3, new(25, 1), _room2, new(12, 23));
        r.SpawnPortal(_room3, new(48, 25), _room4, new(1, 12));
        r.Summon(_room3,
            new MonsterSpawn(18, new(28, 28)),
            new MonsterSpawn(18, new(26, 28)),
            new MonsterSpawn(18, new(24, 28)),
            new MonsterSpawn(18, new(22, 28)),
            new MonsterSpawn(18, new(19, 18)),
            new MonsterSpawn(18, new(17, 18)));
        r.SummonNpcs(_room3, new NpcSpawn(320, new(31, 37)), new NpcSpawn(320, new(28, 37)), new NpcSpawn(320, new(27, 47)), new NpcSpawn(320, new(26, 47)), new NpcSpawn(320, new(26, 37)), new NpcSpawn(320, new(25, 47)), new NpcSpawn(320, new(22, 46)), new NpcSpawn(320, new(23, 40)), new NpcSpawn(320, new(23, 37)), new NpcSpawn(320, new(22, 47)), new NpcSpawn(320, new(21, 47)), new NpcSpawn(320, new(20, 37)), new NpcSpawn(320, new(19, 47)), new NpcSpawn(320, new(18, 37)));
        r.SpawnExit(_room4, new(23, 12));
        r.SpawnPortal(_room4, new(1, 12), _room3, new(48, 25));
        r.Summon(_room4,
            new MonsterSpawn(109, new(20, 14)),
            new MonsterSpawn(109, new(20, 12)),
            new MonsterSpawn(109, new(20, 9)));
        r.SummonNpcs(_room4, new NpcSpawn(320, new(8, 13)), new NpcSpawn(320, new(8, 10)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6062);
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(300));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room3,
                new MonsterSpawn(108, new(18, 21)),
                new MonsterSpawn(105, new(18, 24)),
                new MonsterSpawn(108, new(19, 21)),
                new MonsterSpawn(105, new(21, 21)),
                new MonsterSpawn(106, new(22, 18)),
                new MonsterSpawn(105, new(22, 24)),
                new MonsterSpawn(105, new(23, 21)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(210), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(240), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(270), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(108, new(18, 21)),
                    new MonsterSpawn(105, new(18, 24)),
                    new MonsterSpawn(108, new(19, 21)),
                    new MonsterSpawn(105, new(21, 21)),
                    new MonsterSpawn(106, new(22, 18)),
                    new MonsterSpawn(105, new(22, 24)),
                    new MonsterSpawn(105, new(23, 21)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(300), r =>
            {
                r.Dismiss(_room3, 18);
                r.Dismiss(_room3, 105);
                r.Dismiss(_room3, 106);
                r.Dismiss(_room3, 108);
                r.UnlockPortal(_room3, _room4);
                r.UnlockPortal(_room3, _room2);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Dialog(_room3, 6064);
                r.StopRoomClock(_room3);
            });
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6065);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room4)
        {
            r.UnlockExit(_room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6066);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace11();
