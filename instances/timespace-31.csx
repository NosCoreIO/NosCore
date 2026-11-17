public class TimeSpace31 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;
    private readonly int _room7;

    public TimeSpace31()
    {
        Definition = InstanceDefinitionBuilder.Named(31, "Deux h�ros des Koarens", "Deux h�ros des Koarens")
            .ForLevels(41, 99)
            .StartingAt(13, 12)
            .Requiring(1012, 4)
            .WithLives(2)
            .WithDraw(712, 1)
            .WithDraw(1028, 2)
            .WithSpecialReward(802, 1)
            .WithReward(2134, 1)
            .WithReward(1087, 1)
            .WithRoom(4202, out _room1, 0, 8)
            .WithRoom(4205, out _room2, 1, 8)
            .WithRoom(4209, out _room3, 2, 8)
            .WithRoom(4207, out _room4, 2, 9)
            .WithRoom(4205, out _room5, 3, 9)
            .WithRoom(4205, out _room6, 4, 9)
            .WithRoom(4205, out _room7, 5, 9)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(300));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15));
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15));
        r.Summon(_room2, new MonsterSpawn(329, new(17, 13), IsBonus: true));
        r.SpawnPortal(_room3, new(14, 28), _room4, new(14, 1));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.Summon(_room3, new MonsterSpawn(315, new(16, 13), IsBonus: true));
        r.SpawnPortal(_room4, new(14, 1), _room3, new(14, 28));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15));
        r.Summon(_room4, new MonsterSpawn(315, new(13, 15)));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 15));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15));
        r.Summon(_room5,
            new MonsterSpawn(67, new(20, 11)),
            new MonsterSpawn(67, new(15, 8), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(67, new(11, 18), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(15, 10)));
                }),
                new MonsterSpawn(67, new(15, 21), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(14, 19)));
                }),
                new MonsterSpawn(67, new(18, 18), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(143, new(14, 14)));
                }));
            }),
            new MonsterSpawn(67, new(10, 12)));
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 15));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 15));
        r.Summon(_room6,
            new MonsterSpawn(67, new(14, 12), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(67, new(14, 8), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(84, new(19, 19), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(84, new(18, 11)));
                    }));
                }));
            }),
            new MonsterSpawn(67, new(14, 20), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(67, new(12, 19), IsBonus: true));
            }),
            new MonsterSpawn(67, new(14, 4), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(67, new(13, 25), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(84, new(21, 15)));
                }));
            }));
        r.SpawnExit(_room7, new(28, 15));
        r.SpawnPortal(_room7, new(1, 15), _room6, new(28, 15));
        r.Summon(_room7,
            new MonsterSpawn(137, new(19, 16)),
            new MonsterSpawn(137, new(17, 20), OnDeath: r =>
            {
                r.Summon(_room7, new MonsterSpawn(144, new(19, 8)));
            }),
            new MonsterSpawn(137, new(17, 11)),
            new MonsterSpawn(137, new(14, 21), OnDeath: r =>
            {
                r.Summon(_room7,
                new MonsterSpawn(144, new(21, 12)),
                new MonsterSpawn(144, new(19, 22), OnDeath: r =>
                {
                    r.Summon(_room7,
                    new MonsterSpawn(61, new(16, 18), OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(61, new(12, 17), OnDeath: r =>
                        {
                            r.Summon(_room7, new MonsterSpawn(61, new(11, 13)));
                        }));
                    }),
                    new MonsterSpawn(61, new(17, 14), OnDeath: r =>
                    {
                        r.Summon(_room7, new MonsterSpawn(61, new(14, 12)));
                    }));
                }),
                new MonsterSpawn(144, new(20, 18)));
            }),
            new MonsterSpawn(137, new(13, 10)),
            new MonsterSpawn(137, new(9, 20)),
            new MonsterSpawn(137, new(9, 11)),
            new MonsterSpawn(137, new(8, 15), IsBonus: true));
        r.SummonNpcs(_room7, new NpcSpawn(307, new(14, 15)), new NpcSpawn(315, new(7, 11)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Objective(_room2, LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6140);
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6141);
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_KILL_ALL_ENEMIES);
        }
        else if (room == _room5)
        {
            r.Objective(_room5, LanguageKey.TIMESPACE_GOAL_MONSTERS_ATTACK);
        }
        else if (room == _room6)
        {
            r.Objective(_room6, LanguageKey.TIMESPACE_GOAL_SURPRISE_ATTACK);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6143);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.Message(_room2, Game18NConstString.ADoorHasOpened);
            r.Dialog(_room2, 6139);
        }
        else if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.Message(_room3, Game18NConstString.ADoorHasOpened);
            r.Message(_room3, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.Message(_room4, Game18NConstString.ADoorHasOpened);
            r.Message(_room4, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.Message(_room5, Game18NConstString.ADoorHasOpened);
            r.Message(_room5, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room6)
        {
            r.UnlockPortal(_room6, _room7);
            r.Message(_room6, Game18NConstString.ADoorHasOpened);
            r.Dialog(_room6, 6142);
        }
        else if (room == _room7)
        {
            r.UnlockExit(_room7);
            r.Message(_room7, Game18NConstString.ADoorHasOpened);
            r.Dialog(_room7, 6144);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace31();
