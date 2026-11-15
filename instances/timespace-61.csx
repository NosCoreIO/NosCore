// Monster Hunt: Part 4
public class TimeSpace61 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace61()
    {
        Definition = InstanceDefinitionBuilder.Named(61, "Let's go monster hunting! Defeat as many monsters as you can in the time allowed. When time is up, the hunt is over. Off you go!", "Monster Hunt: Part 4")
            .ForLevels(65, 99)
            .StartingAt(11, 13)
            .WithLives(1)
            .Requiring(1012, 6)
            .WithSpecialReward(2048, 30)
            .WithReward(2099, 4)
            .WithReward(1087, 1)
            .WithRoom(4800, out _room1, 0, 6)
            .WithRoom(4800, out _room2, 0, 6)
            .WithRoom(2101, out _room3, 1, 6)
            .WithRoom(2101, out _room4, 2, 6)
            .WithRoom(4800, out _room5, 2, 7)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 14), _room3, new(5, 20), locked: false);
        r.SpawnPortal(_room3, new(5, 20), _room2, new(28, 14));
        r.SpawnPortal(_room3, new(20, 35), _room4, new(20, 5));
        r.Summon(_room3,
            new MonsterSpawn(246, new(10, 7)),
            new MonsterSpawn(243, new(22, 18)),
            new MonsterSpawn(237, new(18, 5)),
            new MonsterSpawn(205, new(19, 31)));
        r.SpawnPortal(_room4, new(5, 20), _room5, new(28, 14));
        r.SpawnPortal(_room4, new(20, 5), _room3, new(20, 35));
        r.Summon(_room4,
            new MonsterSpawn(235, new(15, 32)),
            new MonsterSpawn(235, new(26, 29)),
            new MonsterSpawn(235, new(7, 12)),
            new MonsterSpawn(235, new(16, 23)),
            new MonsterSpawn(235, new(18, 32), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4, new MonsterSpawn(235, new(12, 33)));
                r.Summon(_room4, new MonsterSpawn(235, new(31, 27)));
                r.Summon(_room4, new MonsterSpawn(235, new(33, 11), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room4, new MonsterSpawn(235, new(28, 31)));
                    r.Summon(_room4, new MonsterSpawn(235, new(34, 18)));
                    r.Summon(_room4, new MonsterSpawn(235, new(11, 8)));
                    r.Summon(_room4, new MonsterSpawn(235, new(31, 19)));
                    r.Summon(_room4, new MonsterSpawn(235, new(15, 17)));
                    r.Summon(_room4, new MonsterSpawn(235, new(24, 15)));
                    r.Summon(_room4, new MonsterSpawn(235, new(14, 31)));
                    r.Summon(_room4, new MonsterSpawn(235, new(34, 13)));
                    r.Summon(_room4, new MonsterSpawn(235, new(19, 19)));
                    r.Summon(_room4, new MonsterSpawn(235, new(35, 27), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room4, new MonsterSpawn(235, new(22, 35)));
                        r.Summon(_room4, new MonsterSpawn(235, new(10, 19)));
                        r.Summon(_room4, new MonsterSpawn(235, new(30, 15)));
                        r.Summon(_room4, new MonsterSpawn(235, new(22, 27)));
                        r.Summon(_room4, new MonsterSpawn(235, new(29, 12)));
                        r.Summon(_room4, new MonsterSpawn(235, new(26, 12)));
                        r.Summon(_room4, new MonsterSpawn(235, new(24, 5)));
                        r.Summon(_room4, new MonsterSpawn(235, new(29, 25)));
                        r.Summon(_room4, new MonsterSpawn(235, new(35, 27)));
                        r.Summon(_room4, new MonsterSpawn(235, new(30, 11)));
                    }));
                }));
                r.Summon(_room4, new MonsterSpawn(235, new(21, 15)));
                r.Summon(_room4, new MonsterSpawn(235, new(28, 21)));
                r.Summon(_room4, new MonsterSpawn(235, new(26, 14)));
                r.Summon(_room4, new MonsterSpawn(235, new(25, 24)));
                r.Summon(_room4, new MonsterSpawn(235, new(13, 14)));
                r.Summon(_room4, new MonsterSpawn(235, new(25, 31)));
                r.Summon(_room4, new MonsterSpawn(235, new(21, 24)));
            }),
            new MonsterSpawn(235, new(17, 6)),
            new MonsterSpawn(235, new(15, 10)),
            new MonsterSpawn(235, new(8, 20)),
            new MonsterSpawn(235, new(27, 15), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room4, new MonsterSpawn(235, new(5, 25)));
                r.Summon(_room4, new MonsterSpawn(235, new(25, 26)));
                r.Summon(_room4, new MonsterSpawn(235, new(14, 17)));
                r.Summon(_room4, new MonsterSpawn(235, new(11, 21)));
                r.Summon(_room4, new MonsterSpawn(235, new(23, 13), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room4, new MonsterSpawn(235, new(15, 30)));
                    r.Summon(_room4, new MonsterSpawn(235, new(13, 19), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room4, new MonsterSpawn(235, new(24, 29)));
                        r.Summon(_room4, new MonsterSpawn(235, new(29, 20)));
                        r.Summon(_room4, new MonsterSpawn(235, new(25, 6)));
                        r.Summon(_room4, new MonsterSpawn(235, new(12, 9)));
                        r.Summon(_room4, new MonsterSpawn(235, new(11, 26)));
                        r.Summon(_room4, new MonsterSpawn(235, new(18, 33)));
                        r.Summon(_room4, new MonsterSpawn(235, new(11, 33)));
                        r.Summon(_room4, new MonsterSpawn(235, new(24, 7), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room4, new MonsterSpawn(235, new(30, 12)));
                            r.Summon(_room4, new MonsterSpawn(235, new(19, 30)));
                            r.Summon(_room4, new MonsterSpawn(235, new(6, 27)));
                            r.Summon(_room4, new MonsterSpawn(235, new(10, 10)));
                            r.Summon(_room4, new MonsterSpawn(235, new(33, 13)));
                            r.Summon(_room4, new MonsterSpawn(235, new(29, 24)));
                            r.Summon(_room4, new MonsterSpawn(235, new(21, 6), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room4, new MonsterSpawn(235, new(27, 34)));
                                r.Summon(_room4, new MonsterSpawn(235, new(33, 24)));
                                r.Summon(_room4, new MonsterSpawn(235, new(24, 22)));
                                r.Summon(_room4, new MonsterSpawn(235, new(31, 25)));
                                r.Summon(_room4, new MonsterSpawn(235, new(32, 21)));
                                r.Summon(_room4, new MonsterSpawn(235, new(22, 27)));
                                r.Summon(_room4, new MonsterSpawn(235, new(14, 13)));
                                r.Summon(_room4, new MonsterSpawn(235, new(13, 31)));
                                r.Summon(_room4, new MonsterSpawn(235, new(5, 21)));
                                r.Summon(_room4, new MonsterSpawn(235, new(10, 24)));
                            }));
                            r.Summon(_room4, new MonsterSpawn(235, new(20, 32)));
                            r.Summon(_room4, new MonsterSpawn(235, new(8, 13)));
                            r.Summon(_room4, new MonsterSpawn(235, new(10, 25)));
                        }));
                        r.Summon(_room4, new MonsterSpawn(235, new(19, 31)));
                        r.Summon(_room4, new MonsterSpawn(235, new(12, 24)));
                    }));
                    r.Summon(_room4, new MonsterSpawn(235, new(30, 28)));
                    r.Summon(_room4, new MonsterSpawn(235, new(15, 8)));
                    r.Summon(_room4, new MonsterSpawn(235, new(27, 12)));
                    r.Summon(_room4, new MonsterSpawn(235, new(23, 16)));
                    r.Summon(_room4, new MonsterSpawn(235, new(17, 25)));
                    r.Summon(_room4, new MonsterSpawn(235, new(15, 7)));
                    r.Summon(_room4, new MonsterSpawn(235, new(27, 7)));
                    r.Summon(_room4, new MonsterSpawn(235, new(16, 21)));
                }));
                r.Summon(_room4, new MonsterSpawn(235, new(25, 32)));
                r.Summon(_room4, new MonsterSpawn(235, new(34, 14)));
                r.Summon(_room4, new MonsterSpawn(235, new(34, 21)));
                r.Summon(_room4, new MonsterSpawn(235, new(17, 29)));
                r.Summon(_room4, new MonsterSpawn(235, new(5, 25)));
            }),
            new MonsterSpawn(235, new(17, 9)));
        r.SpawnExit(_room5, new(15, 28), locked: false);
        r.SpawnPortal(_room5, new(28, 14), _room4, new(5, 20), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(420));
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room3, new MonsterSpawn(246, new(8, 15), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room3, new MonsterSpawn(246, new(32, 19)));
                            r.Summon(_room3, new MonsterSpawn(243, new(16, 19)));
                            r.Summon(_room3, new MonsterSpawn(237, new(29, 5), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room3, new MonsterSpawn(246, new(12, 30)));
                                r.Summon(_room3, new MonsterSpawn(243, new(17, 21)));
                                r.Summon(_room3, new MonsterSpawn(237, new(18, 8)));
                                r.Summon(_room3, new MonsterSpawn(205, new(27, 33), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room3, new MonsterSpawn(246, new(6, 28)));
                                    r.Summon(_room3, new MonsterSpawn(243, new(24, 6)));
                                    r.Summon(_room3, new MonsterSpawn(237, new(8, 24)));
                                    r.Summon(_room3, new MonsterSpawn(205, new(28, 6), OnDeath: r =>
                                    {
                                        r.Message(Game18NConstString.EnemiesReinforcements);
                                        r.Summon(_room3, new MonsterSpawn(246, new(21, 22), OnDeath: r =>
                                        {
                                            r.Message(Game18NConstString.EnemiesReinforcements);
                                            r.Summon(_room3, new MonsterSpawn(246, new(13, 9)));
                                            r.Summon(_room3, new MonsterSpawn(243, new(12, 29)));
                                            r.Summon(_room3, new MonsterSpawn(237, new(27, 8)));
                                            r.Summon(_room3, new MonsterSpawn(205, new(7, 24), OnDeath: r =>
                                            {
                                                r.Message(Game18NConstString.EnemiesReinforcements);
                                                r.Summon(_room3, new MonsterSpawn(246, new(24, 22)));
                                                r.Summon(_room3, new MonsterSpawn(243, new(12, 17)));
                                                r.Summon(_room3, new MonsterSpawn(237, new(13, 34)));
                                                r.Summon(_room3, new MonsterSpawn(205, new(9, 20)));
                                                r.Summon(_room3, new MonsterSpawn(246, new(33, 19)));
                                                r.Summon(_room3, new MonsterSpawn(243, new(28, 27)));
                                                r.Summon(_room3, new MonsterSpawn(237, new(28, 28), OnDeath: r =>
                                                {
                                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                                    r.Summon(_room3, new MonsterSpawn(246, new(25, 31)));
                                                    r.Summon(_room3, new MonsterSpawn(243, new(32, 14)));
                                                    r.Summon(_room3, new MonsterSpawn(237, new(8, 16)));
                                                    r.Summon(_room3, new MonsterSpawn(205, new(33, 10)));
                                                }));
                                                r.Summon(_room3, new MonsterSpawn(205, new(23, 19)));
                                            }));
                                        }));
                                        r.Summon(_room3, new MonsterSpawn(243, new(6, 23)));
                                        r.Summon(_room3, new MonsterSpawn(237, new(12, 16)));
                                        r.Summon(_room3, new MonsterSpawn(205, new(16, 23)));
                                    }));
                                }));
                            }));
                            r.Summon(_room3, new MonsterSpawn(205, new(9, 26)));
                        }));
            r.Summon(_room3, new MonsterSpawn(243, new(13, 13)));
            r.Summon(_room3, new MonsterSpawn(237, new(31, 10)));
            r.Summon(_room3, new MonsterSpawn(205, new(23, 16)));
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(300));
        }
        else if (room == _room4)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(300));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.StopRoomClock(_room3);
            r.AddClockTime(TimeSpan.FromSeconds(400));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room4)
        {
            r.StopRoomClock(_room4);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room4, _room5);
            r.UnlockPortal(_room4, _room3);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace61();
