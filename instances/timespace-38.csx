// The Kenkos' Secret
public class TimeSpace38 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;
    private readonly int _room8;
    private readonly int _room9;
    private readonly int _room10;
    private readonly int _room11;
    private readonly int _room12;
    private readonly int _room13;

    public TimeSpace38()
    {
        Definition = InstanceDefinitionBuilder.Named(38, "Why do the demons want Mount Krem? Why are the Kenkos allied with Lord Cuarry's knights? Perhaps you will find the answers in the Kenko camp. Look for clues about the conspiracy and find out where the danger comes from!", "The Kenkos' Secret")
            .ForLevels(48, 99)
            .StartingAt(13, 11)
            .WithLives(1)
            .Requiring(1012, 4)
            .WithDraw(1085, 2)
            .WithDraw(1011, 3)
            .WithSpecialReward(2040, 15)
            .WithReward(2127, 1)
            .WithReward(1087, 1)
            .WithRoom(5307, out _room1, 0, 8)
            .WithRoom(5307, out _room2, 0, 8)
            .WithRoom(5307, out _room3, 1, 8)
            .WithRoom(5307, out _room4, 2, 8)
            .WithRoom(5307, out _room5, 3, 8)
            .WithRoom(5308, out _room6, 4, 8)
            .WithRoom(4405, out _room7, 5, 8)
            .WithRoom(4406, out _room8, 6, 8)
            .WithRoom(4408, out _room9, 6, 9)
            .WithRoom(4406, out _room10, 5, 9)
            .WithRoom(4407, out _room11, 5, 10)
            .WithRoom(4405, out _room12, 4, 10)
            .WithRoom(4408, out _room13, 4, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.SpawnPortal(_room1, new(28, 16), _room2, new(2, 16), locked: false);
        r.SpawnPortal(_room2, new(2, 16), _room1, new(28, 16), locked: false);
        r.SpawnPortal(_room2, new(28, 16), _room3, new(2, 16), locked: false);
        r.SpawnPortal(_room3, new(2, 16), _room2, new(28, 16), locked: false);
        r.SpawnPortal(_room3, new(28, 16), _room4, new(2, 16), locked: false);
        r.SpawnPortal(_room4, new(2, 16), _room3, new(28, 16), locked: false);
        r.SpawnPortal(_room4, new(28, 16), _room5, new(2, 16), locked: false);
        r.SpawnPortal(_room5, new(2, 16), _room4, new(28, 16), locked: false);
        r.SpawnPortal(_room5, new(28, 16), _room6, new(2, 16), locked: false);
        r.SpawnPortal(_room6, new(2, 16), _room5, new(28, 16), locked: false);
        r.SpawnPortal(_room6, new(28, 16), _room7, new(1, 12), locked: false);
        r.SpawnPortal(_room7, new(1, 12), _room6, new(28, 16), locked: false);
        r.SpawnPortal(_room7, new(12, 23), _room8, new(12, 1), locked: false);
        r.SpawnPortal(_room8, new(1, 12), _room9, new(23, 12));
        r.SpawnPortal(_room8, new(12, 1), _room7, new(12, 23));
        r.SpawnPortal(_room9, new(12, 23), _room10, new(12, 1));
        r.SpawnPortal(_room9, new(23, 12), _room8, new(1, 12));
        r.Summon(_room9,
            new MonsterSpawn(150, new(6, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(153, new(6, 13), IsBonus: true));
            }),
            new MonsterSpawn(147, new(8, 12), IsBonus: true),
            new MonsterSpawn(147, new(7, 17)),
            new MonsterSpawn(152, new(11, 9), IsBonus: true),
            new MonsterSpawn(152, new(11, 12)),
            new MonsterSpawn(152, new(11, 17), IsBonus: true));
        r.SpawnPortal(_room10, new(1, 12), _room11, new(23, 12));
        r.SpawnPortal(_room10, new(12, 1), _room9, new(12, 23));
        r.Summon(_room10,
            new MonsterSpawn(152, new(15, 8), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(150, new(11, 13)));
            }),
            new MonsterSpawn(152, new(16, 12), OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(152, new(14, 11), OnDeath: r =>
                {
                    r.Summon(_room10,
                    new MonsterSpawn(147, new(12, 18), IsBonus: true),
                    new MonsterSpawn(147, new(12, 7), IsBonus: true));
                }));
            }),
            new MonsterSpawn(152, new(16, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room10, new MonsterSpawn(147, new(7, 11), IsBonus: true));
            }));
        r.SpawnPortal(_room11, new(23, 12), _room10, new(1, 12));
        r.SpawnPortal(_room11, new(12, 1), _room12, new(12, 23));
        r.Summon(_room11,
            new MonsterSpawn(152, new(15, 8), IsBonus: true),
            new MonsterSpawn(152, new(16, 15), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(150, new(11, 13), OnDeath: r =>
                {
                    r.Summon(_room11,
                    new MonsterSpawn(147, new(12, 18), IsBonus: true),
                    new MonsterSpawn(147, new(12, 7), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room11, new MonsterSpawn(150, new(9, 16), IsBonus: true));
                    }));
                }));
            }),
            new MonsterSpawn(152, new(16, 17), OnDeath: r =>
            {
                r.Summon(_room11, new MonsterSpawn(152, new(14, 11), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room11, new MonsterSpawn(147, new(7, 11), IsBonus: true));
                }));
            }));
        r.SpawnPortal(_room12, new(1, 12), _room13, new(23, 12));
        r.SpawnPortal(_room12, new(12, 23), _room11, new(12, 1));
        r.Summon(_room12,
            new MonsterSpawn(152, new(11, 9)),
            new MonsterSpawn(152, new(11, 12), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room12,
                new MonsterSpawn(147, new(8, 12), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room12, new MonsterSpawn(150, new(6, 15), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room12, new MonsterSpawn(153, new(6, 13), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room12, new MonsterSpawn(153, new(11, 21), IsBonus: true));
                        }));
                    }));
                }),
                new MonsterSpawn(147, new(7, 17)));
            }),
            new MonsterSpawn(152, new(11, 17)));
        r.SpawnExit(_room13, new(12, 23), locked: false);
        r.SpawnPortal(_room13, new(23, 12), _room12, new(1, 12), locked: false);
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.StartClock(TimeSpan.FromSeconds(150));
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room2, 6161);
        }
        else if (room == _room7)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room7, 6162);
        }
        else if (room == _room8)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Summon(_room8,
                        new MonsterSpawn(152, new(15, 8), OnDeath: r =>
                        {
                            r.Summon(_room8, new MonsterSpawn(150, new(11, 13), OnDeath: r =>
                            {
                                r.Summon(_room8, new MonsterSpawn(147, new(7, 11)));
                            }));
                        }),
                        new MonsterSpawn(152, new(16, 12), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room8, new MonsterSpawn(152, new(14, 11)));
                        }),
                        new MonsterSpawn(152, new(16, 16), IsBonus: true, OnDeath: r =>
                        {
                            r.Summon(_room8,
                            new MonsterSpawn(147, new(12, 18), IsBonus: true),
                            new MonsterSpawn(147, new(12, 7)));
                        }));
            r.Dialog(_room8, 6163);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room9, 6163);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room10, 6163);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room11, 6163);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room12, 6163);
        }
        else if (room == _room13)
        {
            r.Message(Game18NConstString.NewGoal);
            r.Dialog(_room13, 6164);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room8)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room8, _room9);
        }
        else if (room == _room9)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room9, _room10);
        }
        else if (room == _room10)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room10, _room11);
        }
        else if (room == _room11)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room11, _room12);
        }
        else if (room == _room12)
        {
            r.Message(Game18NConstString.QuickMissionComplete);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.UnlockPortal(_room12, _room13);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();

    public override void OnTimeout(IInstanceRun r) => r.Fail();
}

return new TimeSpace38();
