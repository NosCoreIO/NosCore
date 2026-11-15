// A Grim Place for a Monster Hunt
public class TimeSpace32 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace32()
    {
        Definition = InstanceDefinitionBuilder.Named(32, "Let's go monster hunting! Defeat as many monsters as you can in the time allowed. When time is up, the hunt is over. Off you go!", "A Grim Place for a Monster Hunt")
            .ForLevels(42, 99)
            .StartingAt(13, 12)
            .WithLives(2)
            .Requiring(1012, 4)
            .WithDraw(704, 1)
            .WithDraw(1028, 2)
            .WithSpecialReward(1013, 20)
            .WithReward(2028, 5)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 2, 9)
            .WithRoom(4200, out _room2, 2, 9)
            .WithRoom(2100, out _room3, 3, 9)
            .WithRoom(2101, out _room4, 4, 9)
            .WithRoom(2100, out _room5, 4, 8)
            .WithRoom(2101, out _room6, 3, 8)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnExit(_room2, new(14, 28));
        r.SpawnPortal(_room2, new(28, 15), _room3, new(9, 36), locked: false);
        r.SpawnPortal(_room3, new(9, 36), _room2, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(34, 11), _room4, new(20, 35), locked: false);
        r.Summon(_room3,
            new MonsterSpawn(138, new(25, 30)),
            new MonsterSpawn(138, new(57, 25)),
            new MonsterSpawn(138, new(57, 35)),
            new MonsterSpawn(103, new(44, 39)),
            new MonsterSpawn(103, new(43, 20)),
            new MonsterSpawn(82, new(49, 51)),
            new MonsterSpawn(82, new(49, 19)));
        r.SpawnPortal(_room4, new(20, 35), _room5, new(34, 11));
        r.PlaceButton(_room4, 1, new(20, 18), 1000, 1045);
        r.Summon(_room4,
            new MonsterSpawn(377, new(10, 14)),
            new MonsterSpawn(377, new(26, 32)),
            new MonsterSpawn(377, new(18, 35)),
            new MonsterSpawn(377, new(29, 33)),
            new MonsterSpawn(377, new(34, 27)),
            new MonsterSpawn(73, new(5, 20)),
            new MonsterSpawn(73, new(16, 10)));
        r.SpawnPortal(_room5, new(34, 11), _room5, new(34, 11), locked: false);
        r.Summon(_room5,
            new MonsterSpawn(82, new(48, 27)),
            new MonsterSpawn(82, new(52, 51)),
            new MonsterSpawn(103, new(51, 20)),
            new MonsterSpawn(103, new(41, 39)),
            new MonsterSpawn(138, new(59, 30)),
            new MonsterSpawn(138, new(52, 14)),
            new MonsterSpawn(138, new(32, 29)),
            new MonsterSpawn(82, new(32, 12)),
            new MonsterSpawn(82, new(14, 36)),
            new MonsterSpawn(103, new(23, 13)),
            new MonsterSpawn(103, new(55, 28)),
            new MonsterSpawn(138, new(59, 29)),
            new MonsterSpawn(138, new(11, 44)),
            new MonsterSpawn(138, new(33, 46)),
            new MonsterSpawn(82, new(42, 23)),
            new MonsterSpawn(82, new(39, 54)),
            new MonsterSpawn(103, new(58, 10)),
            new MonsterSpawn(103, new(59, 35)),
            new MonsterSpawn(138, new(11, 14), OnDeath: r =>
            {
                r.StopRoomClock(_room5);
            }),
            new MonsterSpawn(138, new(12, 52)),
            new MonsterSpawn(138, new(27, 32)),
            new MonsterSpawn(82, new(39, 50)),
            new MonsterSpawn(82, new(31, 41)),
            new MonsterSpawn(103, new(37, 55)),
            new MonsterSpawn(103, new(40, 59)),
            new MonsterSpawn(138, new(57, 53)),
            new MonsterSpawn(138, new(59, 28)),
            new MonsterSpawn(138, new(50, 36)),
            new MonsterSpawn(82, new(47, 42)),
            new MonsterSpawn(82, new(12, 42)),
            new MonsterSpawn(103, new(22, 19)),
            new MonsterSpawn(103, new(45, 21)),
            new MonsterSpawn(138, new(19, 52)),
            new MonsterSpawn(138, new(29, 42)),
            new MonsterSpawn(138, new(39, 21)),
            new MonsterSpawn(377, new(39, 42)),
            new MonsterSpawn(377, new(38, 21)),
            new MonsterSpawn(82, new(41, 27)),
            new MonsterSpawn(82, new(58, 10)),
            new MonsterSpawn(103, new(43, 57)),
            new MonsterSpawn(103, new(53, 47)),
            new MonsterSpawn(82, new(24, 32)),
            new MonsterSpawn(82, new(48, 41)),
            new MonsterSpawn(103, new(33, 23)),
            new MonsterSpawn(103, new(12, 38)),
            new MonsterSpawn(377, new(58, 58)),
            new MonsterSpawn(377, new(44, 53)),
            new MonsterSpawn(377, new(51, 52)));
        r.SpawnPortal(_room6, new(5, 20), _room5, new(35, 0), locked: false);
        r.SpawnPortal(_room6, new(20, 35), _room6, new(20, 35), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(850));
        }
        else if (room == _room3)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room3, TimeSpan.FromSeconds(300));
        }
        else if (room == _room4)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.StartRoomClock(_room4, TimeSpan.FromSeconds(200));
        }
        else if (room == _room5)
        {
            r.StartRoomClock(_room5, TimeSpan.FromSeconds(89));
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room4 && button == 1)
        {
            r.Message(Game18NConstString.EnemiesReinforcements);
            r.Summon(_room4, new MonsterSpawn(73, new(12, 14), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room4, new MonsterSpawn(138, new(9, 27)));
                            r.Summon(_room4, new MonsterSpawn(138, new(26, 9)));
                            r.Summon(_room4, new MonsterSpawn(138, new(5, 25)));
                            r.Summon(_room4, new MonsterSpawn(138, new(32, 15)));
                            r.Summon(_room4, new MonsterSpawn(138, new(12, 8)));
                            r.Summon(_room4, new MonsterSpawn(73, new(18, 20)));
                            r.Summon(_room4, new MonsterSpawn(73, new(29, 21), OnDeath: r =>
                            {
                                r.StopRoomClock(_room4);
                                r.Message(Game18NConstString.ADoorHasOpened);
                                r.UnlockPortal(_room4, _room5);
                            }));
                        }));
            r.Summon(_room4, new MonsterSpawn(73, new(28, 34), OnDeath: r =>
                        {
                            r.Message(Game18NConstString.EnemiesReinforcements);
                            r.Summon(_room4, new MonsterSpawn(377, new(18, 13)));
                            r.Summon(_room4, new MonsterSpawn(377, new(26, 9), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room4, new MonsterSpawn(138, new(23, 12)));
                                r.Summon(_room4, new MonsterSpawn(138, new(28, 29)));
                                r.Summon(_room4, new MonsterSpawn(138, new(25, 29)));
                                r.Summon(_room4, new MonsterSpawn(138, new(10, 20)));
                                r.Summon(_room4, new MonsterSpawn(138, new(6, 22)));
                                r.Summon(_room4, new MonsterSpawn(73, new(35, 23)));
                                r.Summon(_room4, new MonsterSpawn(73, new(7, 30)));
                            }));
                            r.Summon(_room4, new MonsterSpawn(73, new(9, 30)));
                            r.Summon(_room4, new MonsterSpawn(73, new(23, 25), OnDeath: r =>
                            {
                                r.Message(Game18NConstString.EnemiesReinforcements);
                                r.Summon(_room4, new MonsterSpawn(73, new(33, 13)));
                                r.Summon(_room4, new MonsterSpawn(73, new(31, 23), OnDeath: r =>
                                {
                                    r.Message(Game18NConstString.EnemiesReinforcements);
                                    r.Summon(_room4, new MonsterSpawn(377, new(15, 14)));
                                    r.Summon(_room4, new MonsterSpawn(377, new(21, 14)));
                                    r.Summon(_room4, new MonsterSpawn(377, new(13, 27)));
                                    r.Summon(_room4, new MonsterSpawn(138, new(34, 26)));
                                    r.Summon(_room4, new MonsterSpawn(138, new(9, 14)));
                                    r.Summon(_room4, new MonsterSpawn(138, new(8, 25)));
                                    r.Summon(_room4, new MonsterSpawn(138, new(25, 11)));
                                    r.Summon(_room4, new MonsterSpawn(138, new(19, 22)));
                                    r.Summon(_room4, new MonsterSpawn(73, new(22, 34)));
                                    r.Summon(_room4, new MonsterSpawn(73, new(25, 24)));
                                }));
                            }));
                        }));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace32();
