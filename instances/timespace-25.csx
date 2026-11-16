public class TimeSpace25 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace25()
    {
        Definition = InstanceDefinitionBuilder.Named(25, "L'invincible Kenko", "L'invincible Kenko")
            .ForLevels(35, 99)
            .StartingAt(7, 11)
            .Requiring(1012, 3)
            .WithLives(1)
            .WithDraw(518, 1)
            .WithDraw(234, 1)
            .WithSpecialReward(2043, 15)
            .WithReward(2099, 2)
            .WithReward(1087, 1)
            .WithRoom(5206, out _room1, 3, 11)
            .WithRoom(5206, out _room2, 3, 10)
            .WithRoom(5206, out _room3, 3, 9)
            .WithRoom(5206, out _room4, 3, 8)
            .WithRoom(5206, out _room5, 3, 7)
            .WithRoom(5206, out _room6, 3, 6)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(250));
        r.SpawnPortal(_room1, new(12, 1), _room2, new(12, 23), locked: false);
        r.SpawnPortal(_room2, new(12, 1), _room3, new(12, 23));
        r.SpawnPortal(_room2, new(12, 23), _room1, new(12, 1));
        r.Summon(_room2,
            new MonsterSpawn(118, new(14, 10), OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(118, new(13, 19)));
            }),
            new MonsterSpawn(115, new(11, 22)),
            new MonsterSpawn(115, new(9, 15), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room2, new MonsterSpawn(118, new(14, 15)));
            }));
        r.SpawnPortal(_room3, new(12, 1), _room4, new(12, 23));
        r.SpawnPortal(_room3, new(12, 23), _room2, new(12, 1));
        r.Summon(_room3,
            new MonsterSpawn(118, new(15, 18), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(118, new(12, 12), OnDeath: r =>
                {
                    r.Summon(_room3,
                    new MonsterSpawn(121, new(8, 14), IsBonus: true),
                    new MonsterSpawn(121, new(13, 15), OnDeath: r =>
                    {
                        r.Summon(_room3,
                        new MonsterSpawn(121, new(9, 10)),
                        new MonsterSpawn(121, new(13, 10)));
                    }));
                }));
            }),
            new MonsterSpawn(118, new(15, 13)));
        r.SpawnPortal(_room4, new(12, 1), _room5, new(12, 23));
        r.SpawnPortal(_room4, new(12, 23), _room3, new(12, 1));
        r.Summon(_room4,
            new MonsterSpawn(118, new(12, 16), IsBonus: true, OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(121, new(14, 18)));
            }),
            new MonsterSpawn(118, new(11, 18), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(118, new(9, 15), OnDeath: r =>
                {
                    r.Summon(_room4,
                    new MonsterSpawn(121, new(8, 9), IsBonus: true, OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(121, new(9, 14), OnDeath: r =>
                        {
                            r.Summon(_room4, new MonsterSpawn(118, new(14, 16), IsBonus: true));
                        }));
                    }),
                    new MonsterSpawn(121, new(14, 13), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(121, new(16, 12)));
                    }));
                }));
            }));
        r.SpawnPortal(_room5, new(12, 1), _room6, new(12, 23));
        r.SpawnPortal(_room5, new(12, 23), _room4, new(12, 1));
        r.Summon(_room5,
            new MonsterSpawn(118, new(17, 10), IsBonus: true),
            new MonsterSpawn(118, new(16, 12), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(115, new(10, 17)),
                new MonsterSpawn(118, new(12, 15), OnDeath: r =>
                {
                    r.Summon(_room5,
                    new MonsterSpawn(115, new(13, 17), OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(115, new(14, 17)));
                    }),
                    new MonsterSpawn(115, new(16, 15)));
                }));
            }),
            new MonsterSpawn(118, new(10, 11)));
        r.SpawnExit(_room6, new(12, 1));
        r.SpawnPortal(_room6, new(12, 23), _room5, new(12, 1));
        r.Summon(_room6,
            new MonsterSpawn(118, new(16, 10)),
            new MonsterSpawn(316, new(15, 18)),
            new MonsterSpawn(118, new(9, 9)),
            new MonsterSpawn(115, new(8, 11)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(_room1, Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Objective(_room2, LanguageKey.TIMESPACE_GOAL_FIGHT_KOVOLTS);
        }
        else if (room == _room3)
        {
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_FIGHT_KOVOLTS);
        }
        else if (room == _room4)
        {
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_FIGHT_KOVOLTS);
        }
        else if (room == _room5)
        {
            r.Objective(_room5, LanguageKey.TIMESPACE_GOAL_FIGHT_KOVOLTS);
        }
        else if (room == _room6)
        {
            r.StartRoomClock(_room6, TimeSpan.FromSeconds(120));
            r.Dialog(_room6, 6123);
            r.Objective(_room6, LanguageKey.TIMESPACE_GOAL_SURVIVE_VIOLENT_KENKO);
            r.At(r.Elapsed + TimeSpan.FromSeconds(120), r =>
            {
                r.Dismiss(_room6, 115);
                r.Dismiss(_room6, 118);
                r.Dismiss(_room6, 316);
                r.UnlockExit(_room6);
                r.Message(_room6, Game18NConstString.ADoorHasOpened);
                r.Message(_room6, Game18NConstString.QuickMissionComplete);
                r.StopRoomClock(_room6);
            });
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room2)
        {
            r.UnlockPortal(_room2, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(_room2, Game18NConstString.ADoorHasOpened);
            r.Message(_room2, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(_room3, Game18NConstString.ADoorHasOpened);
            r.Message(_room3, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(_room4, Game18NConstString.ADoorHasOpened);
            r.Message(_room4, Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(200));
            r.Message(_room5, Game18NConstString.ADoorHasOpened);
            r.Message(_room5, Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace25();
