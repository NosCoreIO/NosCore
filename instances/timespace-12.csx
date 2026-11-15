public class TimeSpace12 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace12()
    {
        Definition = InstanceDefinitionBuilder.Named(12, "Monster Fight", "Monster Fight")
            .ForLevels(22, 99)
            .StartingAt(10, 13)
            .Requiring(1012, 2)
            .WithLives(2)
            .WithDraw(2034, 2)
            .WithDraw(2042, 2)
            .WithDraw(2046, 2)
            .WithDraw(2038, 2)
            .WithSpecialReward(1027, 30)
            .WithReward(1027, 1)
            .WithReward(1087, 1)
            .WithRoom(4100, out _room1, 2, 8)
            .WithRoom(4102, out _room2, 2, 9)
            .WithRoom(2101, out _room3, 3, 9)
            .WithRoom(4112, out _room4, 4, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(250));
        r.SpawnPortal(_room1, new(14, 28), _room2, new(14, 1), locked: false);
        r.SpawnPortal(_room2, new(14, 1), _room1, new(14, 28), locked: false);
        r.SpawnPortal(_room2, new(28, 14), _room3, new(5, 20), locked: false);
        r.SpawnPortal(_room3, new(35, 20), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(5, 20), _room2, new(28, 14));
        r.Summon(_room3,
            new MonsterSpawn(19, new(33, 19)),
            new MonsterSpawn(19, new(33, 18)),
            new MonsterSpawn(19, new(33, 17)),
            new MonsterSpawn(19, new(33, 15)),
            new MonsterSpawn(19, new(33, 13)),
            new MonsterSpawn(19, new(33, 11)));
        r.SummonNpcs(_room3, new NpcSpawn(320, new(10, 25)), new NpcSpawn(322, new(10, 22)), new NpcSpawn(322, new(9, 20)), new NpcSpawn(320, new(9, 20)), new NpcSpawn(322, new(12, 17)), new NpcSpawn(322, new(10, 15)), new NpcSpawn(337, new(10, 14)), new NpcSpawn(320, new(10, 11)), new NpcSpawn(322, new(8, 23)), new NpcSpawn(322, new(8, 19)));
        r.SpawnExit(_room4, new(28, 14), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(35, 20), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6067);
        }
        else if (room == _room3)
        {
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(180));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room3,
                new MonsterSpawn(131, new(30, 22)),
                new MonsterSpawn(56, new(30, 25)),
                new MonsterSpawn(57, new(30, 26)),
                new MonsterSpawn(136, new(30, 28)),
                new MonsterSpawn(131, new(30, 33)));
            r.At(r.Elapsed + TimeSpan.FromSeconds(30), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(131, new(30, 22)),
                    new MonsterSpawn(56, new(30, 25)),
                    new MonsterSpawn(57, new(30, 26)),
                    new MonsterSpawn(136, new(30, 28)),
                    new MonsterSpawn(131, new(30, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(60), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(131, new(30, 15)),
                    new MonsterSpawn(56, new(30, 20)),
                    new MonsterSpawn(131, new(30, 22)),
                    new MonsterSpawn(56, new(30, 25)),
                    new MonsterSpawn(57, new(30, 26)),
                    new MonsterSpawn(136, new(30, 28)),
                    new MonsterSpawn(57, new(30, 30)),
                    new MonsterSpawn(131, new(30, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(90), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(131, new(30, 15)),
                    new MonsterSpawn(56, new(30, 20)),
                    new MonsterSpawn(56, new(30, 25)),
                    new MonsterSpawn(57, new(30, 30)),
                    new MonsterSpawn(131, new(30, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(136, new(30, 10)),
                    new MonsterSpawn(131, new(30, 15)),
                    new MonsterSpawn(56, new(30, 20)),
                    new MonsterSpawn(56, new(30, 25)),
                    new MonsterSpawn(57, new(30, 30)),
                    new MonsterSpawn(131, new(30, 33)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(150), r =>
            {
                r.Summon(_room3,
                    new MonsterSpawn(136, new(30, 10)),
                    new MonsterSpawn(56, new(30, 25)),
                    new MonsterSpawn(57, new(30, 30)));
            });
            r.At(r.Elapsed + TimeSpan.FromSeconds(180), r =>
            {
                r.Dismiss(_room3, 19);
                r.Dismiss(_room3, 56);
                r.Dismiss(_room3, 57);
                r.Dismiss(_room3, 131);
                r.Dismiss(_room3, 136);
                r.UnlockPortal(_room3, _room4);
                r.Message(Game18NConstString.ADoorHasOpened);
                r.Dialog(_room3, 6068);
                r.StopRoomClock(_room3);
            });
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6069);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace12();
