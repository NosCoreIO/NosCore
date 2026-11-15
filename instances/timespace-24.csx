// The Struggle for Fernon
public class TimeSpace24 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;

    public TimeSpace24()
    {
        Definition = InstanceDefinitionBuilder.Named(24, "Korrkoaren has taken the Fernon Temple after a fierce battle. This is no longer a strike, it is war! The NosVille guards are trying to hold the Kovolts back, but they won't last long. Hurry and help them!", "The Struggle for Fernon")
            .ForLevels(34, 99)
            .StartingAt(11, 10)
            .WithLives(1)
            .Requiring(1012, 3)
            .WithDraw(164, 1)
            .WithDraw(170, 1)
            .WithDraw(176, 1)
            .WithSpecialReward(2047, 15)
            .WithReward(2133, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 8)
            .WithRoom(2100, out _room2, 0, 8)
            .WithRoom(2100, out _room3, 1, 8)
            .WithRoom(4205, out _room4, 2, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 15), _room2, new(9, 36), locked: false);
        r.SpawnPortal(_room2, new(9, 36), _room1, new(28, 15));
        r.SpawnPortal(_room2, new(58, 37), _room3, new(9, 36));
        r.SummonNpcs(_room2, new NpcSpawn(312, new(18, 41)), new NpcSpawn(312, new(18, 45)), new NpcSpawn(312, new(19, 21)), new NpcSpawn(310, new(22, 20)), new NpcSpawn(310, new(22, 24)), new NpcSpawn(310, new(22, 27)), new NpcSpawn(310, new(22, 31)), new NpcSpawn(310, new(22, 34)), new NpcSpawn(310, new(22, 37)), new NpcSpawn(310, new(22, 40)));
        r.SpawnPortal(_room3, new(9, 36), _room2, new(58, 37));
        r.SpawnPortal(_room3, new(58, 37), _room4, new(1, 15));
        r.Summon(_room3,
            new MonsterSpawn(110, new(34, 29)),
            new MonsterSpawn(110, new(34, 34)),
            new MonsterSpawn(110, new(34, 38), IsBonus: true),
            new MonsterSpawn(112, new(37, 27), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(110, new(36, 28)));
            }),
            new MonsterSpawn(112, new(37, 31)),
            new MonsterSpawn(112, new(37, 35), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(110, new(35, 36), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(110, new(36, 32)),
                    new MonsterSpawn(110, new(35, 41), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.ADoorHasOpened);
                        r.UnlockPortal(_room3, _room4);
                    }));
                }));
            }),
            new MonsterSpawn(112, new(37, 38), IsBonus: true),
            new MonsterSpawn(112, new(37, 41)));
        r.SummonNpcs(_room3, new NpcSpawn(338, new(27, 31)), new NpcSpawn(338, new(27, 35)), new NpcSpawn(338, new(28, 23)), new NpcSpawn(338, new(28, 26)), new NpcSpawn(254, new(32, 29)), new NpcSpawn(254, new(32, 37)), new NpcSpawn(254, new(33, 25)), new NpcSpawn(254, new(33, 40)), new NpcSpawn(254, new(35, 23)), new NpcSpawn(254, new(38, 43)), new NpcSpawn(254, new(39, 26)), new NpcSpawn(254, new(39, 31)), new NpcSpawn(254, new(39, 35)), new NpcSpawn(254, new(39, 39)));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(58, 37), locked: false);
        r.SpawnExit(_room4, new(28, 15), locked: false);
        r.SummonNpcs(_room4, new NpcSpawn(310, new(12, 19)), new NpcSpawn(310, new(13, 11)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(400));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room2, new MonsterSpawn(112, new(14, 24)));
            r.Summon(_room2, new MonsterSpawn(112, new(20, 49)));
            r.Summon(_room2, new MonsterSpawn(110, new(30, 26)));
            r.Summon(_room2, new MonsterSpawn(110, new(26, 58)));
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(240));
            r.Summon(_room2,
                        new MonsterSpawn(20, new(34, 19), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room2, new MonsterSpawn(112, new(56, 31), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                r.Summon(_room2, new MonsterSpawn(112, new(45, 58), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room2, new MonsterSpawn(113, new(47, 46)));
                                    r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                    r.Summon(_room2, new MonsterSpawn(110, new(43, 19)));
                                    r.Summon(_room2, new MonsterSpawn(112, new(55, 32)));
                                    r.Summon(_room2, new MonsterSpawn(112, new(41, 14)));
                                    r.Summon(_room2, new MonsterSpawn(110, new(16, 57), OnDeath: r =>
                                    {
                                        r.Message(Game18NConstString.EnemiesReinforcements);
                                        r.Summon(_room2, new MonsterSpawn(113, new(47, 46), OnDeath: r =>
                                        {
                                            r.Message(Game18NConstString.EnemiesReinforcements);
                                            r.Summon(_room2, new MonsterSpawn(113, new(47, 46)));
                                            r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                            r.Summon(_room2, new MonsterSpawn(110, new(43, 19), OnDeath: r =>
                                            {
                                                r.Message(Game18NConstString.EnemiesReinforcements);
                                                r.Summon(_room2, new MonsterSpawn(113, new(47, 46)));
                                                r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                                r.Summon(_room2, new MonsterSpawn(110, new(43, 19)));
                                                r.Summon(_room2, new MonsterSpawn(112, new(16, 58)));
                                                r.Summon(_room2, new MonsterSpawn(112, new(34, 50), OnDeath: r =>
                                                {
                                                    r.Dismiss(_room2, 20);
                                                    r.Dismiss(_room2, 110);
                                                    r.Dismiss(_room2, 112);
                                                    r.Dismiss(_room2, 113);
                                                    r.StopRoomClock(_room2);
                                                    r.Dialog(_room2, 6120);
                                                    r.Message(Game18NConstString.ADoorHasOpened);
                                                    r.UnlockPortal(_room2, _room3);
                                                }));
                                            }));
                                            r.Summon(_room2, new MonsterSpawn(112, new(10, 49)));
                                            r.Summon(_room2, new MonsterSpawn(112, new(29, 52)));
                                            r.Summon(_room2, new MonsterSpawn(110, new(49, 37)));
                                            r.Summon(_room2, new MonsterSpawn(110, new(27, 36)));
                                        }));
                                        r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                        r.Summon(_room2, new MonsterSpawn(110, new(43, 19)));
                                        r.Summon(_room2, new MonsterSpawn(112, new(57, 12)));
                                        r.Summon(_room2, new MonsterSpawn(112, new(26, 20)));
                                        r.Summon(_room2, new MonsterSpawn(110, new(40, 23)));
                                        r.Summon(_room2, new MonsterSpawn(110, new(27, 43)));
                                    }));
                                    r.Summon(_room2, new MonsterSpawn(110, new(27, 13)));
                                }));
                                r.Summon(_room2, new MonsterSpawn(112, new(22, 41)));
                                r.Summon(_room2, new MonsterSpawn(110, new(56, 32), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room2, new MonsterSpawn(113, new(47, 33)));
                                    r.Summon(_room2, new MonsterSpawn(112, new(21, 38)));
                                    r.Summon(_room2, new MonsterSpawn(112, new(20, 34)));
                                    r.Summon(_room2, new MonsterSpawn(110, new(32, 50)));
                                    r.Summon(_room2, new MonsterSpawn(110, new(31, 18)));
                                }));
                                r.Summon(_room2, new MonsterSpawn(110, new(47, 47)));
                            }));
                            r.Summon(_room2, new MonsterSpawn(112, new(24, 44)));
                            r.Summon(_room2, new MonsterSpawn(110, new(11, 58)));
                            r.Summon(_room2, new MonsterSpawn(110, new(27, 48)));
                        }),
                        new MonsterSpawn(20, new(34, 22)),
                        new MonsterSpawn(20, new(34, 25)),
                        new MonsterSpawn(20, new(34, 29)),
                        new MonsterSpawn(20, new(34, 32), IsBonus: true),
                        new MonsterSpawn(20, new(34, 35)),
                        new MonsterSpawn(20, new(34, 39)),
                        new MonsterSpawn(20, new(34, 44)),
                        new MonsterSpawn(20, new(34, 47)),
                        new MonsterSpawn(20, new(34, 51)),
                        new MonsterSpawn(112, new(39, 29)));
            r.Dialog(_room2, 6119);
        }
        else if (room == _room3)
        {
            r.Summon(_room3,
                        new MonsterSpawn(109, new(39, 35)),
                        new MonsterSpawn(106, new(39, 36), OnDeath: r =>
                        {
                            r.Summon(_room3,
                            new MonsterSpawn(110, new(35, 26)),
                            new MonsterSpawn(110, new(33, 31)));
                        }),
                        new MonsterSpawn(106, new(41, 36), OnDeath: r =>
                        {
                            r.Summon(_room3, new MonsterSpawn(110, new(33, 35)));
                        }));
            r.Dialog(_room3, 6121);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6122);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace24();
