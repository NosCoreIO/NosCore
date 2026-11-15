// Monster Hunt: Part 3
public class TimeSpace50 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;

    public TimeSpace50()
    {
        Definition = InstanceDefinitionBuilder.Named(50, "Let's go monster hunting! Defeat as many monsters as you can in the time allowed. When time is up, the hunt is over. Off you go!", "Monster Hunt: Part 3")
            .ForLevels(58, 99)
            .StartingAt(8, 14)
            .WithLives(1)
            .Requiring(1012, 5)
            .WithDraw(535, 1)
            .WithSpecialReward(1013, 30)
            .WithReward(2131, 4)
            .WithReward(1087, 1)
            .WithRoom(4814, out _room1, 0, 8)
            .WithRoom(4805, out _room2, 0, 8)
            .WithRoom(2101, out _room3, 1, 8)
            .WithRoom(4805, out _room4, 2, 8)
            .WithRoom(2101, out _room5, 3, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(29, 14), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(29, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 14), _room3, new(5, 20), locked: false);
        r.SpawnPortal(_room3, new(5, 20), _room2, new(28, 14));
        r.SpawnPortal(_room3, new(35, 20), _room4, new(1, 14));
        r.Summon(_room3,
            new MonsterSpawn(134, new(21, 34), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room3, new MonsterSpawn(134, new(25, 7), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room3, new MonsterSpawn(235, new(21, 6), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room3, new MonsterSpawn(235, new(22, 6)));
                        r.Summon(_room3, new MonsterSpawn(204, new(18, 24)));
                        r.Summon(_room3, new MonsterSpawn(204, new(9, 24)));
                        r.Summon(_room3, new MonsterSpawn(200, new(7, 14)));
                    }));
                    r.Summon(_room3, new MonsterSpawn(232, new(30, 8)));
                    r.Summon(_room3, new MonsterSpawn(232, new(24, 10)));
                    r.Summon(_room3, new MonsterSpawn(200, new(10, 25), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room3, new MonsterSpawn(206, new(25, 10)));
                        r.Summon(_room3, new MonsterSpawn(206, new(17, 19), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room3, new MonsterSpawn(218, new(13, 26)));
                            r.Summon(_room3, new MonsterSpawn(218, new(16, 26), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room3, new MonsterSpawn(218, new(10, 25)));
                                r.Summon(_room3, new MonsterSpawn(202, new(18, 30)));
                                r.Summon(_room3, new MonsterSpawn(202, new(10, 22)));
                                r.Summon(_room3, new MonsterSpawn(202, new(30, 11)));
                            }));
                            r.Summon(_room3, new MonsterSpawn(206, new(27, 24)));
                            r.Summon(_room3, new MonsterSpawn(206, new(33, 10), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room3, new MonsterSpawn(202, new(29, 28)));
                                r.Summon(_room3, new MonsterSpawn(202, new(10, 21)));
                                r.Summon(_room3, new MonsterSpawn(202, new(5, 24)));
                            }));
                            r.Summon(_room3, new MonsterSpawn(206, new(21, 35)));
                        }));
                        r.Summon(_room3, new MonsterSpawn(206, new(21, 29)));
                    }));
                }));
                r.Summon(_room3, new MonsterSpawn(134, new(7, 20), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room3, new MonsterSpawn(154, new(28, 12)));
                    r.Summon(_room3, new MonsterSpawn(154, new(13, 12)));
                    r.Summon(_room3, new MonsterSpawn(134, new(25, 26)));
                    r.Summon(_room3, new MonsterSpawn(134, new(7, 13)));
                    r.Summon(_room3, new MonsterSpawn(88, new(7, 13)));
                    r.Summon(_room3, new MonsterSpawn(88, new(6, 30)));
                    r.Summon(_room3, new MonsterSpawn(134, new(23, 16), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room3, new MonsterSpawn(232, new(24, 31), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room3, new MonsterSpawn(232, new(26, 35)));
                            r.Summon(_room3, new MonsterSpawn(232, new(13, 26)));
                            r.Summon(_room3, new MonsterSpawn(200, new(8, 23)));
                        }));
                        r.Summon(_room3, new MonsterSpawn(232, new(19, 33)));
                        r.Summon(_room3, new MonsterSpawn(154, new(26, 21)));
                        r.Summon(_room3, new MonsterSpawn(154, new(18, 18)));
                        r.Summon(_room3, new MonsterSpawn(88, new(8, 24), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room3, new MonsterSpawn(232, new(11, 24), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room3, new MonsterSpawn(232, new(34, 13)));
                                r.Summon(_room3, new MonsterSpawn(232, new(10, 17)));
                                r.Summon(_room3, new MonsterSpawn(200, new(29, 9)));
                                r.Summon(_room3, new MonsterSpawn(154, new(14, 32), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room3, new MonsterSpawn(235, new(22, 25)));
                                    r.Summon(_room3, new MonsterSpawn(200, new(7, 20)));
                                }));
                                r.Summon(_room3, new MonsterSpawn(154, new(32, 10), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room3, new MonsterSpawn(235, new(15, 14)));
                                    r.Summon(_room3, new MonsterSpawn(204, new(20, 32)));
                                    r.Summon(_room3, new MonsterSpawn(204, new(17, 6)));
                                }));
                            }));
                            r.Summon(_room3, new MonsterSpawn(232, new(20, 32)));
                            r.Summon(_room3, new MonsterSpawn(154, new(7, 21)));
                            r.Summon(_room3, new MonsterSpawn(154, new(27, 26)));
                        }));
                        r.Summon(_room3, new MonsterSpawn(88, new(19, 8)));
                        r.Summon(_room3, new MonsterSpawn(154, new(25, 25), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room3, new MonsterSpawn(235, new(34, 30)));
                            r.Summon(_room3, new MonsterSpawn(206, new(35, 14)));
                            r.Summon(_room3, new MonsterSpawn(206, new(10, 32)));
                            r.Summon(_room3, new MonsterSpawn(206, new(13, 32)));
                            r.Summon(_room3, new MonsterSpawn(204, new(16, 20)));
                            r.Summon(_room3, new MonsterSpawn(204, new(22, 19)));
                        }));
                        r.Summon(_room3, new MonsterSpawn(154, new(30, 12)));
                        r.Summon(_room3, new MonsterSpawn(88, new(26, 15)));
                        r.Summon(_room3, new MonsterSpawn(88, new(35, 21)));
                    }));
                    r.Summon(_room3, new MonsterSpawn(134, new(12, 24)));
                    r.Summon(_room3, new MonsterSpawn(88, new(15, 7)));
                    r.Summon(_room3, new MonsterSpawn(88, new(32, 29), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room3, new MonsterSpawn(202, new(11, 8)));
                        r.Summon(_room3, new MonsterSpawn(202, new(27, 9)));
                        r.Summon(_room3, new MonsterSpawn(202, new(17, 11)));
                    }));
                }));
                r.Summon(_room3, new MonsterSpawn(88, new(10, 13)));
                r.Summon(_room3, new MonsterSpawn(88, new(27, 32)));
                r.Summon(_room3, new MonsterSpawn(134, new(12, 26)));
                r.Summon(_room3, new MonsterSpawn(134, new(26, 31)));
            }),
            new MonsterSpawn(134, new(16, 28)));
        r.SpawnPortal(_room4, new(1, 14), _room3, new(35, 20), locked: false);
        r.SpawnPortal(_room4, new(28, 14), _room5, new(5, 20), locked: false);
        r.SpawnPortal(_room5, new(5, 20), _room4, new(28, 14));
        r.SpawnExit(_room5, new(35, 20));
        r.Summon(_room5,
            new MonsterSpawn(220, new(20, 20), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5, new MonsterSpawn(217, new(8, 23)));
                r.Summon(_room5, new MonsterSpawn(217, new(10, 20)));
                r.Summon(_room5, new MonsterSpawn(217, new(14, 19), OnDeath: r =>
                {
                    r.Message(Game18NConstString.EnemiesReinforcements);
                    r.Summon(_room5, new MonsterSpawn(217, new(26, 31), OnDeath: r =>
                    {
                        r.Message(Game18NConstString.EnemiesReinforcements);
                        r.Summon(_room5, new MonsterSpawn(217, new(15, 9)));
                        r.Summon(_room5, new MonsterSpawn(217, new(31, 18)));
                        r.Summon(_room5, new MonsterSpawn(217, new(30, 32), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room5, new MonsterSpawn(217, new(31, 21)));
                            r.Summon(_room5, new MonsterSpawn(217, new(19, 27), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room5, new MonsterSpawn(217, new(18, 11)));
                                r.Summon(_room5, new MonsterSpawn(217, new(13, 21)));
                                r.Summon(_room5, new MonsterSpawn(217, new(27, 30)));
                                r.Summon(_room5, new MonsterSpawn(217, new(33, 27), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room5, new MonsterSpawn(217, new(28, 13)));
                                    r.Summon(_room5, new MonsterSpawn(217, new(34, 19)));
                                    r.Summon(_room5, new MonsterSpawn(217, new(28, 12)));
                                    r.Summon(_room5, new MonsterSpawn(217, new(13, 34), OnDeath: r =>
                                    {
                                        r.Message(Game18NConstString.EnemiesReinforcements);
                                        r.Summon(_room5, new MonsterSpawn(217, new(21, 11)));
                                        r.Summon(_room5, new MonsterSpawn(217, new(29, 33), OnDeath: r =>
                                        {
                                            r.Message(Game18NConstString.EnemiesReinforcements);
                                            r.Summon(_room5, new MonsterSpawn(217, new(17, 24)));
                                            r.Summon(_room5, new MonsterSpawn(217, new(25, 7), OnDeath: r =>
                                            {
                                                r.Message(Game18NConstString.EnemiesReinforcements);
                                                r.Summon(_room5, new MonsterSpawn(217, new(25, 8)));
                                                r.Summon(_room5, new MonsterSpawn(217, new(28, 5), OnDeath: r =>
                                                {
                                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                                    r.Summon(_room5, new MonsterSpawn(217, new(6, 26)));
                                                    r.Summon(_room5, new MonsterSpawn(217, new(6, 18)));
                                                    r.Summon(_room5, new MonsterSpawn(217, new(7, 30)));
                                                    r.Summon(_room5, new MonsterSpawn(217, new(18, 32), OnDeath: r =>
                                                    {
                                                        r.Message(Game18NConstString.EnemiesReinforcements);
                                                        r.Summon(_room5, new MonsterSpawn(217, new(14, 13)));
                                                        r.Summon(_room5, new MonsterSpawn(217, new(18, 21)));
                                                        r.Summon(_room5, new MonsterSpawn(214, new(30, 11), OnDeath: r =>
                                                        {
                                                            r.Message(Game18NConstString.EnemiesReinforcements);
                                                            r.Summon(_room5, new MonsterSpawn(217, new(29, 24)));
                                                            r.Summon(_room5, new MonsterSpawn(217, new(7, 25)));
                                                            r.Summon(_room5, new MonsterSpawn(214, new(13, 7)));
                                                            r.Summon(_room5, new MonsterSpawn(214, new(20, 21)));
                                                            r.Summon(_room5, new MonsterSpawn(217, new(6, 29)));
                                                            r.Summon(_room5, new MonsterSpawn(217, new(26, 33), OnDeath: r =>
                                                            {
                                                                r.Message(Game18NConstString.EnemiesReinforcements);
                                                                r.Summon(_room5, new MonsterSpawn(217, new(6, 22)));
                                                                r.Summon(_room5, new MonsterSpawn(217, new(17, 27)));
                                                                r.Summon(_room5, new MonsterSpawn(214, new(31, 28)));
                                                                r.Summon(_room5, new MonsterSpawn(214, new(11, 15)));
                                                                r.Summon(_room5, new MonsterSpawn(217, new(6, 29)));
                                                                r.Summon(_room5, new MonsterSpawn(217, new(22, 15), OnDeath: r =>
                                                                {
                                                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                                                    r.Summon(_room5, new MonsterSpawn(214, new(15, 29)));
                                                                    r.Summon(_room5, new MonsterSpawn(214, new(24, 29), OnDeath: r =>
                                                                    {
                                                                        r.Message(Game18NConstString.EnemiesReinforcements);
                                                                        r.Summon(_room5, new MonsterSpawn(214, new(19, 33), OnDeath: r =>
                                                                        {
                                                                            r.Message(Game18NConstString.EnemiesReinforcements);
                                                                            r.Summon(_room5, new MonsterSpawn(214, new(10, 5)));
                                                                            r.Summon(_room5, new MonsterSpawn(214, new(35, 21), OnDeath: r =>
                                                                            {
                                                                                r.Message(Game18NConstString.EnemiesReinforcements);
                                                                                r.Summon(_room5, new MonsterSpawn(214, new(13, 30)));
                                                                                r.Summon(_room5, new MonsterSpawn(214, new(12, 31)));
                                                                            }));
                                                                        }));
                                                                        r.Summon(_room5, new MonsterSpawn(214, new(26, 21)));
                                                                    }));
                                                                }));
                                                            }));
                                                        }));
                                                        r.Summon(_room5, new MonsterSpawn(214, new(24, 30)));
                                                        r.Summon(_room5, new MonsterSpawn(217, new(10, 15)));
                                                        r.Summon(_room5, new MonsterSpawn(217, new(16, 27)));
                                                    }));
                                                }));
                                                r.Summon(_room5, new MonsterSpawn(217, new(10, 25)));
                                                r.Summon(_room5, new MonsterSpawn(217, new(13, 7)));
                                            }));
                                            r.Summon(_room5, new MonsterSpawn(217, new(24, 35)));
                                            r.Summon(_room5, new MonsterSpawn(217, new(29, 23)));
                                        }));
                                        r.Summon(_room5, new MonsterSpawn(217, new(15, 8)));
                                        r.Summon(_room5, new MonsterSpawn(217, new(19, 30)));
                                    }));
                                }));
                            }));
                            r.Summon(_room5, new MonsterSpawn(217, new(29, 26)));
                            r.Summon(_room5, new MonsterSpawn(217, new(11, 23)));
                        }));
                        r.Summon(_room5, new MonsterSpawn(217, new(22, 26)));
                    }));
                    r.Summon(_room5, new MonsterSpawn(217, new(28, 8)));
                    r.Summon(_room5, new MonsterSpawn(217, new(30, 26)));
                    r.Summon(_room5, new MonsterSpawn(217, new(23, 11)));
                }));
                r.Summon(_room5, new MonsterSpawn(217, new(10, 32)));
            }),
            new MonsterSpawn(217, new(21, 5)),
            new MonsterSpawn(217, new(14, 16)),
            new MonsterSpawn(217, new(25, 8)),
            new MonsterSpawn(217, new(33, 22), OnDeath: r =>
            {
                r.Message(Game18NConstString.EnemiesReinforcements);
                r.Summon(_room5, new MonsterSpawn(217, new(31, 18)));
                r.Summon(_room5, new MonsterSpawn(217, new(29, 23)));
                r.Summon(_room5, new MonsterSpawn(217, new(28, 23)));
                r.Summon(_room5, new MonsterSpawn(217, new(28, 28)));
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
            r.StartClock(TimeSpan.FromSeconds(820));
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(720));
        }
        else if (room == _room5)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(600));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.StopRoomClock(_room3);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room3, _room2);
            r.UnlockPortal(_room3, _room4);
        }
        else if (room == _room5)
        {
            r.StopRoomClock(_room5);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room5, _room4);
            r.UnlockExit(_room5);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace50();
