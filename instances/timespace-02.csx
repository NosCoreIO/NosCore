public class TimeSpace2 : InstanceScript
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

    public TimeSpace2()
    {
        Definition = InstanceDefinitionBuilder.Named(2, "The Trap", "The Trap")
            .ForLevels(8, 99)
            .StartingAt(10, 9)
            .WithLives(3)
            .WithDraw(14, 1)
            .WithDraw(1002, 2)
            .WithSpecialReward(1012, 10)
            .WithReward(1035, 1)
            .WithRoom(4100, out _room1, 0, 8)
            .WithRoom(4106, out _room2, 1, 8)
            .WithRoom(4100, out _room3, 2, 8)
            .WithRoom(4105, out _room4, 3, 8)
            .WithRoom(4105, out _room5, 2, 9)
            .WithRoom(4107, out _room6, 3, 9)
            .WithRoom(4107, out _room7, 4, 9)
            .WithRoom(4107, out _room8, 3, 10)
            .WithRoom(4107, out _room9, 2, 10)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(3600));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 15), _room1, new(28, 15), locked: false);
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 14));
        r.SpawnPortal(_room3, new(14, 28), _room5, new(14, 1));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.PlaceButton(_room3, 1, new(14, 19), 1000, 1045);
        r.SpawnPortal(_room4, new(1, 14), _room3, new(28, 15));
        r.Summon(_room4,
            new MonsterSpawn(54, new(9, 15), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(54, new(15, 9), IsBonus: true));
            }),
            new MonsterSpawn(54, new(8, 10)));
        r.SpawnPortal(_room5, new(14, 1), _room3, new(14, 28), locked: false);
        r.SpawnPortal(_room5, new(28, 15), _room6, new(1, 14), locked: false);
        r.SpawnPortal(_room5, new(14, 28), _room9, new(14, 1), locked: false);
        r.SpawnPortal(_room6, new(28, 15), _room7, new(1, 14), locked: false);
        r.SpawnPortal(_room6, new(14, 28), _room8, new(14, 1), locked: false);
        r.SpawnPortal(_room6, new(1, 14), _room5, new(28, 15), locked: false);
        r.SpawnExit(_room7, new(28, 15), locked: false);
        r.SpawnPortal(_room7, new(1, 14), _room6, new(28, 15), locked: false);
        r.SpawnPortal(_room8, new(14, 1), _room6, new(14, 28));
        r.Summon(_room8,
            new MonsterSpawn(54, new(11, 19), OnDeath: r =>
            {
                r.Summon(_room8, new MonsterSpawn(9, new(10, 10)));
            }),
            new MonsterSpawn(54, new(8, 18), IsBonus: true));
        r.SpawnPortal(_room9, new(14, 1), _room5, new(14, 28));
        r.Summon(_room9, new MonsterSpawn(54, new(10, 17), OnDeath: r =>
            {
                r.Summon(_room9, new MonsterSpawn(32, new(14, 10), OnDeath: r =>
                {
                    r.Summon(_room9,
                    new MonsterSpawn(54, new(11, 11)),
                    new MonsterSpawn(54, new(18, 12)));
                }));
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
            r.Dialog(_room2, 6005);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6006);
        }
        else if (room == _room4)
        {
            r.Objective(_room4, LanguageKey.TIMESPACE_GOAL_IS_IT_A_DEAD_END);
        }
        else if (room == _room7)
        {
            r.Dialog(_room7, 6011);
        }
        else if (room == _room8)
        {
            r.Objective(_room8, LanguageKey.TIMESPACE_GOAL_DEAD_END_HURRY);
        }
        else if (room == _room9)
        {
            r.Objective(_room9, LanguageKey.TIMESPACE_GOAL_KILL_ALL_FOR_BONUS_TIME);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.UnlockPortal(_room3, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(60));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room3, 6007);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room3);
            r.AddClockTime(TimeSpan.FromSeconds(80));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Dialog(_room4, 6008);
        }
        else if (room == _room8)
        {
            r.UnlockPortal(_room8, _room6);
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room9)
        {
            r.UnlockPortal(_room9, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(30));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(LanguageKey.TIMESPACE_ALL_MONSTERS_DEFEATED);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room3 && button == 1)
        {
            r.AddClockTime(TimeSpan.FromSeconds(1));
            r.Objective(_room3, LanguageKey.TIMESPACE_GOAL_MUST_KILL_ALL_IN_TIME);
            r.Summon(_room3,
                new MonsterSpawn(31, new(11, 15)),
                new MonsterSpawn(31, new(11, 12)));
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace2();
