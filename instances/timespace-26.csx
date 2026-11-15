// The Battle of the Demons
public class TimeSpace26 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace26()
    {
        Definition = InstanceDefinitionBuilder.Named(26, "The Kenkos are attacking! Lord Cuarry is furious and sends all his knights to fight them. Who will win?", "The Battle of the Demons")
            .ForLevels(36, 99)
            .StartingAt(7, 10)
            .WithLives(1)
            .Requiring(1012, 3)
            .WithDraw(137, 1)
            .WithDraw(144, 1)
            .WithDraw(151, 1)
            .WithSpecialReward(2035, 15)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(5203, out _room1, 0, 8)
            .WithRoom(5201, out _room2, 0, 8)
            .WithRoom(5208, out _room3, 1, 8)
            .WithRoom(5204, out _room4, 1, 9)
            .WithRoom(5207, out _room5, 2, 9)
            .WithRoom(5208, out _room6, 3, 9)
            .WithRoom(5201, out _room7, 3, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(23, 12), _room2, new(1, 27), locked: false);
        r.SpawnPortal(_room2, new(1, 27), _room1, new(23, 12));
        r.SpawnPortal(_room2, new(25, 48), _room3, new(12, 1));
        r.Summon(_room2,
            new MonsterSpawn(145, new(24, 19), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(148, new(30, 36), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room2,
                    new MonsterSpawn(148, new(30, 31), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room2,
                        new MonsterSpawn(145, new(30, 32)),
                        new MonsterSpawn(145, new(30, 25), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room2, new MonsterSpawn(145, new(31, 47)));
                        }),
                        new MonsterSpawn(145, new(30, 16)));
                    }),
                    new MonsterSpawn(151, new(28, 46)),
                    new MonsterSpawn(145, new(28, 22), OnDeath: r =>
                    {
                        r.Summon(_room2,
                        new MonsterSpawn(145, new(30, 43)),
                        new MonsterSpawn(145, new(30, 37), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(151, new(24, 46)),
            new MonsterSpawn(145, new(31, 26), IsBonus: true),
            new MonsterSpawn(151, new(31, 42)));
        r.SpawnPortal(_room3, new(23, 12), _room4, new(1, 12), locked: false);
        r.SpawnPortal(_room3, new(12, 1), _room2, new(25, 48), locked: false);
        r.Summon(_room3, new MonsterSpawn(145, new(13, 8), OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(145, new(14, 10), IsBonus: true),
                new MonsterSpawn(145, new(14, 6), IsBonus: true));
            }));
        r.SpawnPortal(_room4, new(1, 12), _room3, new(23, 12), locked: false);
        r.SpawnPortal(_room4, new(23, 12), _room5, new(1, 12), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(148, new(12, 8)),
            new MonsterSpawn(148, new(22, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(148, new(20, 14), IsBonus: true));
            }));
        r.SpawnPortal(_room5, new(1, 12), _room4, new(23, 12), locked: false);
        r.SpawnPortal(_room5, new(12, 23), _room6, new(12, 1), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(145, new(9, 10), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(148, new(10, 17)));
            }),
            new MonsterSpawn(145, new(11, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(148, new(10, 18), IsBonus: true));
            }));
        r.SpawnPortal(_room6, new(23, 12), _room7, new(1, 27), locked: false);
        r.SpawnPortal(_room6, new(12, 1), _room5, new(12, 23), locked: false);
        r.Summon(_room6,
            new MonsterSpawn(145, new(10, 7)),
            new MonsterSpawn(148, new(13, 8), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(151, new(17, 14), IsBonus: true));
            }),
            new MonsterSpawn(151, new(17, 7), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(145, new(17, 8)));
            }));
        r.SpawnPortal(_room7, new(1, 27), _room6, new(23, 12));
        r.SpawnExit(_room7, new(48, 25));
        r.Summon(_room7,
            new MonsterSpawn(145, new(33, 20)),
            new MonsterSpawn(148, new(32, 37)),
            new MonsterSpawn(148, new(31, 45), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room7, new MonsterSpawn(145, new(33, 20), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room7, new MonsterSpawn(145, new(33, 20), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room7, new MonsterSpawn(145, new(33, 20), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.ReinforcementsArrived);
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room7, new MonsterSpawn(145, new(33, 20), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.ReinforcementsArrived);
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room7, new MonsterSpawn(145, new(33, 20)));
                                r.Summon(_room7, new MonsterSpawn(148, new(31, 45)));
                                r.Summon(_room7, new MonsterSpawn(148, new(31, 41)));
                                r.Summon(_room7, new MonsterSpawn(145, new(31, 31)));
                                r.Summon(_room7, new MonsterSpawn(146, new(31, 28)));
                                r.Summon(_room7, new MonsterSpawn(145, new(31, 25)));
                                r.Summon(_room7, new MonsterSpawn(146, new(30, 38)));
                            }));
                            r.Summon(_room7, new MonsterSpawn(148, new(32, 37)));
                            r.Summon(_room7, new MonsterSpawn(148, new(31, 45)));
                            r.Summon(_room7, new MonsterSpawn(148, new(31, 41)));
                            r.Summon(_room7, new MonsterSpawn(145, new(31, 31)));
                            r.Summon(_room7, new MonsterSpawn(146, new(31, 28)));
                            r.Summon(_room7, new MonsterSpawn(145, new(31, 25)));
                            r.Summon(_room7, new MonsterSpawn(146, new(30, 38)));
                        }));
                        r.Summon(_room7, new MonsterSpawn(148, new(32, 37)));
                        r.Summon(_room7, new MonsterSpawn(148, new(31, 45)));
                        r.Summon(_room7, new MonsterSpawn(148, new(31, 41)));
                        r.Summon(_room7, new MonsterSpawn(145, new(31, 31)));
                        r.Summon(_room7, new MonsterSpawn(146, new(31, 28)));
                        r.Summon(_room7, new MonsterSpawn(146, new(30, 38)));
                    }));
                    r.Summon(_room7, new MonsterSpawn(148, new(32, 37)));
                    r.Summon(_room7, new MonsterSpawn(148, new(31, 45)));
                    r.Summon(_room7, new MonsterSpawn(148, new(31, 41)));
                    r.Summon(_room7, new MonsterSpawn(145, new(31, 31)));
                    r.Summon(_room7, new MonsterSpawn(146, new(31, 28)));
                    r.Summon(_room7, new MonsterSpawn(146, new(30, 38)));
                }));
                r.Summon(_room7, new MonsterSpawn(148, new(32, 37)));
                r.Summon(_room7, new MonsterSpawn(148, new(31, 45)));
                r.Summon(_room7, new MonsterSpawn(145, new(31, 31)));
            }),
            new MonsterSpawn(145, new(31, 31)),
            new MonsterSpawn(145, new(32, 33), IsBonus: true));
        r.SummonNpcs(_room7, new NpcSpawn(253, new(33, 20)), new NpcSpawn(253, new(32, 37)), new NpcSpawn(253, new(31, 45)), new NpcSpawn(253, new(31, 41)), new NpcSpawn(253, new(31, 31)), new NpcSpawn(253, new(31, 28)), new NpcSpawn(253, new(31, 25)), new NpcSpawn(253, new(30, 38)), new NpcSpawn(253, new(33, 20)), new NpcSpawn(253, new(32, 37)), new NpcSpawn(253, new(31, 45)), new NpcSpawn(253, new(31, 41)), new NpcSpawn(253, new(31, 31)), new NpcSpawn(253, new(31, 28)), new NpcSpawn(253, new(31, 25)), new NpcSpawn(253, new(30, 38)), new NpcSpawn(312, new(17, 22)), new NpcSpawn(312, new(23, 26)), new NpcSpawn(312, new(23, 34)), new NpcSpawn(312, new(23, 39)), new NpcSpawn(312, new(23, 44)), new NpcSpawn(312, new(19, 32)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(230));
            r.Dialog(_room2, 6125);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Dialog(_room7, 6127);
            r.StartRoomClock(_room7, TimeSpan.FromSeconds(180));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.Dialog(_room2, 6126);
            r.AddClockTime(TimeSpan.FromSeconds(240));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room2, _room3);
        }
        else if (room == _room7)
        {
            r.StopRoomClock(_room7);
            r.Dialog(_room7, 6128);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockExit(_room7);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace26();
