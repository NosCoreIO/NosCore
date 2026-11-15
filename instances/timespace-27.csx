// Monster Hunt: Part 1
public class TimeSpace27 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace27()
    {
        Definition = InstanceDefinitionBuilder.Named(27, "Let's go monster hunting! Defeat as many monsters as you can in the time allowed. When time is up, the hunt is over. Off you go!", "Monster Hunt: Part 1")
            .ForLevels(37, 99)
            .StartingAt(14, 9)
            .WithLives(2)
            .Requiring(1012, 3)
            .WithDraw(1047, 2)
            .WithDraw(1072, 1)
            .WithSpecialReward(2039, 15)
            .WithReward(2134, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 1, 9)
            .WithRoom(2101, out _room2, 1, 9)
            .WithRoom(4209, out _room3, 2, 9)
            .WithRoom(2101, out _room4, 3, 9)
            .WithRoom(4208, out _room5, 3, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 15), _room2, new(5, 20), locked: false);
        r.SpawnPortal(_room2, new(5, 20), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room2, new(35, 20), _room3, new(1, 15), locked: false);
        r.Summon(_room2,
            new MonsterSpawn(65, new(16, 35)),
            new MonsterSpawn(65, new(24, 30)),
            new MonsterSpawn(16, new(15, 26)),
            new MonsterSpawn(16, new(8, 10)));
        r.SpawnPortal(_room3, new(1, 15), _room4, new(35, 20));
        r.Summon(_room3,
            new MonsterSpawn(137, new(11, 13)),
            new MonsterSpawn(137, new(10, 17), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(142, new(16, 20), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(67, new(21, 18), IsBonus: true, OnDeath: r =>
                    {
                        r.StopRoomClock(_room3);
                        r.Message(Game18NConstString.QuickMissionComplete);
                        r.Message(Game18NConstString.ADoorHasOpened);
                        r.UnlockPortal(_room3, _room4);
                    }));
                }));
            }),
            new MonsterSpawn(22, new(16, 10), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(142, new(19, 14), IsBonus: true));
            }),
            new MonsterSpawn(22, new(16, 17), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room3,
                new MonsterSpawn(67, new(21, 11), IsBonus: true),
                new MonsterSpawn(142, new(13, 10)));
            }));
        r.SpawnPortal(_room4, new(35, 20), _room4, new(35, 20), locked: false);
        r.Summon(_room4,
            new MonsterSpawn(65, new(22, 14)),
            new MonsterSpawn(65, new(11, 19)),
            new MonsterSpawn(16, new(14, 29)),
            new MonsterSpawn(16, new(21, 26), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4, new MonsterSpawn(65, new(29, 22), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room4, new MonsterSpawn(142, new(32, 27)));
                    r.Summon(_room4, new MonsterSpawn(142, new(7, 30)));
                    r.Summon(_room4, new MonsterSpawn(65, new(15, 16)));
                    r.Summon(_room4, new MonsterSpawn(65, new(16, 23)));
                    r.Summon(_room4, new MonsterSpawn(58, new(9, 10)));
                    r.Summon(_room4, new MonsterSpawn(58, new(28, 27), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room4, new MonsterSpawn(142, new(12, 6)));
                        r.Summon(_room4, new MonsterSpawn(142, new(19, 29)));
                        r.Summon(_room4, new MonsterSpawn(58, new(21, 16)));
                        r.Summon(_room4, new MonsterSpawn(58, new(27, 15)));
                        r.Summon(_room4, new MonsterSpawn(58, new(27, 13)));
                    }));
                    r.Summon(_room4, new MonsterSpawn(58, new(11, 28)));
                    r.Summon(_room4, new MonsterSpawn(16, new(21, 27), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room4, new MonsterSpawn(198, new(31, 22)));
                        r.Summon(_room4, new MonsterSpawn(198, new(34, 20)));
                        r.Summon(_room4, new MonsterSpawn(198, new(12, 14)));
                        r.Summon(_room4, new MonsterSpawn(198, new(31, 24)));
                        r.Summon(_room4, new MonsterSpawn(198, new(19, 7)));
                        r.Summon(_room4, new MonsterSpawn(142, new(15, 31)));
                        r.Summon(_room4, new MonsterSpawn(142, new(11, 8)));
                        r.Summon(_room4, new MonsterSpawn(58, new(26, 10)));
                        r.Summon(_room4, new MonsterSpawn(58, new(34, 11), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room4, new MonsterSpawn(198, new(19, 25)));
                            r.Summon(_room4, new MonsterSpawn(198, new(33, 17)));
                            r.Summon(_room4, new MonsterSpawn(198, new(17, 23)));
                            r.Summon(_room4, new MonsterSpawn(198, new(16, 17)));
                            r.Summon(_room4, new MonsterSpawn(198, new(17, 31)));
                        }));
                        r.Summon(_room4, new MonsterSpawn(58, new(22, 11), OnDeath: r =>
                        {
                            r.AddClockTime(TimeSpan.FromSeconds(30));
                            r.Message(Game18NConstString.QuickMissionComplete);
                        }));
                    }));
                    r.Summon(_room4, new MonsterSpawn(16, new(32, 16)));
                    r.Summon(_room4, new MonsterSpawn(65, new(25, 7)));
                    r.Summon(_room4, new MonsterSpawn(65, new(16, 27)));
                    r.Summon(_room4, new MonsterSpawn(58, new(22, 25)));
                    r.Summon(_room4, new MonsterSpawn(58, new(10, 8)));
                    r.Summon(_room4, new MonsterSpawn(58, new(14, 19)));
                    r.Summon(_room4, new MonsterSpawn(16, new(10, 26)));
                    r.Summon(_room4, new MonsterSpawn(16, new(18, 11)));
                }));
                r.Summon(_room4, new MonsterSpawn(65, new(7, 22)));
                r.Summon(_room4, new MonsterSpawn(58, new(14, 30)));
                r.Summon(_room4, new MonsterSpawn(58, new(24, 11)));
                r.Summon(_room4, new MonsterSpawn(58, new(27, 34)));
                r.Summon(_room4, new MonsterSpawn(16, new(29, 28)));
                r.Summon(_room4, new MonsterSpawn(16, new(14, 33)));
            }),
            new MonsterSpawn(16, new(9, 15)),
            new MonsterSpawn(16, new(27, 18)),
            new MonsterSpawn(65, new(24, 25)),
            new MonsterSpawn(65, new(13, 30)));
        r.SpawnExit(_room5, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(28, 15), _room4, new(5, 0), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(59, new(12, 18), IsBonus: true),
            new MonsterSpawn(59, new(19, 24)),
            new MonsterSpawn(59, new(24, 13)),
            new MonsterSpawn(142, new(22, 17), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(142, new(21, 14), IsBonus: true),
                new MonsterSpawn(142, new(15, 19), IsBonus: true));
            }));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(720));
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room2, TimeSpan.FromSeconds(300));
        }
        else if (room == _room4)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(270));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace27();
