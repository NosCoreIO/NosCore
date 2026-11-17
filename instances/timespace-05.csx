public class TimeSpace5 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace5()
    {
        Definition = InstanceDefinitionBuilder.Named(5, "An Invincible Enemy", "An Invincible Enemy")
            .ForLevels(14, 99)
            .StartingAt(8, 15)
            .Requiring(1012, 1)
            .WithLives(3)
            .WithDraw(11, 1)
            .WithDraw(503, 1)
            .WithSpecialReward(2046, 4)
            .WithReward(2042, 1)
            .WithRoom(4100, out _room1, 0, 11)
            .WithRoom(4101, out _room2, 1, 11)
            .WithRoom(4102, out _room3, 2, 11)
            .WithRoom(4103, out _room4, 3, 11)
            .WithRoom(4104, out _room5, 4, 11)
            .WithRoom(4105, out _room6, 5, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(180));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 14), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(1, 14), _room2, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(129, new(10, 17)),
            new MonsterSpawn(129, new(10, 12), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(129, new(14, 9), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(55, new(20, 14), OnDeath: r =>
                    {
                        r.Summon(_room3, new MonsterSpawn(55, new(13, 13)));
                    }));
                }));
            }));
        r.SpawnPortal(_room4, new(28, 15), _room5, new(1, 15), locked: false);
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 14), locked: false);
        r.SummonNpcs(_room4, new NpcSpawn(336, new(9, 14)));
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 14));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 15));
        r.Summon(_room5,
            new MonsterSpawn(55, new(13, 21), OnDeath: r =>
            {
                r.Summon(_room5,
                new MonsterSpawn(55, new(13, 16)),
                new MonsterSpawn(55, new(18, 20)));
            }),
            new MonsterSpawn(270, new(17, 16), IsBonus: true));
        r.SpawnExit(_room6, new(28, 15));
        r.SpawnPortal(_room6, new(1, 14), _room5, new(28, 15));
        r.SummonNpcs(_room6, new NpcSpawn(320, new(10, 9)), new NpcSpawn(320, new(9, 17)), new NpcSpawn(320, new(7, 12)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6020);
        }
        else if (room == _room3)
        {
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_WATCH_OUT_ENEMIES);
        }
        else if (room == _room4)
        {
            r.Dialog(_room4, 6021);
        }
        else if (room == _room5)
        {
            r.Objective(_room5, LanguageKey.TIMESPACE_GOAL_BOSS_APPROACHING);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6024);
            r.Objective(_room6, LanguageKey.TIMESPACE_GOAL_BOSS_APPEARED);
        }
    }

    public override void OnDialogClosed(IInstanceRun r, int room, int dialog)
    {
        if (room == _room6 && dialog == 6024)
        {
            r.Summon(_room6,
                new MonsterSpawn(55, new(22, 22)),
                new MonsterSpawn(55, new(20, 14), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(55, new(19, 19)));
                }),
                new MonsterSpawn(270, new(17, 14), IsBonus: true));
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room5)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room5, 6023);
        }
        else if (room == _room6)
        {
            r.UnlockExit(_room6);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room6, 6025);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace5();
