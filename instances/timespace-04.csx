public class TimeSpace4 : InstanceScript
{
    private readonly int _room1;
    private readonly int _room2;
    private readonly int _room3;
    private readonly int _room4;
    private readonly int _room5;
    private readonly int _room6;

    public TimeSpace4()
    {
        Definition = InstanceDefinitionBuilder.Named(4, "The Defence of NosVille", "The Defence of NosVille")
            .ForLevels(12, 99)
            .StartingAt(12, 9)
            .Requiring(1012, 1)
            .WithLives(3)
            .WithDraw(2024, 1)
            .WithDraw(211, 1)
            .WithSpecialReward(2042, 4)
            .WithReward(2038, 1)
            .WithReward(5, 1)
            .WithRoom(4100, out _room1, 0, 11)
            .WithRoom(4101, out _room2, 1, 11)
            .WithRoom(4100, out _room3, 2, 11)
            .WithRoom(4112, out _room4, 3, 11)
            .WithRoom(4112, out _room5, 4, 11)
            .WithRoom(4100, out _room6, 5, 11)
            .Build();
    }

    public override void OnFirstEnter(IInstanceRun r)
    {
        r.StartClock(TimeSpan.FromSeconds(250));
        r.SpawnPortal(_room1, new(28, 15), _room2, new(1, 14), locked: false);
        r.SpawnPortal(_room2, new(28, 15), _room3, new(1, 15), locked: false);
        r.SpawnPortal(_room2, new(1, 14), _room1, new(28, 15), locked: false);
        r.SummonNpcs(_room2, new NpcSpawn(320, new(22, 15)));
        r.SpawnPortal(_room3, new(28, 15), _room4, new(1, 15));
        r.SpawnPortal(_room3, new(1, 15), _room2, new(28, 15));
        r.Summon(_room3,
            new MonsterSpawn(129, new(21, 13)),
            new MonsterSpawn(129, new(10, 11), OnDeath: r =>
            {
                r.Summon(_room3, new MonsterSpawn(129, new(17, 10), IsBonus: true, OnDeath: r =>
                {
                    r.Summon(_room3, new MonsterSpawn(129, new(21, 18)));
                }));
            }));
        r.SpawnPortal(_room4, new(28, 14), _room5, new(1, 15));
        r.SpawnPortal(_room4, new(1, 15), _room3, new(28, 15));
        r.Summon(_room4, new MonsterSpawn(129, new(10, 19), OnDeath: r =>
            {
                r.Summon(_room4, new MonsterSpawn(129, new(10, 17), OnDeath: r =>
                {
                    r.Summon(_room4, new MonsterSpawn(129, new(10, 11), OnDeath: r =>
                    {
                        r.Summon(_room4, new MonsterSpawn(129, new(10, 14), OnDeath: r =>
                        {
                            r.Summon(_room4, new MonsterSpawn(55, new(13, 7)));
                        }));
                    }));
                }));
            }));
        r.SpawnPortal(_room5, new(28, 14), _room6, new(1, 15));
        r.SpawnPortal(_room5, new(1, 15), _room4, new(28, 14));
        r.PlaceButton(_room5, 1, new(23, 21), 1000, 1045);
        r.Summon(_room5, new MonsterSpawn(129, new(13, 11), OnDeath: r =>
            {
                r.Summon(_room5, new MonsterSpawn(129, new(13, 18), OnDeath: r =>
                {
                    r.Summon(_room5, new MonsterSpawn(129, new(13, 14), OnDeath: r =>
                    {
                        r.Summon(_room5, new MonsterSpawn(55, new(13, 23)));
                    }));
                }));
            }));
        r.SpawnExit(_room6, new(28, 15));
        r.SpawnPortal(_room6, new(1, 15), _room5, new(28, 14));
        r.PlaceButton(_room6, 1, new(14, 5), 1000, 1045);
        r.Summon(_room6,
            new MonsterSpawn(129, new(10, 21), OnDeath: r =>
            {
                r.Summon(_room6, new MonsterSpawn(129, new(10, 10), OnDeath: r =>
                {
                    r.Summon(_room6, new MonsterSpawn(129, new(12, 7), OnDeath: r =>
                    {
                        r.Summon(_room6, new MonsterSpawn(55, new(16, 22)));
                    }));
                }));
            }),
            new MonsterSpawn(129, new(10, 18)));
    }

    public override void OnRoomEntered(IInstanceRun r, int room)
    {
        if (room == _room1)
        {
            r.Message(Game18NConstString.MissionStartWhenEnterFirstRoom);
        }
        else if (room == _room2)
        {
            r.Dialog(_room2, 6015);
            r.Message(LanguageKey.TIMESPACE_GUARDS_ARRIVING);
        }
        else if (room == _room3)
        {
            r.Dialog(_room3, 6016);
        }
        else if (room == _room5)
        {
            r.Dialog(_room5, 6017);
        }
        else if (room == _room6)
        {
            r.Dialog(_room6, 6019);
            r.Message(LanguageKey.TIMESPACE_MORE_KEEP_COMING);
        }
    }

    public override void OnRoomCleared(IInstanceRun r, int room)
    {
        if (room == _room3)
        {
            r.UnlockPortal(_room3, _room4);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
        else if (room == _room4)
        {
            r.UnlockPortal(_room4, _room5);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
            r.Message(Game18NConstString.QuickMissionComplete);
        }
    }

    public override void OnButtonUsed(IInstanceRun r, int room, int button)
    {
        if (room == _room5 && button == 1)
        {
            r.UnlockPortal(_room5, _room6);
            r.AddClockTime(TimeSpan.FromSeconds(100));
            r.Message(Game18NConstString.ADoorHasOpened);
        }
        else if (room == _room6 && button == 1)
        {
            r.UnlockExit(_room6);
            r.Message(Game18NConstString.ADoorHasOpened);
        }
    }

    public override void OnExitReached(IInstanceRun r, int room) => r.Succeed();
}

return new TimeSpace4();
